using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.IDS.Validator.Core.Extensions;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Binders
{
#nullable disable
    public class IfcTypeFacetBinder : FacetBinderBase<IfcTypeFacet>
    {

        public IfcTypeFacetBinder(IModel model): base(model)
        {
        }

        /// <summary>
        /// Builds expression filtering on an IFC Type Facet
        /// </summary>
        /// <param name="baseExpression"></param>
        /// <param name="ifcFacet"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public override Expression BindFilterExpression(Expression baseExpression, IfcTypeFacet ifcFacet)
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
            foreach (var expressType in expressTypes)
            {
                var rightExpr = baseExpression;
                rightExpr = BindIfcExpressType(rightExpr, expressType);
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

        public override void ValidateEntity(IPersistEntity item, FacetGroup requirement, ILogger logger, IdsValidationResult result, IfcTypeFacet f)
        {

            var entityType = Model.Metadata.ExpressType(item);
            if (entityType == null)
            {
                result.Messages.Add(ValidationMessage.Failure(f, fn => fn.IfcType!, null, "Invalid IFC Type", item));
            }
            var actual = entityType?.Name.ToUpperInvariant();

            if (f?.IfcType?.SatisfiesRequirement(requirement, actual, logger) == true)
            {
                result.Messages.Add(ValidationMessage.Success(f, fn => fn.IfcType!, actual, "Correct IFC Type", item));
            }
            else
            {
                result.Messages.Add(ValidationMessage.Failure(f!, fn => fn.IfcType!, actual, "IFC Type incorrect", item));
            }
            if (f?.PredefinedType?.HasAnyAcceptedValue() == true)
            {
                var preDefValue = GetPredefinedType(item);
                if (f!.PredefinedType.SatisfiesRequirement(requirement, preDefValue, logger) == true)
                {
                    result.Messages.Add(ValidationMessage.Success(f, fn => fn.PredefinedType!, actual, "Correct Predefined Type", item));
                }
                else
                {
                    result.Messages.Add(ValidationMessage.Failure(f, fn => fn.PredefinedType!, preDefValue, "Predefined Type incorrect", item));
                }
            }
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
                        yield return Model.Metadata.ExpressType(ifcTypeName.ToUpperInvariant());
                        break;
                    case PatternConstraint p:
                        foreach (var type in Model?.Metadata?.Types())
                        {
                            if (p.IsSatisfiedBy(type.Name, ifcFacet.IfcType, ignoreCase: true))
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

            return AttributeFacetBinder.BindEqualsAttributeFilter(expression, ifcAttributePropInfo, ifcFacet!.PredefinedType); ;

        }




        private static IEnumerable<IValueConstraintComponent> GetPredefinedTypes(IfcTypeFacet ifcFacet)
        {
            return ifcFacet?.PredefinedType?.AcceptedValues ?? Enumerable.Empty<IValueConstraintComponent>();
        }


        public string GetPredefinedType(IPersistEntity entity)
        {
            var expressType = Model.Metadata.ExpressType(entity.GetType());
            var propertyMeta = expressType.Properties.FirstOrDefault(p => p.Value.Name == "PredefinedType").Value;
            if (propertyMeta == null)
            {
                return string.Empty;
            }
            var ifcAttributePropInfo = propertyMeta.PropertyInfo;
            var value = ifcAttributePropInfo.GetValue(entity)?.ToString();
            if (value == null && entity is IIfcObject entObj)
            {
                if (entObj.IsTypedBy?.Any() == true)
                {
                    return GetPredefinedType(entObj.IsTypedBy.First().RelatingType);
                }
            }
            if (value == "USERDEFINED")
            {
                if (entity is IIfcObject obj)
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
