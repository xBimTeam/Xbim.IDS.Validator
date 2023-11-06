using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.IDS.Validator.Core.Extensions;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;
using static Xbim.InformationSpecifications.RequirementCardinalityOptions;

namespace Xbim.IDS.Validator.Core.Binders
{

    public class IfcTypeFacetBinder : FacetBinderBase<IfcTypeFacet>
    {
        private readonly ILogger<IfcTypeFacetBinder> logger;

        public IfcTypeFacetBinder(BinderContext binderContext, ILogger<IfcTypeFacetBinder> logger) : base(binderContext, logger)
        {
            this.logger = logger;
        }


        /// <summary>
        /// Builds expression filtering on an IFC Type Facet
        /// </summary>
        /// <param name="baseExpression"></param>
        /// <param name="ifcFacet"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public override Expression BindSelectionExpression(Expression baseExpression, IfcTypeFacet ifcFacet)
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
            var expression = baseExpression;
            if (expression.Type.IsInterface && !typeof(IEntityCollection).IsAssignableFrom(expression.Type))
            {
                throw new NotSupportedException("Expected an unfiltered set of Instances");
            }

            var selectionCriteria = BuildSelectionCriteria(ifcFacet);
            if(!ExpressTypesAreValid(selectionCriteria.Select(e => e.ElementExpressType)))
            {
                var types = ifcFacet.IfcType.ToString();
                logger.LogWarning("Unexpected IFC Type: {ifcTypes} for schema {ifcSchema}", types, Model.SchemaVersion);
                
                throw new InvalidOperationException($"Invalid IFC Type '{types}' for {Model.SchemaVersion}" );
            }

           
            bool doConcat = false;
            foreach (var selection in selectionCriteria)
            {
                var rightExpr = baseExpression;
                rightExpr = BindIfcExpressType(rightExpr, selection.ElementExpressType, ifcFacet.IncludeSubtypes);
                if(selection.DefiningExpressType != null)
                {
                    rightExpr = BindDefiningType(rightExpr, selection);
                }
                if (ifcFacet.PredefinedType != null)
                    rightExpr = BindPredefinedTypeFilter(ifcFacet, rightExpr, selection);


                // Union to main expression.
                if (doConcat)
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

        public override Expression BindWhereExpression(Expression baseExpression, IfcTypeFacet facet)
        {
            // A real edge case, since we always try to start with a Ifc Type. 
            // e.g. Select all items with materials 'wood', where IfcType is IfcDoor - we should filter the entities from first predicate
            // But we reverse the predicate upstream as it's likely more efficient that way anyway
            throw new NotImplementedException("Filtering by IfcType after initial selection not implemented");
        }

        public override void ValidateEntity(IPersistEntity item, IfcTypeFacet f, Cardinality cardinality, IdsValidationResult result)
        {
            if (f is null)
            {
                throw new ArgumentNullException(nameof(f));
            }

            var ctx = CreateValidationContext(cardinality, f);
            var entityType = Model.Metadata.ExpressType(item);
            if (entityType == null)
            {
                result.FailWithError(ValidationMessage.Failure(ctx, fn => fn.IfcType!, null, "Invalid IFC Type", item)); 
                return;
            }
            var actualEntityType = entityType.Name.ToUpperInvariant();
            var currentEntityType = entityType;

            
            // We can't easily get IfcType Subtypes since the constraint could be complex
            // Instead when IncludeSubtypes = true, we get the supertypes and see if any of them satisfy
            while(currentEntityType != null)
            {
                var actualName = currentEntityType?.Name.ToUpperInvariant();
                if (f.IfcType.ExpectationIsSatisifedBy(actualName, ctx, logger))
                {
                    result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.IfcType!, actualName, "Correct IFC Type", item));
                    break;
                }
                else
                {
                    if (f!.IncludeSubtypes && currentEntityType!.SuperType != null)
                    {
                        // Get entity's supertype and test that
                        currentEntityType = currentEntityType?.SuperType;
                    } 
                    else
                    {
                        result.Fail(ValidationMessage.Failure(ctx, fn => fn.IfcType!, actualEntityType, "IFC Type incorrect", item));
                        break;
                    }
                }
            }
            
           
            if (f?.PredefinedType?.HasAnyAcceptedValue() == true)
            {
                var preDefValue = GetPredefinedType(item);
                if (f!.PredefinedType.ExpectationIsSatisifedBy(preDefValue, ctx, logger))
                {
                    result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.PredefinedType!, preDefValue, "Correct Predefined Type", item));
                }
                else
                {
                    result.Fail(ValidationMessage.Failure(ctx, fn => fn.PredefinedType!, preDefValue, "Predefined Type incorrect", item));
                }
            }
        }

        /// <summary>
        /// Given an <see cref="IfcTypeFacet"/> determines the xbim Express types to select matching the constraints. 
        /// </summary>
        /// <remarks>Also handles edge-case mapping between schemas where types may be new or deprecated</remarks>
        /// <param name="ifcFacet"></param>
        /// <returns></returns>
        private IEnumerable<EntitySelectionCriteria> BuildSelectionCriteria(IfcTypeFacet ifcFacet)
        {
            if (ifcFacet?.IfcType?.AcceptedValues?.Any() == false)
            {
                yield break;
            }

            if(ifcFacet?.IfcType?.IsSingleExact(out string? ifcTypeName) == true)
            {
                // Optimise for the typical scenario
                var metaData = Model.Metadata.ExpressType(ifcTypeName.ToUpperInvariant());
                if(metaData != null) 
                {
                    // Found in Schema
                    yield return new EntitySelectionCriteria(metaData);
                }
                else
                {
                    
                    // Check for direct subtitutes E.g. IfcDoorStyle => IfcDoorType
                    var equivalent = SchemaTypeMap.GetSchemaEquivalent(Model, ifcTypeName.ToUpperInvariant());
                    if (equivalent != null)
                    {
                        metaData = Model.Metadata.ExpressType(equivalent.Name.ToUpperInvariant());
                        yield return new EntitySelectionCriteria(metaData);

                    }

                    // Attempt subtitution by inference. E.g. 2x3 IfcAirTerminal = FlowTerminals where defined AirTerminalType
                    // E.g. Handle https://github.com/buildingSMART/IDS/issues/116
                    var inferred = SchemaTypeMap.InferSchemaForEntity(Model, ifcTypeName.ToUpperInvariant());
                    if (inferred != null)
                    {
                        var element = Model.Metadata.ExpressType(inferred.ElementType.Name.ToUpperInvariant());
                        var type = Model.Metadata.ExpressType(inferred.DefiningType.Name.ToUpperInvariant());
                        yield return new EntitySelectionCriteria(element, type);
                    }
                }
               
            }
            else
            {
                if (Model?.Metadata?.Types() == null) yield break;
                // It's an enum, Regex, Range or Structure
                // We don't support inference for these more complex scenarios
                var types = Model?.Metadata?.Types() ?? Enumerable.Empty<ExpressType>();
                foreach (var type in types)
                {
                    if (ifcFacet?.IfcType?.IsSatisfiedBy(type.Name, true) == true)
                    {
                        yield return new EntitySelectionCriteria(type!);
                    }
                }
            }
        }

        // cloned from Attributes Binder as we need to special case - e.g. for Types
        private Expression BindPredefinedTypeFilter(IfcTypeFacet ifcFacet, Expression expression, EntitySelectionCriteria selection)
        {
            if (ifcFacet?.PredefinedType?.AcceptedValues?.Any() != true ||
                ifcFacet?.PredefinedType?.AcceptedValues?.FirstOrDefault()?.IsValid(ifcFacet.PredefinedType) != true) return expression;


            // Intent: match on PredefinedType or ObjectType on the instance (where present).
            // Otherwise we need check the equivalent fields on the Type - which is non-trivial as we don't know the .NET runtime type
            // We have a rare edge case we do know the Type, when we're infering instance from the Type. E.g. Ifc2x3 AirTerminal use case
            var objectTypeMetadata = GetMatchingProperty(selection.ElementExpressType, nameof(IIfcObject.ObjectType));
            var pdtMetadata = GetMatchingProperty(selection.ElementExpressType, "PredefinedType");
            //var typeObjectTypeMetadata = GetMatchingProperty(selection.DefiningExpressType, nameof(IIfcElementType.ElementType));
            //var typePdtMetadata = GetMatchingProperty(selection.DefiningExpressType, "PredefinedType");
            var instanceAttributes = new List<PropertyInfo>();
            //var typeAttributes = new List<PropertyInfo>();
            if (pdtMetadata != null) instanceAttributes.Add(pdtMetadata.PropertyInfo);
            if (objectTypeMetadata != null) instanceAttributes.Add(objectTypeMetadata.PropertyInfo);
            //if (typePdtMetadata != null) typeAttributes.Add(typePdtMetadata.PropertyInfo);
            //if (typeObjectTypeMetadata != null) typeAttributes.Add(typeObjectTypeMetadata.PropertyInfo);

            if (!instanceAttributes.Any() /*&& !typeAttributes.Any()*/)
            {
                var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);
                return BindNotFound(expression, collectionType);
            }


            return BindPredefinedAttributeSelection(expression, ifcFacet!.PredefinedType, instanceAttributes);
            // TODO: fallback to any defined Type's PDT and ElementType

        }

        internal static Expression BindPredefinedAttributeSelection(Expression expression,
            ValueConstraint constraint, List<PropertyInfo> ifcAttributePropInfos)
        {
            if (constraint.AcceptedValues?.Any() != true)
            {
                return expression;
            }

            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);

            // Build IEnumerable<TEntity>().Where(t => ValueConstraintExtensions.SatisfiesConstraint(constraint, t.[AttributeName]))
            // TODO: ||
            //          ValueConstraintExtensions.SatisfiesConstraint(constraint, t.IsTypedBy?[AttributeName]))

            // build IEnumerable.Where<TEntity>(...)
            var whereMethod = ExpressionHelperMethods.EnumerableWhereGeneric.MakeGenericMethod(collectionType);

            // build lambda param 'ent => ...'
            ParameterExpression ifcTypeParam = Expression.Parameter(collectionType, "ent");

            var constraintExpr = Expression.Constant(constraint, typeof(ValueConstraint));

            // build t => ValueConstraintExtensions.SatisfiesConstraint(constraint, t.[AttributeName])
            Expression querybody = AttributeFacetBinder.BuildAttributeQuery(ifcAttributePropInfos.ToArray(), ifcTypeParam, constraintExpr);

            // Build Lambda expression for filter predicate (Func<T,bool>)
            var filterExpression = Expression.Lambda(querybody, ifcTypeParam);
            
            // Bind Lambda to Where method
            return Expression.Call(null, whereMethod, new[] { expression, filterExpression });
        }


        private string? GetPredefinedType(IPersistEntity entity)
        {
            string? value = null;
            var expressType = Model.Metadata.ExpressType(entity.GetType());
            var propertyMeta = expressType.Properties.FirstOrDefault(p => p.Value.Name == "PredefinedType").Value;

            if (propertyMeta != null)
            {
                var ifcAttributePropInfo = propertyMeta.PropertyInfo;
                value = ifcAttributePropInfo.GetValue(entity)?.ToString();
            }
            
            if (value == "USERDEFINED" || value == null)
            {
                if (entity is IIfcObject obj)
                    value = obj.ObjectType?.Value.ToString();
                else if (entity is IIfcElementType type)
                    value = type.ElementType?.Value.ToString();
                else if (entity is IIfcTypeProcess process)
                    value = process.ProcessType?.Value.ToString();
            }
            if (value == null && entity is IIfcObject entObj)
            {
                // Check the Type's PredefinedType if it has one
                if (entObj.IsTypedBy?.Any() == true)
                {
                    return GetPredefinedType(entObj.IsTypedBy.First().RelatingType);
                }
            }

            return value;
        }

        
    }

    /// <summary>
    /// Class representing the selection criteria to find applicable IFC Entity types
    /// </summary>
    public class EntitySelectionCriteria
    {
        public EntitySelectionCriteria(ExpressType elementType, ExpressType? definingType = null)
        {
            DefiningExpressType = definingType;
            ElementExpressType = elementType;
        }
        /// <summary>
        /// The IFC entity type to seek
        /// </summary>
        public ExpressType ElementExpressType { get; private set; }

        /// <summary>
        /// The optional IfcTypeObject the <see cref="ElementExpressType"/> should be DefinedBy
        /// </summary>
        /// <remarks>Typically used to handle IFC2x3 concepts of generic FlowTerminals defined by e.g. IfcAirTerminalType in the absence of the IfcAirTerminal product</remarks>
        public ExpressType? DefiningExpressType { get; private set; }

    }


}
