using System.Linq.Expressions;
using System.Reflection;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.UtilityResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.InformationSpecifications;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications.Helpers;
using Microsoft.Extensions.Logging;
using Xbim.Ifc4.Kernel;
using Xbim.IDS.Validator.Core.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace Xbim.IDS.Validator.Core
{
#nullable disable

    /// <summary>
    /// Class to dynamically bind IDS Facets to IModel 
    /// <see cref="IQueryable{T}"/> and <see cref="IEnumerable{T}"/> <see cref="Expression"/>s
    /// enabling late-bound querying and filtering of Entities
    /// </summary>
    public class IdsFacetBinder
    {
        private readonly IModel model;

        public IdsFacetBinder(IModel model)
        {
            this.model = model;
        }

        

        /// <summary>
        /// Binds an <see cref="IFacet"/> to an Expression bound to filter on IModel.Instances
        /// </summary>
        /// <param name="baseExpression"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Expression BindFilters(Expression baseExpression, IFacet facet)
        {
            switch (facet)
            {
                case IfcTypeFacet f:
                    return BindFilterExpression(baseExpression, f);

                case AttributeFacet af:
                    return BindFilterExpression(baseExpression, af);

                case IfcPropertyFacet pf:
                    // TODO:
                    return baseExpression;

                case IfcClassificationFacet af:
                    // TODO: 
                    return baseExpression;

                case DocumentFacet df:
                    // TODO: 
                    return baseExpression;

                case IfcRelationFacet rf:
                    // TODO: 
                    return baseExpression;

                case PartOfFacet pf:
                    // TODO: 
                    return baseExpression;

                case MaterialFacet mf:
                    // TODO: 
                    return baseExpression;

                default:
                    throw new NotImplementedException($"Facet not implemented: '{facet.GetType().Name}'");
            }
        }

        /// <summary>
        /// Builds expression filtering on an IFC Type Facet
        /// </summary>
        /// <param name="baseExpression"></param>
        /// <param name="ifcFacet"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public Expression BindFilterExpression(Expression baseExpression, IfcTypeFacet ifcFacet)
        {
            if (baseExpression is null)
            {
                throw new ArgumentNullException(nameof(baseExpression));
            }

            if (ifcFacet is null)
            {
                throw new ArgumentNullException(nameof(ifcFacet));
            }

            if (!ifcFacet.IsValid())
            {
                throw new InvalidOperationException("IfcTypeFacet is not valid");
            }

            var expressTypes = GetExpressTypes(ifcFacet);
            ValidateExpressTypes(expressTypes);

            var expression = baseExpression;
            bool doConcat = false;
            foreach(var expressType in expressTypes)
            {
                var rightExpr = baseExpression;
                rightExpr = BindIfcType(rightExpr, expressType);
                if (ifcFacet.PredefinedType != null)
                    rightExpr = BindPredefinedTypeFilter(ifcFacet, rightExpr, expressType);


                // Union to main expression.
                if(doConcat)
                {
                    expression = BindConcat(expression, rightExpr);
                }
                else
                {
                    expression = rightExpr;
                    doConcat = true;
                }
            }
                
            return expression;
            
        }

        private Expression BindConcat(Expression expression, Expression right)
        {
            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(typeof(IIfcRoot)), expression);

            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableConcatGeneric.MakeGenericMethod(typeof(IIfcRoot)), expression, right);
            return expression;
        }

        /// <summary>
        /// Binds an IFC attribute filter to an expression, where Attributes are built in IFC schema fields
        /// </summary>
        /// <remarks>e.g Where(p=> p.GlobalId == "someGuid")</remarks>
        /// <param name="baseExpression"></param>
        /// <param name="attrFacet"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public Expression BindFilterExpression(Expression baseExpression, AttributeFacet attrFacet)
        {
            if (baseExpression is null)
            {
                throw new ArgumentNullException(nameof(baseExpression));
            }

            if (attrFacet is null)
            {
                throw new ArgumentNullException(nameof(attrFacet));
            }

            if (!attrFacet.IsValid())
            {
                // IsValid checks against a know list of all IFC Attributes
                throw new InvalidOperationException($"Attribute Facet '{attrFacet?.AttributeName}' is not valid");
            }


            var expression = baseExpression;
            // When an Ifc Type has not yet been specified, find correct root type(s) for this AttributeName
            // using the lookup that XIDS provides
            if (expression.Type.IsInterface && expression.Type.IsAssignableTo(typeof(IEntityCollection)))
            {

                // TODO: Use correct IFC Schema
                var rootTypes = SchemaInfo.SchemaIfc4.GetAttributeClasses(attrFacet.AttributeName.SingleValue(), onlyTopClasses: true);

                IfcTypeFacet ifcFacet = new IfcTypeFacet
                {
                    IfcType = new ValueConstraint(NetTypeName.String)
                };
                foreach (var root in rootTypes)
                {
                    ifcFacet.IfcType.AcceptedValues?.Add(new ExactConstraint(root));
                }
                expression = BindFilterExpression(expression, ifcFacet);
            }
            
            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);
            var expressType = model.Metadata.ExpressType(collectionType);
            ValidateExpressType(expressType);

            expression = BindEqualsAttributeFilter(expression, expressType, attrFacet.AttributeName.SingleValue(), // TODO Check if we ever want to filter multiple Names
                attrFacet?.AttributeValue);
            return expression;
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
        public Expression BindFilterExpression(Expression baseExpression, IfcPropertyFacet psetFacet)
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
                // IsValid checks against a know list of all IFC Attributes
                throw new InvalidOperationException($"IFC Property Facet '{psetFacet?.PropertySetName}'.{psetFacet?.PropertyName} is not valid");
            }


            var expression = baseExpression;
            // When an Ifc Type has not yet been specified, we start with the RelDefinesByProperties
            // TODO: Types
            if (expression.Type.IsInterface && expression.Type.IsAssignableTo(typeof(IEntityCollection)))
            {

                var rootTypes = new[] { nameof(IfcRelDefinesByProperties) };

                IfcTypeFacet ifcFacet = new IfcTypeFacet
                {
                    IfcType = new ValueConstraint(NetTypeName.String)
                };
                foreach (var root in rootTypes)
                {
                    ifcFacet.IfcType.AcceptedValues?.Add(new ExactConstraint(root));
                }
                expression = BindFilterExpression(expression, ifcFacet);
            }

            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);
            var expressType = model.Metadata.ExpressType(collectionType);
            ValidateExpressType(expressType);

            expression = BindEqualPsetFilter(expression, expressType, psetFacet);
            return expression;
        }

        private Expression BindIfcType(Expression expression, ExpressType expressType)
        {
            var ofTypeMethod = ExpressionHelperMethods.EntityCollectionOfType;
            
            var entityTypeName = Expression.Constant(expressType.Name, typeof(string));
            var activate = Expression.Constant(true, typeof(bool));
            // call .OfType("IfcWall", true)
            expression = Expression.Call(expression, ofTypeMethod, entityTypeName, activate);   // TODO: switch to Generic sig

            // TODO: Is this required?
            // call .Cast<EntityType>()
            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(expressType.Type), expression);
            
            return expression;
        }

        private static void ValidateExpressType(ExpressType expressType)
        {
            // Exclude invalid schema items (including un-rooted entity types like IfcLabel)
            if (expressType == null || expressType.Properties.Count == 0)
            {
                throw new InvalidOperationException($"Invalid IFC Type '{expressType?.Name}'");
            }
        }

        private static void ValidateExpressTypes(IEnumerable<ExpressType> expressTypes)
        {
            foreach(var expressType in expressTypes)
            {
                ValidateExpressType(expressType);
            }
        }

        private Expression BindPredefinedTypeFilter(IfcTypeFacet ifcFacet, Expression expression, ExpressType expressType)
        {
            if (ifcFacet?.PredefinedType?.AcceptedValues?.Any() == false || 
                ifcFacet?.PredefinedType?.AcceptedValues?.FirstOrDefault()?.IsValid(ifcFacet.PredefinedType) == false) return expression;

            var propertyMeta = expressType.Properties.FirstOrDefault(p => p.Value.Name == "PredefinedType").Value;
            if(propertyMeta == null)
            {
                return expression;
            }
            var ifcAttributePropInfo = propertyMeta.PropertyInfo;
            var ifcAttributeValues = GetPredefinedTypes(ifcFacet);

            return BindEqualsAttributeFilter(expression, ifcAttributePropInfo, ifcFacet!.PredefinedType); ;

        }


        private static Expression BindEqualsAttributeFilter(Expression expression, ExpressType expressType,
            string ifcAttributeName, ValueConstraint constraint)
        {

            var propertyMeta = expressType.Properties.First(p => p.Value.Name == ifcAttributeName).Value;
            if(propertyMeta == null)
            {
                throw new InvalidOperationException($"Property '{ifcAttributeName} not found on '{expressType.Name}'");
            }
            if(propertyMeta.EnumerableType != null)
            {
                throw new NotSupportedException("Cannot filter on collection properties");
            }
            return BindEqualsAttributeFilter(expression, propertyMeta.PropertyInfo, constraint);
        }

        private static Expression BindEqualsAttributeFilter(Expression expression,
            PropertyInfo ifcAttributePropInfo, ValueConstraint constraint)
        {
            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);

            var constraints = constraint.AcceptedValues;

            // call .Cast<EntityType>()
            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(collectionType), expression);

            if(constraints.Any() == false)
            {
                return expression;
            }

            // IEnumerable.Where<TEntity>(...)
            var whereMethod = ExpressionHelperMethods.EnumerableWhereGeneric.MakeGenericMethod(collectionType);

            // build lambda param 'ent => ...'
            ParameterExpression ifcTypeParam = Expression.Parameter(collectionType, "ent");

            // build 'ent.AttributeName'
            Expression nameProperty = Expression.Property(ifcTypeParam, ifcAttributePropInfo);

            var propType = ifcAttributePropInfo.PropertyType;
            var isNullWrapped = TypeHelper.IsNullable(propType);
            var underlyingType = isNullWrapped ? Nullable.GetUnderlyingType(propType) : propType;
            
            Expression querybody = Expression.Empty();
            
            bool applyOr = false;
            foreach (var ifcAttributeValue in constraints)
            {
                Expression rightExpr;
                
                switch (ifcAttributeValue)
                {
                    case ExactConstraint e:

                        string exactValue = e.Value;
                        // Get the Constant
                        rightExpr = BuildAttributeValueConstant(isNullWrapped, underlyingType, exactValue);
                        nameProperty = SetAttributeProperty(nameProperty, underlyingType);
                        // Binding Equals(x,y)
                        rightExpr = Expression.Equal(nameProperty, rightExpr);
                        break;

                    case PatternConstraint p:
                        // Build a query that builds an expression that delegates to XIDS's IsSatisfied regex method.
                        // model.Instances.OfType<IIfcWall>().Where(ent => patternconstraint.IsSatisfiedBy(w.Name.ToString(), <AttributeValue>, true, null));
                        //                                                 instance,         methodIn new[]{rightExpr, constraintExpr,   case, logger }
                        var isSatisfiedMethod = ExpressionHelperMethods.IdsValidationIsSatisifiedMethod;
                        // Get Property: entity.<attribute>
                        rightExpr = BuildAttributeValueRegexPredicate(nameProperty, isNullWrapped, underlyingType, p);
                        var constraintExpr = Expression.Constant(constraint, typeof(ValueConstraint));
                        var caseInsensitive = Expression.Constant(true, typeof(bool));
                        var loggerExpr = Expression.Constant(null, typeof(ILogger));
                        var instanceExpr = Expression.Constant(p, typeof(PatternConstraint));
                        rightExpr = Expression.Call(instanceExpr, isSatisfiedMethod, new[] { rightExpr, constraintExpr, caseInsensitive, loggerExpr });
                        
                        break;
                    case StructureConstraint s:
                    case RangeConstraint r:
                        throw new NotSupportedException(ifcAttributeValue.GetType().Name);
                        
                    default:
                        throw new NotImplementedException(ifcAttributeValue.GetType().Name);
                }

                // Or the expressions on subsequent iterations.
                if (applyOr)
                {
                    querybody = Expression.Or(querybody, rightExpr);
                }
                else
                {
                    querybody = rightExpr;
                    applyOr = true;
                }
            }

            // Build Lambda expression for filter predicate (Func<T,bool>)
            var filterExpression = Expression.Lambda(querybody, ifcTypeParam);

            // Bind Lambda to Where method
            return Expression.Call(null, whereMethod, new[] { expression, filterExpression });
           
        }

        private static Expression BuildAttributeValueRegexPredicate(Expression nameProperty, bool isNullWrapped, Type underlyingType, PatternConstraint p)
        {
            Expression queryValue;

            if (TypeHelper.IsCollection(underlyingType))
            {
                throw new NotSupportedException("Collections not supported");
            }
            // Unpack simple objects for string comparisons

            else if (underlyingType == typeof(string))
            {
                queryValue = nameProperty;
            }
            else if (underlyingType == typeof(IfcLabel) ||
                underlyingType == typeof(IfcText) ||
                underlyingType == typeof(IfcGloballyUniqueId) ||
                // TODO: Other primitives
                underlyingType.IsEnum
                )
            {
                // Call ToString on these primitives. HACK to avoid handling Nullables but good enough for Regex. Should got to Value (object)
                queryValue =  Expression.Call(nameProperty, nameof(Object.ToString), typeArguments: null, arguments: null);
            }
            else
            {
                throw new NotImplementedException($"Filtering on Ifc type {underlyingType.Name} not implemented");
            }


            return queryValue;
        }

        private static Expression SetAttributeProperty(Expression nameProperty, Type underlyingType)
        {
            if (underlyingType!.IsEnum)
            {
                // HACK: Use ToString rather than convert Predefined to correct Enum type.
                nameProperty = Expression.Call(nameProperty, nameof(Object.ToString), typeArguments: null, arguments: null);
            }

            return nameProperty;
        }

        private static Expression BuildAttributeValueConstant(bool isNullWrapped, Type? underlyingType, string ifcAttributeValue)
        {
            Expression queryValue;
            if (TypeHelper.IsCollection(underlyingType))
            {
                throw new NotSupportedException("Collections not supported");
            }
            // Wrap simple navigation objects to use built-in equality operators
            else if (underlyingType == typeof(IfcLabel))
            {
                var val = new IfcLabel(ifcAttributeValue);
                queryValue = Expression.Constant(val, typeof(IfcLabel));
            }
            else if (underlyingType == typeof(IfcText))
            {
                var val = new IfcText(ifcAttributeValue);
                queryValue = Expression.Constant(val, typeof(IfcText));
            }
            else if (underlyingType == typeof(IfcGloballyUniqueId))
            {
                var val = new IfcGloballyUniqueId(ifcAttributeValue);
                queryValue = Expression.Constant(val, typeof(IfcGloballyUniqueId));
            }
            // TODO: Other primitives
            else if (underlyingType.IsEnum)
            {
                // And use ToString upstream
                queryValue = Expression.Constant(ifcAttributeValue.ToUpperInvariant());
            }
            else if (underlyingType == typeof(string))
            {
                queryValue = Expression.Constant(ifcAttributeValue);
            }
            else
            {
                throw new NotImplementedException($"Filtering on Ifc type {underlyingType.Name} not implemented");
            }
            // Wrap when comparing to Nullable
            if (isNullWrapped && !TypeHelper.IsNullable(queryValue.Type))
            {
                queryValue = Expression.Convert(queryValue, TypeHelper.ToNullable(queryValue.Type));
            }

            return queryValue;
        }

        private  Expression BindEqualPsetFilter(Expression expression, ExpressType expressType, IfcPropertyFacet psetFacet)
        {
            if (psetFacet is null)
            {
                throw new ArgumentNullException(nameof(psetFacet));
            }
            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);

            //var constraints = constraint.AcceptedValues;

            // call .Cast<EntityType>()
            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(collectionType), expression);

            if (psetFacet?.PropertySetName?.AcceptedValues?.Any() == false ||
                psetFacet?.PropertyName?.AcceptedValues?.Any() == false)
            {
                return expression;
            }

            var psetName = psetFacet.PropertySetName.SingleValue();
            var propName = psetFacet.PropertyName.SingleValue();
            var propValue = psetFacet.PropertyValue.SingleValue();

            var psetNameExpr = Expression.Constant(psetName, typeof(string));
            var propNameExpr = Expression.Constant(propName, typeof(string));
            var propValExpr = Expression.Constant(propValue, typeof(string));
            // Expression we're building
            // var psetRelDefines = model.Instances.OfType<IIfcRelDefinesByProperties>();
            // var entities = IfcExtensions.GetIfcPropertySingleValues(psetRelDefines, psetName, propName, propValue);


            var propsMethod = ExpressionHelperMethods.EnumerableIfcPropertySinglePropsValue;

            return Expression.Call(null, propsMethod, new[] { expression, psetNameExpr, propNameExpr, propValExpr });

            /*



            // IEnumerable.Where<TEntity>(...)
            var whereMethod = ExpressionHelperMethods.EnumerableWhereGeneric.MakeGenericMethod(collectionType);

            // build lambda param 'ent => ...'
            ParameterExpression ifcTypeParam = Expression.Parameter(collectionType, "ent");
            
            // build 'ent.AttributeName'
            Expression nameProperty = Expression.Property(ifcTypeParam, ifcAttributePropInfo);

            var propType = ifcAttributePropInfo.PropertyType;
            var isNullWrapped = TypeHelper.IsNullable(propType);
            var underlyingType = isNullWrapped ? Nullable.GetUnderlyingType(propType) : propType;

            Expression querybody = Expression.Empty();

            bool applyOr = false;
            foreach (var ifcAttributeValue in constraints)
            {
                Expression rightExpr;

                switch (ifcAttributeValue)
                {
                    case ExactConstraint e:

                        string exactValue = e.Value;
                        // Get the Constant
                        rightExpr = BuildAttributeValueConstant(isNullWrapped, underlyingType, exactValue);
                        nameProperty = SetAttributeProperty(nameProperty, underlyingType);
                        // Binding Equals(x,y)
                        rightExpr = Expression.Equal(nameProperty, rightExpr);
                        break;

                    case PatternConstraint p:
                        // Build a query that builds an expression that delegates to XIDS's IsSatisfied regex method.
                        // model.Instances.OfType<IIfcWall>().Where(ent => patternconstraint.IsSatisfiedBy(w.Name.ToString(), <AttributeValue>, true, null));
                        //                                                 instance,         methodIn new[]{rightExpr, constraintExpr,   case, logger }
                        var isSatisfiedMethod = ExpressionHelperMethods.IdsValidationIsSatisifiedMethod;
                        // Get Property: entity.<attribute>
                        rightExpr = BuildAttributeValueRegexPredicate(nameProperty, isNullWrapped, underlyingType, p);
                        var constraintExpr = Expression.Constant(constraint, typeof(ValueConstraint));
                        var caseInsensitive = Expression.Constant(true, typeof(bool));
                        var loggerExpr = Expression.Constant(null, typeof(ILogger));
                        var instanceExpr = Expression.Constant(p, typeof(PatternConstraint));
                        rightExpr = Expression.Call(instanceExpr, isSatisfiedMethod, new[] { rightExpr, constraintExpr, caseInsensitive, loggerExpr });

                        break;
                    case StructureConstraint s:
                    case RangeConstraint r:
                        throw new NotSupportedException(ifcAttributeValue.GetType().Name);

                    default:
                        throw new NotImplementedException(ifcAttributeValue.GetType().Name);
                }

                // Or the expressions on subsequent iterations.
                if (applyOr)
                {
                    querybody = Expression.Or(querybody, rightExpr);
                }
                else
                {
                    querybody = rightExpr;
                    applyOr = true;
                }
            }

            // Build Lambda expression for filter predicate (Func<T,bool>)
            var filterExpression = Expression.Lambda(querybody, ifcTypeParam);

            // Bind Lambda to Where method
            return Expression.Call(null, whereMethod, new[] { expression, filterExpression });
            */
            //return expression;
        }



        private IEnumerable<ExpressType> GetExpressTypes(IfcTypeFacet ifcFacet)
        {
            if (ifcFacet?.IfcType?.AcceptedValues?.Any() == false)
            {
                yield break;
            }
            foreach (var ifcTypeConstraint in ifcFacet!.IfcType!.AcceptedValues ?? default)
            {
                switch (ifcTypeConstraint)
                {
                    case ExactConstraint e:
                        string ifcTypeName = e.Value;
                        yield return model.Metadata.ExpressType(ifcTypeName.ToUpperInvariant());
                        break;
                    case PatternConstraint p:
                        foreach(var type in model?.Metadata?.Types())
                        {
                            if(p.IsSatisfiedBy(type.Name, ifcFacet.IfcType, ignoreCase:true))
                            {
                                yield return type;
                            }
                        }
                        break;
                    case RangeConstraint r:
                    case StructureConstraint s:

                    default:
                        throw new NotImplementedException(ifcTypeConstraint.GetType().Name);
                }
            }

        }

        private static IEnumerable<IValueConstraintComponent> GetPredefinedTypes(IfcTypeFacet ifcFacet)
        {
            return ifcFacet?.PredefinedType?.AcceptedValues ?? Enumerable.Empty<IValueConstraintComponent>();
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
            var entity = model.Instances[entityLabel];

            IIfcPropertySingleValue psetValue;
            if (entity is IIfcTypeObject type)
            {
                psetValue = type.HasPropertySets.OfType<IIfcPropertySet>()
                    .Where(p => p.Name == psetName)
                    .SelectMany(p => p.HasProperties.Where(ps => ps.Name == propName)
                        .OfType<IIfcPropertySingleValue>())
                    .FirstOrDefault();
            }
            else if(entity is IIfcObject)
            {
                psetValue = model.Instances.OfType<IIfcRelDefinesByProperties>()
                    .Where(r => r.RelatedObjects.Any(o => o.EntityLabel == entityLabel))
                    .Where(r => r.RelatingPropertyDefinition is IIfcPropertySet ps && ps.Name == psetName)
                    .SelectMany(p => ((IIfcPropertySet)p.RelatingPropertyDefinition)
                        .HasProperties.Where(ps => ps.Name == propName)
                        .OfType<IIfcPropertySingleValue>())
                    .FirstOrDefault();
            }
            else
            {
                return null;
            }

            return psetValue?.NominalValue;

        }

        public string GetPredefinedType(IPersistEntity entity)
        {
            var expressType = model.Metadata.ExpressType(entity.GetType());
            var propertyMeta = expressType.Properties.FirstOrDefault(p => p.Value.Name == "PredefinedType").Value;
            if (propertyMeta == null)
            {
                return string.Empty;
            }
            var ifcAttributePropInfo = propertyMeta.PropertyInfo;
            var value = ifcAttributePropInfo.GetValue(entity)?.ToString();
            if(value == null && entity is IIfcObject entObj)
            {
                if(entObj.IsTypedBy?.Any() == true)
                {
                    return GetPredefinedType(entObj.IsTypedBy.First().RelatingType);
                }
            }
            if(value == "USERDEFINED")
            {
                if(entity is IIfcObject obj)
                    value = obj.ObjectType.Value;
                else if (entity is IIfcElementType type)
                    value = type.ElementType.Value;
                else if (entity is IIfcTypeProcess process)
                    value = process.ProcessType.Value;
            }

            return value;
        }
    }
}
