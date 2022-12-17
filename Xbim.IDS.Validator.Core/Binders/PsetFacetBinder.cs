using Microsoft.Extensions.Logging;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Binders
{
#nullable disable
    public class PsetFacetBinder : FacetBinderBase<IfcPropertyFacet>
    {

        public PsetFacetBinder(IModel model) : base(model)
        {

        }

        /// <summary>
        /// Binds an IFC property filter to an expression, where propertoes are IFC Pset and Quantity fields
        /// </summary>
        /// <remarks>e.g Where(p=> p.RelatingPropertyDefinition... )</remarks>
        /// <param name="baseExpression"></param>
        /// <param name="psetFacet"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public override Expression BindSelectionExpression(Expression baseExpression, IfcPropertyFacet psetFacet)
        {
            if (baseExpression is null)
            {
                throw new ArgumentNullException(nameof(baseExpression));
            }

            if (psetFacet is null)
            {
                throw new ArgumentNullException(nameof(psetFacet));
            }

            if (!psetFacet.IsValid())
            {
                // IsValid checks against a known list of all IFC Attributes
                throw new InvalidOperationException($"IFC Property Facet '{psetFacet?.PropertySetName}'.{psetFacet?.PropertyName} is not valid");
            }


            var expression = baseExpression;
            // When an Ifc Type has not yet been specified, we start with the RelDefinesByProperties
            
            if (expression.Type.IsInterface && expression.Type.IsAssignableTo(typeof(IEntityCollection)))
            {
                expression = BindIfcExpressType(expression, Model.Metadata.ExpressType(typeof(IfcRelDefinesByProperties)));
                expression = BindPropertySelection(expression, psetFacet);
                return expression;
            }

            throw new NotSupportedException("Selection of Psets must be the first expression in the graph");
        }

        public override Expression BindWhereExpression(Expression baseExpression, IfcPropertyFacet facet)
        {
            if (baseExpression is null)
            {
                throw new ArgumentNullException(nameof(baseExpression));
            }

            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }

            if (!facet.IsValid())
            {
                // IsValid checks against a known list of all IFC Attributes
                throw new InvalidOperationException($"IFC Property Facet '{facet?.PropertySetName}'.{facet?.PropertyName} is not valid");
            }


            var expression = baseExpression;
            
            if (expression.Type.IsInterface && expression.Type.IsAssignableTo(typeof(IEntityCollection)))
            {
                throw new NotSupportedException("Expected a selection expression before applying filters");
            }

            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);
            var expressType = Model.Metadata.ExpressType(collectionType);
            ValidateExpressType(expressType);

            expression = BindPropertyFilter(expression, facet);
            return expression;
        }

        public override void ValidateEntity(IPersistEntity item, FacetGroup requirement, ILogger logger, IdsValidationResult result, IfcPropertyFacet pf)
        {
            var ctx = CreateValidationContext(requirement, pf);
            var psets = GetPropertySetsMatching(item.EntityLabel, pf.PropertySetName, logger);
            if (psets.Any())
            {
                foreach (var pset in psets)
                {
                    result.Messages.Add(ValidationMessage.Success(ctx, fn => fn.PropertySetName!, pset.Name, "Pset Matched", pset));
                    var props = GetPropertiesMatching<IIfcPropertySingleValue>(item.EntityLabel, pset.Name, pf.PropertyName);
                    var quants = GetQuantitiesMatching(item.EntityLabel, pset.Name, pf.PropertyName);
                    if (props.Any() || quants.Any())
                    {
                        foreach (var prop in props)
                        {
                            var propValue = prop.NominalValue;
                            object? value = UnwrapValue(propValue);
                            bool isPopulated = IsValueRelevant(value);
                            if (isPopulated)
                            {
                                result.Messages.Add(ValidationMessage.Success(ctx, fn => fn.PropertyName!, prop.Name, "Property provided", prop));
                            }
                            else
                            {
                                result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.PropertyName!, prop.Name, "No property matching", prop));
                            }
                            ValidateMeasure(ctx, result, propValue, pf.Measure);

                            ValidatePropertyValue(ctx, logger, result, pf, value, propValue);
                        }
                        foreach (var quant in quants)
                        {
                            var propValue = UnwrapQuantity(quant);
                            object? value = UnwrapValue(propValue);
                            bool isPopulated = IsValueRelevant(value);

                            if (isPopulated)
                            {
                                result.Messages.Add(ValidationMessage.Success(ctx, fn => fn.PropertyName!, quant.Name, "Quantity provided", quant));
                            }
                            else
                            {
                                result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.PropertyName!, quant.Name, "No quantity matching", quant));
                            }
                            ValidateMeasure(ctx, result, propValue, pf.Measure);

                            ValidatePropertyValue(ctx, logger, result, pf, value, propValue);
                        }
                    }
                    else
                    {
                        result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.PropertyName!, null, "No properties matching", pset));
                    }
                }
            }

            else
            {
                result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.PropertySetName!, null, "No Psets matching", item));
            }


        }


        private Expression BindPropertySelection(Expression expression, IfcPropertyFacet psetFacet)
        {
            if (psetFacet is null)
            {
                throw new ArgumentNullException(nameof(psetFacet));
            }
            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);

            // call .Cast<EntityType>()
            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(collectionType), expression);

            if (psetFacet?.PropertySetName?.AcceptedValues?.Any() == false ||
                psetFacet?.PropertyName?.AcceptedValues?.Any() == false)
            {
                return expression;
            }

            var facetExpr = Expression.Constant(psetFacet, typeof(IfcPropertyFacet));
            // Expression we're building
            // var psetRelDefines = model.Instances.OfType<IIfcRelDefinesByProperties>();
            // var entities = IfcExtensions.GetIfcObjectsWithProperties(psetRelDefines, facet);

            var propsMethod = ExpressionHelperMethods.EnumerableIfcObjectsWithProperties;

            return Expression.Call(null, propsMethod, new[] { expression, facetExpr });

          
        }


        private Expression BindPropertyFilter(Expression expression, IfcPropertyFacet facet)
        {
            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }
            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);

          
            MethodInfo propsMethod;
            if(collectionType.IsAssignableTo(typeof(IIfcObject)))
            {
                propsMethod = ExpressionHelperMethods.EnumerableObjectWhereAssociatedWithProperty;

            } else if (collectionType.IsAssignableTo(typeof(IIfcTypeObject)))
            {
                propsMethod = ExpressionHelperMethods.EnumerableTypeWhereAssociatedWithProperty;
            }
            else
            {
                // TODO: log
                // Not applicable
                return BindNotFound(expression, collectionType);
            }
            ConstantExpression psetFacetExpr = Expression.Constant(facet, typeof(IfcPropertyFacet));

            return Expression.Call(null, propsMethod, new[] { expression, psetFacetExpr });


        }


        // For Selections on instances we don't need to use expressions. When filtering we will
        /// <summary>
        /// Gets a specific property for an entity, matching a psetName and property name
        /// </summary>
        /// <param name="entityLabel"></param>
        /// <param name="psetName"></param>
        /// <param name="propName"></param>
        /// <returns></returns>
        public IIfcValue GetProperty(int entityLabel, string psetName, string propName)
        {
            var entity = Model.Instances[entityLabel];

            IIfcPropertySingleValue psetValue;
            if (entity is IIfcTypeObject type)
            {
                psetValue = type.HasPropertySets.OfType<IIfcPropertySet>()
                    .Where(p => p.Name == psetName)
                    .SelectMany(p => p.HasProperties.Where(ps => ps.Name == propName)
                        .OfType<IIfcPropertySingleValue>())
                    .FirstOrDefault();
            }
            else if (entity is IIfcObject obj)
            {

                psetValue = obj.IsDefinedBy
                    .Where(r => r.RelatingPropertyDefinition is IIfcPropertySet ps && ps.Name == psetName)
                    .SelectMany(p => ((IIfcPropertySet)p.RelatingPropertyDefinition)
                        .HasProperties.Where(ps => ps.Name == propName)
                        .OfType<IIfcPropertySingleValue>())
                    
                    .FirstOrDefault();
                if (psetValue == null)
                {
                    if (obj.IsTypedBy?.Any() == true)
                    {
                        return GetProperty(obj.IsTypedBy.First().RelatingType.EntityLabel, psetName, propName);
                    }
                }
            }
            else
            {
                return null;
            }

            return psetValue?.NominalValue;

        }

        public IIfcPhysicalQuantity GetQuantity(int entityLabel, string psetName, string propName)
        {
            var entity = Model.Instances[entityLabel];

            IIfcPhysicalQuantity psetValue;
            if (entity is IIfcTypeObject type)
            {
                psetValue = type.HasPropertySets.OfType<IIfcElementQuantity>()
                    .Where(p => p.Name == psetName)
                    .SelectMany(p => p.Quantities.Where(ps => ps.Name == propName))
                    .FirstOrDefault();
            }
            else if (entity is IIfcObject obj)
            {

                psetValue = obj.IsDefinedBy
                       .Where(r => r.RelatingPropertyDefinition is IIfcElementQuantity ps && ps.Name == psetName)
                       .SelectMany(p => ((IIfcElementQuantity)p.RelatingPropertyDefinition)
                            .Quantities.Where(q => q.Name == propName))
                       .FirstOrDefault();

                if (psetValue == null)
                {
                    if (obj.IsTypedBy?.Any() == true)
                    {
                        return GetQuantity(obj.IsTypedBy.First().RelatingType.EntityLabel, psetName, propName);
                    }
                }

            }
            else
            {
                return default;
            }

            return psetValue;

        }

        /// <summary>
        /// Finds all Properties in a pset meeting a constraint
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entityLabel"></param>
        /// <param name="psetName"></param>
        /// <param name="constraint"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public IEnumerable<T> GetPropertiesMatching<T>(int entityLabel, string psetName, ValueConstraint constraint, ILogger logger = null) where T: IIfcProperty
        {
            var entity = Model.Instances[entityLabel];
            if (entity is IIfcTypeObject type)
            {
                var typeProperties = type.HasPropertySets.OfType<IIfcPropertySet>()
                    .Where(p => p.Name == psetName)
                    .SelectMany(p => p.HasProperties.Where(ps => constraint.IsSatisfiedBy(ps.Name.Value, true, logger))
                        .OfType<T>());
                return typeProperties;


            }
            else if (entity is IIfcObject obj)
            {
                var entityProperties = obj.IsDefinedBy
                    .Where(r => r.RelatingPropertyDefinition is IIfcPropertySet ps && ps.Name == psetName)
                    .SelectMany(p => ((IIfcPropertySet)p.RelatingPropertyDefinition)
                        .HasProperties.Where(ps => constraint.IsSatisfiedBy(ps.Name.Value, true, logger))
                        .OfType<T>());
                    
                
                if (obj.IsTypedBy?.Any() == true)
                {
                    // Inherit extra properties from Type - Deduping on name
                    entityProperties = entityProperties
                        .Union(GetPropertiesMatching<T>(obj.IsTypedBy.First().RelatingType.EntityLabel, psetName, constraint, logger), new PropertyEqualityComparer<T>());
                }
                return entityProperties;


            }
            else
            {
                return Enumerable.Empty<T>();
            }
        }

        /// <summary>
        /// Finds all Quantities in a pset meeting a constraint
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entityLabel"></param>
        /// <param name="psetName"></param>
        /// <param name="nameConstraint"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public IEnumerable<IIfcPhysicalQuantity> GetQuantitiesMatching(int entityLabel, string psetName, ValueConstraint nameConstraint, ILogger logger = null)
        {
            var entity = Model.Instances[entityLabel];
            if (entity is IIfcTypeObject type)
            {
                var typeProperties = type.HasPropertySets.OfType<IIfcElementQuantity>()
                    .Where(p => p.Name == psetName)
                    .SelectMany(p => p.Quantities.Where(ps => nameConstraint.IsSatisfiedBy(ps.Name.Value, true, logger)));
                return typeProperties;


            }
            else if (entity is IIfcObject obj)
            {
                var entityProperties = obj.IsDefinedBy
                    .Where(r => r.RelatingPropertyDefinition is IIfcElementQuantity ps && ps.Name == psetName)
                    .SelectMany(p => ((IIfcElementQuantity)p.RelatingPropertyDefinition)
                        .Quantities.Where(ps => nameConstraint.IsSatisfiedBy(ps.Name.Value, true, logger)));


                if (obj.IsTypedBy?.Any() == true)
                {
                    // Inherit extra properties from Type - Deduping on Name
                    entityProperties = entityProperties
                        .Union(GetQuantitiesMatching(obj.IsTypedBy.First().RelatingType.EntityLabel, psetName, nameConstraint, logger), new QuantityEqualityComparer());
                }
                return entityProperties;


            }
            else
            {
                return Enumerable.Empty<IIfcPhysicalQuantity>();
            }
        }


        public IEnumerable<IIfcPropertySetDefinition> GetPropertySetsMatching(int entityLabel, ValueConstraint psetConstraint, ILogger logger = null)
        {
            var entity = Model.Instances[entityLabel];
            if (entity is IIfcTypeObject type)
            {
                var typeProperties = type.HasPropertySets.OfType<IIfcPropertySetDefinition>()
                    .Where(p => psetConstraint.IsSatisfiedBy(p.Name.ToString(), true, logger));
                return typeProperties;

            }
            else if (entity is IIfcObject obj)
            {

                var entityProperties = obj.IsDefinedBy
                    .Where(t => t.RelatingPropertyDefinition is IIfcPropertySetDefinition ps && psetConstraint.IsSatisfiedBy(ps.Name.ToString(), true, logger))
                    .Select(p => (IIfcPropertySetDefinition)p.RelatingPropertyDefinition);


                if (obj.IsTypedBy?.Any() == true)
                {
                    // Inherit extra properties from Type
                    entityProperties = entityProperties.Concat(GetPropertySetsMatching(obj.IsTypedBy.First().RelatingType.EntityLabel, psetConstraint, logger));
                }

                return entityProperties;
            }
            else
            {
                return Enumerable.Empty<IIfcPropertySet>();
            }
        }



        private bool ValidatePropertyValue(ValidationContext<IfcPropertyFacet> ctx, ILogger logger, IdsValidationResult result, IfcPropertyFacet pf, object value, IIfcValue ifcValue)
        {
            if (pf.PropertyValue != null)
            {
                value = ApplyWorkarounds(value, pf.PropertyValue);
                if (IsTypeAppropriateForConstraint(pf.PropertyValue, value) &&  pf.PropertyValue.IsSatisfiedBy(value, logger))
                {
                    result.Messages.Add(ValidationMessage.Success(ctx, fn => fn.PropertyValue!, value, "Value matches", ifcValue));
                    return true;
                }
                else
                {
                    result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.PropertyValue!, value, "Invalid Value", ifcValue));
                    return false;
                }
            }
            return true;
        }


        protected void ValidateMeasure(ValidationContext<IfcPropertyFacet> ctx, IdsValidationResult result, IIfcValue propValue, string expectedMeasure)
        {
            if (propValue is null)
            {
                return;
            }

            if (string.IsNullOrEmpty(expectedMeasure)) return;

            var measure = Model.Metadata.ExpressType(propValue).Name;
            if (measure.Equals(expectedMeasure, StringComparison.InvariantCultureIgnoreCase))
            {
                result.Messages.Add(ValidationMessage.Success(ctx, fn => fn.Measure!, measure, "Measure matches", propValue));
            }
            else
            {
                result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.Measure!, measure, "Invalid Measure", propValue));
            }
        }



        private class QuantityEqualityComparer : IEqualityComparer<IIfcPhysicalQuantity>
        {
            public bool Equals(IIfcPhysicalQuantity x, IIfcPhysicalQuantity y)
            {
                return x?.Name == y?.Name;
            }

            public int GetHashCode([DisallowNull] IIfcPhysicalQuantity obj)
            {
                return (obj.Name, obj.Description).GetHashCode();
            }
        }

        private class PropertyEqualityComparer<T> : IEqualityComparer<T> where T: IIfcProperty
        {
            public bool Equals(T x, T y)
            {
                return x?.Name == y?.Name;
            }

            public int GetHashCode([DisallowNull] T obj)
            {
                return (obj.Name, obj.Description).GetHashCode();
            }
        }
    }
}
