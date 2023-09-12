using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Binders
{

    public class IfcTypeFacetBinder : FacetBinderBase<IfcTypeFacet>
    {
        private readonly ILogger<IfcTypeFacetBinder> logger;

        public IfcTypeFacetBinder(BinderContext binderContext, ILogger<IfcTypeFacetBinder> logger) : base(binderContext)
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
            // So we can do case insensitive comparisons
            ifcFacet.IfcType.BaseType = NetTypeName.String;

            var expressTypes = GetExpressTypes(ifcFacet);
            if(!ExpressTypesAreValid(expressTypes))
            {
                var types = ifcFacet.IfcType.ToString();
                logger.LogWarning("Unexpected IFC Type: {ifcTypes} for schema {ifcSchema}", types, Model.SchemaVersion);
                throw new InvalidOperationException($"Invalid IFC Type '{types}' for {Model.SchemaVersion}" );
            }

            var expression = baseExpression;
            if (expression.Type.IsInterface && !typeof(IEntityCollection).IsAssignableFrom(expression.Type))
            {
                throw new NotSupportedException("Expected an unfiltered set of Instances");
            }
            bool doConcat = false;
            foreach (var expressType in expressTypes)
            {
                var rightExpr = baseExpression;
                rightExpr = BindIfcExpressType(rightExpr, expressType, ifcFacet.IncludeSubtypes);
                if (ifcFacet.PredefinedType != null)
                    rightExpr = BindPredefinedTypeFilter(ifcFacet, rightExpr, expressType);


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
            throw new NotImplementedException();
        }

        public override void ValidateEntity(IPersistEntity item, IfcTypeFacet f, RequirementCardinalityOptions requirement, IdsValidationResult result)
        {
            if (f is null)
            {
                throw new ArgumentNullException(nameof(f));
            }

            var ctx = CreateValidationContext(requirement, f);
            var entityType = Model.Metadata.ExpressType(item);
            if (entityType == null)
            {
                result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.IfcType!, null, "Invalid IFC Type", item));
            }
            var actualEntityType = entityType!.Name.ToUpperInvariant();
            var currentEntityType = entityType;

            
            // We can't easily get IfcType Subtypes since the constraint could be complex
            // Instead when IncludeSubtypes = true, we get the supertypes and see if any of them satisfy
            while(currentEntityType != null)
            {
                var actualName = currentEntityType?.Name.ToUpperInvariant();
                if (f?.IfcType?.IsSatisfiedBy(actualName, true, logger) == true)
                {
                    result.Messages.Add(ValidationMessage.Success(ctx, fn => fn.IfcType!, actualName, "Correct IFC Type", item));
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
                        result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.IfcType!, actualEntityType, "IFC Type incorrect", item));
                        break;
                    }
                }
            }
            
           
            if (f?.PredefinedType?.HasAnyAcceptedValue() == true)
            {
                var preDefValue = GetPredefinedType(item);
                if (f!.PredefinedType.IsSatisfiedBy(preDefValue, logger) == true)
                {
                    result.Messages.Add(ValidationMessage.Success(ctx, fn => fn.PredefinedType!, preDefValue, "Correct Predefined Type", item));
                }
                else
                {
                    result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.PredefinedType!, preDefValue, "Predefined Type incorrect", item));
                }
            }
        }


        private IEnumerable<ExpressType> GetExpressTypes(IfcTypeFacet ifcFacet)
        {
            if (ifcFacet?.IfcType?.AcceptedValues?.Any() == false)
            {
                yield break;
            }

            if(ifcFacet?.IfcType?.IsSingleExact(out string? ifcTypeName) == true)
            {
                // Optimise for the typical scenario
                yield return Model.Metadata.ExpressType(ifcTypeName.ToUpperInvariant());
            }
            else
            {
                if (Model?.Metadata?.Types() == null) yield break;
                // It's an enum, Regex, Range or Structure
                var types = Model?.Metadata?.Types() ?? Enumerable.Empty<ExpressType>();
                foreach (var type in types)
                {
                    if (ifcFacet?.IfcType?.IsSatisfiedBy(type.Name, true) == true)
                    {
                        yield return type!;
                    }
                }
            }
        }

        private Expression BindPredefinedTypeFilter(IfcTypeFacet ifcFacet, Expression expression, ExpressType expressType)
        {
            if (ifcFacet?.PredefinedType?.AcceptedValues?.Any() == false ||
                ifcFacet?.PredefinedType?.AcceptedValues?.FirstOrDefault()?.IsValid(ifcFacet.PredefinedType) == false) return expression;

            var propertyMeta = expressType.Properties.FirstOrDefault(p => p.Value.Name == "PredefinedType").Value;
            if (propertyMeta == null)
            {
                return expression;
            }
            var ifcAttributePropInfo = propertyMeta.PropertyInfo;
            var ifcAttributeValues = GetPredefinedTypes(ifcFacet);

            return AttributeFacetBinder.BindAttributeSelection(expression, ifcAttributePropInfo, ifcFacet!.PredefinedType); ;

        }




        private static IEnumerable<IValueConstraintComponent> GetPredefinedTypes(IfcTypeFacet ifcFacet)
        {
            return ifcFacet?.PredefinedType?.AcceptedValues ?? Enumerable.Empty<IValueConstraintComponent>();
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
}
