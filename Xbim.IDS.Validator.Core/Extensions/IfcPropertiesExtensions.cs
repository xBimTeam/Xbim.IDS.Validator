using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Extensions
{
    /// <summary>
    /// Extension methods to help build xbim queries for <see cref="IfcPropertyFacet"/>
    /// </summary>
    internal static class IfcPropertiesExtensions
    {
        /// <summary>
        /// Gets all <see cref="IIfcObjectDefinition"/>s defined by the propertyset and name
        /// </summary>
        /// <param name="relDefines"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        public static IEnumerable<IIfcObjectDefinition> GetIfcObjectsWithProperties(this IEnumerable<IIfcRelDefinesByProperties> relDefines,
            IfcPropertyFacet facet)
        {
            if (relDefines is null)
            {
                throw new ArgumentNullException(nameof(relDefines));
            }

            return relDefines.FilterByPropertyFacet(facet)
                    .SelectMany(r => r.RelatedObjects);
        }

        /// <summary>
        /// Filters a set of <see cref="IIfcObject"/> entries to those satisifying the property facet
        /// </summary>
        /// <param name="ent"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        public static IEnumerable<IIfcObject> WhereAssociatedWithProperty(this IEnumerable<IIfcObject> ent, IfcPropertyFacet facet)
        {
            return ent.Where(e => e.IsDefinedBy.OfType<IIfcRelDefinesByProperties>().FilterByPropertyFacet(facet).Any()
            ||
            // Or we inherit from the type
            e.IsTypedBy.Any(r => r.RelatingType.HasPropertySets.OfType<IIfcPropertySet>().FilterByPropertyFacet(facet).Any()));
        }

        /// <summary>
        /// Filters a set of <see cref="IIfcTypeObject"/> entries to those satisifying the property facet
        /// </summary>
        /// <param name="ent"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        public static IEnumerable<IIfcTypeObject> WhereAssociatedWithProperty(this IEnumerable<IIfcTypeObject> ent, IfcPropertyFacet facet)
        {
            return ent.Where(e => e.HasPropertySets.OfType<IIfcPropertySet>().FilterByPropertyFacet(facet).Any());
        }

        /// <summary>
        /// Unwraps a <see cref="IIfcPhysicalQuantity"/> value
        /// </summary>
        /// <param name="quantity"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static IIfcValue? UnwrapQuantity(this IIfcPhysicalQuantity quantity)
        {
            if (quantity is IIfcQuantityCount c)
                return c.CountValue;
            if (quantity is IIfcQuantityArea area)
                return area.AreaValue;
            else if (quantity is IIfcQuantityLength l)
                return l.LengthValue;
            else if (quantity is IIfcQuantityVolume v)
                return v.VolumeValue;
            if (quantity is IIfcQuantityWeight w)
                return w.WeightValue;
            if (quantity is IIfcQuantityTime t)
                return t.TimeValue;
            if (quantity is IIfcPhysicalComplexQuantity)
                return default;

            // TODO: Check other Physical Quantities
            throw new NotImplementedException(quantity.GetType().Name);
        }


        /// <summary>
        /// Applies facet filter to the supplied <see cref="IIfcRelDefinesByProperties"/> set
        /// </summary>
        /// <param name="relDefines"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private static IEnumerable<IIfcRelDefinesByProperties> FilterByPropertyFacet(this IEnumerable<IIfcRelDefinesByProperties> relDefines,
            IfcPropertyFacet facet)
        {

            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }
            return relDefines.Select(r => r.RelatingPropertyDefinition)
                .OfType<IIfcPropertySetDefinition>()
                .FilterByPropertyFacet(facet)
                .SelectMany(p => p.DefinesOccurrence);



        }


        /// <summary>
        /// Applies facet filter to the supplied <see cref="IIfcPropertySet"/> set
        /// </summary>
        /// <param name="relDefines"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private static IEnumerable<IIfcPropertySetDefinition> FilterByPropertyFacet(this IEnumerable<IIfcPropertySetDefinition> relDefines,
           IfcPropertyFacet facet)
        {

            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }

            // Try Psets first
            var psets = relDefines
                .Where(psd => facet!.PropertySetName?.IsSatisfiedBy(psd.Name?.Value, true) == true)
                .Where(psd => psd is IIfcPropertySet ps &&
                    (
                        // Single Values
                        ps.HasProperties.OfType<IIfcPropertySingleValue>().Where(psv =>
                        facet!.PropertyName?.IsSatisfiedBy(psv.Name.Value, true) == true &&
                        (
                            facet.PropertyValue?.HasAnyAcceptedValue() != true ||
                            facet!.PropertyValue?.IsSatisfiedBy(psv.NominalValue.Value, true) == true)
                        ).Any()
                        // Bounded Values
                        || ps.HasProperties.OfType<IIfcPropertyBoundedValue>().Where(pbv =>
                        facet!.PropertyName?.IsSatisfiedBy(pbv.Name.Value, true) == true &&
                        (
                            facet.PropertyValue?.HasAnyAcceptedValue() != true ||
                            facet!.PropertyValue?.IsSatisfiedBy(pbv.UpperBoundValue.Value, true) == true ||
                            facet!.PropertyValue?.IsSatisfiedBy(pbv.LowerBoundValue.Value, true) == true ||
                            facet!.PropertyValue?.IsSatisfiedBy(pbv.SetPointValue.Value, true) == true
                        )).Any()
                        // Enum Values
                        || ps.HasProperties.OfType<IIfcPropertyEnumeratedValue>().Where(pe =>
                        facet!.PropertyName?.IsSatisfiedBy(pe.Name.Value, true) == true &&
                        (
                            facet.PropertyValue?.HasAnyAcceptedValue() != true ||
                            pe.EnumerationValues.Any(v => facet!.PropertyName?.IsSatisfiedBy(v.Value, true) == true)
                        )).Any()
                        // List Values
                        || ps.HasProperties.OfType<IIfcPropertyListValue>().Where(pl =>
                        facet!.PropertyName?.IsSatisfiedBy(pl.Name.Value, true) == true &&
                        (
                            facet.PropertyValue?.HasAnyAcceptedValue() != true ||
                            pl.ListValues.Any(v => facet!.PropertyName?.IsSatisfiedBy(v.Value, true) == true)
                        )).Any()
                        // Table Values
                        || ps.HasProperties.OfType<IIfcPropertyTableValue>().Where(ptv =>
                        facet!.PropertyName?.IsSatisfiedBy(ptv.Name.Value, true) == true &&
                        (
                            facet.PropertyValue?.HasAnyAcceptedValue() != true ||
                            ptv.DefinedValues.Any(v => facet!.PropertyName?.IsSatisfiedBy(v.Value, true) == true) ||
                            ptv.DefiningValues.Any(v => facet!.PropertyName?.IsSatisfiedBy(v.Value, true) == true)
                        )).Any()
                    ));

            // Append Quantities
            var quants = relDefines
                .Where(r => facet!.PropertySetName?.IsSatisfiedBy(r.Name?.Value, true) == true)
                .Where(ps => ps is IIfcElementQuantity eq && eq.Quantities.
                    Where(q =>
                    facet!.PropertyName?.IsSatisfiedBy(q.Name.Value, true) == true &&
                    (facet.PropertyValue?.HasAnyAcceptedValue() != true ||
                        PhysicalQuantitySatisifies(facet.PropertyValue, q))
                    ).Any());

            return psets.Concat(quants);

        }

        private static bool PhysicalQuantitySatisifies(ValueConstraint propertyValue, IIfcPhysicalQuantity q)
        {
            return propertyValue.IsSatisfiedBy(q.UnwrapQuantity()?.Value) == true;
        }

    }
}
