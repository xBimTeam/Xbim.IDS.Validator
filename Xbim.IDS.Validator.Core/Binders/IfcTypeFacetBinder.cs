﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

        public IfcTypeFacetBinder(ILogger<IfcTypeFacetBinder> logger) : base(logger)
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
                var preDefValue = GetPredefinedType(item, out bool isUserDefined);
                var satisifiedByAnyUserDefined = isUserDefined && f!.PredefinedType.IsSatisfiedBy("USERDEFINED");

                if (f!.PredefinedType.ExpectationIsSatisifedBy(preDefValue, ctx, logger))
                {
                    result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.PredefinedType!, preDefValue, "Correct Predefined Type", item));
                }
                else if(satisifiedByAnyUserDefined)
                {
                    result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.PredefinedType!, "USERDEFINED", "Is Userdefined Type", item));
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
                foreach(var item in GetSelectionCriteriaForTypeAndSchema(ifcTypeName))
                {
                    yield return item;
                }
            }
            else if(ifcFacet?.IfcType?.AcceptedValues.All(a => a is ExactConstraint) == true)
            {
                // Enums also handle typically
                foreach (var choice in ifcFacet!.IfcType!.AcceptedValues.OfType<ExactConstraint>())
                {
                    foreach (var item in GetSelectionCriteriaForTypeAndSchema(choice.Value))
                    {
                        yield return item;
                    }
                }
            }
            else
            {
                // It's a Regex, Range or Structure

                if (Model?.Metadata?.Types() == null) yield break;
                // We don't support inference for these more complex scenarios.
                // e.g. IFCTYPE matches IFCDUCT.* returning IFC2x3's IfcDuctSegment equivalent
                var types = (Model?.Metadata?.Types() ?? Enumerable.Empty<ExpressType>())
                    .Where(t => TypeHelper.ImplementsInterface<IPersistEntity>(t.Type));
                foreach (var type in types)
                {
                    if (ifcFacet?.IfcType?.IsSatisfiedBy(type.Name, true) == true)
                    {
                        yield return new EntitySelectionCriteria(type!);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the means of querying for an IFC type including approaches that provide backward compatible equivalents when
        /// newer schema types are be used. 
        /// </summary>
        /// <remarks>E.g. IfcDuctFitting in IFC2x3 = IfcFlowFitting AND DefinedBy(IfcDuctFittingType)</remarks>
        /// <param name="ifcTypeName"></param>
        /// <returns></returns>
        private IEnumerable<EntitySelectionCriteria> GetSelectionCriteriaForTypeAndSchema(string ifcTypeName)
        {

            var typeName = ifcTypeName.ToUpperInvariant();
            var metaData = Model.Metadata.ExpressType(typeName);
            if (metaData != null)
            {
                // Found in Schema
                if(TypeHelper.ImplementsInterface<IPersistEntity>(metaData.Type))
                {
                    yield return new EntitySelectionCriteria(metaData);
                }
                // else skip as it's not a rooted element type
            }
            else
            {
                // Type not explicitly not in this schema. Look for another means of querying

                // Attempt subtitution by inference. E.g. 2x3 IfcAirTerminal = FlowTerminals where defined AirTerminalType
                // E.g. Handle https://github.com/buildingSMART/IDS/issues/116
                var inferred = SchemaTypeMap.InferSchemaForEntity(Model, typeName);
                if (inferred != null)
                {
                    var element = Model.Metadata.ExpressType(inferred.ElementType.Name.ToUpperInvariant());
                    var type = Model.Metadata.ExpressType(inferred.DefiningType.Name.ToUpperInvariant());
                    yield return new EntitySelectionCriteria(element, type);
                }

                // Check for direct subtitutes E.g. IfcDoorStyle => IfcDoorType, BuildingElement => BuiltElement
                var equivalent = SchemaTypeMap.GetSchemaEquivalent(Model, typeName);
                if (equivalent != null)
                {
                    metaData = Model.Metadata.ExpressType(equivalent.Name.ToUpperInvariant());
                    yield return new EntitySelectionCriteria(metaData);

                }
            }
        }

        private Expression BindPredefinedTypeFilter(IfcTypeFacet ifcFacet, Expression expression, EntitySelectionCriteria selection)
        {
            if (ifcFacet?.PredefinedType?.AcceptedValues?.Any() != true ||
                ifcFacet?.PredefinedType?.AcceptedValues?.FirstOrDefault()?.IsValid(ifcFacet.PredefinedType) != true) return expression;


            // Expression we're building:
            // var entities = model.Instances.OfType<IfcObjectDefinition>();
            // var filteredresult = entities.WhereHasPredefinedType(facet));
            // or
            // var filteredresult =  IfcEntityTypeExtensions.WhereHasPredefinedType(entities, facet);
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);

            var typeFacetExpr = Expression.Constant(ifcFacet, typeof(IfcTypeFacet));

            var propsMethod = ExpressionHelperMethods.EnumerableWhereIfcTypeHasMatchingPredefinedType.MakeGenericMethod(collectionType);

            return Expression.Call(null, propsMethod, new[] { expression, typeFacetExpr });

        }

        private string? GetPredefinedType(IPersistEntity entity, out bool isUserDefined)
        {
            isUserDefined = false;
            if (entity is IIfcObjectDefinition o)
            {
                return IfcEntityTypeExtensions.GetPredefinedType(o, out isUserDefined);
            }
            return null;
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
