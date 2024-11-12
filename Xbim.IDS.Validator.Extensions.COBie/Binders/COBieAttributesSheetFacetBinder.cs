using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xbim.CobieExpress.Interfaces;
using Xbim.Common;
using Xbim.IDS.Validator.Common.Interfaces;
using Xbim.IDS.Validator.Core;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.IDS.Validator.Core.Extensions;
using Xbim.InformationSpecifications;
using static Xbim.InformationSpecifications.RequirementCardinalityOptions;

namespace Xbim.IDS.Validator.Extensions.COBie.Binders
{
    /// <summary>
    /// Applies a IDS <see cref="IfcPropertyFacet"/> filtering and verification to the equivalent Attributes sheet in COBie.
    /// </summary>
    public class COBieAttributesSheetFacetBinder : PsetFacetBinder
    {
        public COBieAttributesSheetFacetBinder(ILogger<PsetFacetBinder> logger, IValueMapper valueMapper) : base(logger, valueMapper)
        {
        }

        public override Expression BindSelectionExpression(Expression baseExpression, IfcPropertyFacet psetFacet)
        {
            throw new NotImplementedException("Selecting by COBIe Attributes to do");
            //return base.BindSelectionExpression(baseExpression, psetFacet);
        }

        public override Expression BindWhereExpression(Expression baseExpression, IfcPropertyFacet facet)
        {
            throw new NotImplementedException(message: "Filtering by COBIe Attributes to do");
            //return base.BindWhereExpression(baseExpression, facet);
        }

        public override void ValidateEntity(IPersistEntity item, IfcPropertyFacet facet, Cardinality cardinality, IdsValidationResult result)
        {
            var ctx = CreateValidationContext(cardinality, facet);

            // We ignore PropertySet as irrelevant to COBie

            var props = GetAttributesMatching(item, facet.PropertyName);
            if (props.Any())
            {
                foreach (var prop in props)
                {

                    var propValue = prop.Value;

                    object? value = MapValue(propValue, valueMapper);
                    bool isPopulated = IsValueRelevant(value);
                    var expectedPopulated = cardinality != Cardinality.Prohibited || !facet.PropertyValue!.IsNullOrEmpty();
                    if (isPopulated == expectedPopulated)
                    {
                        result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.PropertyName!, prop.Name, $"Attribute provided", prop));
                    }
                    if (ValueSatifiesConstraint(facet, value, ctx))
                    {
                        // TODO Consider Prohibited 
                        result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.PropertyValue!, value, $"Attribute Value matches", prop));
                    }
                    else
                    {
                        result.Fail(ValidationMessage.Failure(ctx, fn => fn.PropertyValue!, value, $"Invalid Attribute value in {prop.Name}", prop));
                    }
                }
            }
            else
            {
                // No Attribute found
                if (cardinality == Cardinality.Expected)
                {
                    result.Fail(ValidationMessage.Failure(ctx, fn => fn.PropertyName!, null, $"No Attribute row found", item));
                }
            }
        }

        private IEnumerable<ICobieAttribute> GetAttributesMatching(IPersistEntity entity, ValueConstraint constraint)
        {
            if (entity is ICobieAsset cobieObject)
            {
                return cobieObject.Attributes.Where(a => constraint.IsSatisfiedBy(a.Name, true, logger));
            }
            else
            {
                return Enumerable.Empty<ICobieAttribute>();
            }
        }
    }
}
