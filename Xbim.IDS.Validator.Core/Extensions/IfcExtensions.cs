using System.Linq;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Extensions
{
    internal static class IfcExtensions
    {
      

        /// <summary>
        /// Gets all <see cref="IIfcObjectDefinition"/>s defined by the propertyset and name
        /// </summary>
        /// <param name="relDefines"></param>
        /// <param name="psetName"></param>
        /// <param name="propName"></param>
        /// <returns></returns>
        public static IEnumerable<IIfcObjectDefinition> GetIfcObjectsWithProperties(this IEnumerable<IIfcRelDefinesByProperties> relDefines,
            IfcPropertyFacet facet)
        {
            // TODO: Update to Facet filter
            return relDefines.FilterByFacet(facet)
                    .SelectMany(r => r.RelatedObjects);
        }

        /// <summary>
        /// Selects all objects using a matching material
        /// </summary>
        /// <param name="relAssociates"></param>
        /// <param name="materialName"></param>
        /// <returns></returns>
        public static IEnumerable<IIfcObjectDefinition> GetIfcObjectsUsingMaterials(this IEnumerable<IIfcRelAssociatesMaterial> relAssociates, MaterialFacet materialFacet)
        {
            if (materialFacet is null)
            {
                throw new ArgumentNullException(nameof(materialFacet));
            }

            return relAssociates.FilterByFacet(materialFacet)
                    .SelectMany(r => r.RelatedObjects).OfType<IIfcObjectDefinition>();
        }


        /// <summary>
        /// Selects all objects using a matching classification
        /// </summary>
        /// <param name="relAssociates"></param>
        /// <param name="materialName"></param>
        /// <returns></returns>
        public static IEnumerable<IIfcObjectDefinition> GetIfcObjectsUsingClassification(this IEnumerable<IIfcRelAssociatesClassification> relAssociates, IfcClassificationFacet facet)
        {
            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }

            return relAssociates.FilterByFacet(facet)
                .SelectMany(r => r.RelatedObjects).OfType<IIfcObjectDefinition>();

        }

        /// <summary>
        /// Gets Materials associated to an entity that match the requirement
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="materialFacet"></param>
        /// <returns></returns>
        public static IEnumerable<IIfcMaterial> GetMaterialsForEntity(IIfcObjectDefinition obj, MaterialFacet materialFacet)
        {
            if (obj.Material is IIfcMaterial material && MatchesMaterial(material, materialFacet)) return new[] { material };
            if (obj.Material is IIfcMaterialList list) return list.Materials.Where(m => MatchesMaterial(m, materialFacet));

            if (obj.Material is IIfcMaterialLayerSet layerSet) return
                    layerSet.MaterialLayers.Select(ml => ml.Material).Where(m => MatchesMaterial(m, materialFacet))
                    .Union(layerSet.MaterialLayers.Where(ml => MatchesMaterial(ml, materialFacet)).Select(l => l.Material));
            if (obj.Material is IIfcMaterialLayerSetUsage layerusage) return layerusage.ForLayerSet.MaterialLayers.Select(ml => ml.Material).Where(m => MatchesMaterial(m, materialFacet));

            if (obj.Material is IIfcMaterialProfile profile && MatchesMaterial(profile.Material, materialFacet)) return new[] { profile.Material };
            if (obj.Material is IIfcMaterialProfileSet profileSet) return
                    profileSet.MaterialProfiles.Where(m => MatchesMaterial(m, materialFacet)).Select(mc => mc.Material)
                    .Union(profileSet.MaterialProfiles.Select(mc => mc.Material).Where(m => MatchesMaterial(m, materialFacet)));

            if (obj.Material is IIfcMaterialConstituent constituent && MatchesMaterial(constituent, materialFacet)) return new[] { constituent.Material };
            if (obj.Material is IIfcMaterialConstituentSet constituentSet) return
                    constituentSet.MaterialConstituents.Where(m => MatchesMaterial(m, materialFacet)).Select(mc => mc.Material)
                    .Union(constituentSet.MaterialConstituents.Select(mc => mc.Material).Where(m => MatchesMaterial(m, materialFacet)));

            return Enumerable.Empty<IIfcMaterial>();
        }



        /// <summary>
        /// Selects all classificationReferences matching criteria
        /// </summary>
        /// <param name="relAssociates"></param>
        /// <param name="materialName"></param>
        /// <returns></returns>
        public static IEnumerable<IIfcClassificationSelect> GetClassificationReferences(this IEnumerable<IIfcRelAssociatesClassification> relAssociates, IfcClassificationFacet facet)
        {
            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }
            return relAssociates.FilterByFacet(facet)
                .Select(r=> r.RelatingClassification).OfType<IIfcClassificationSelect>();

        }


        public static IEnumerable<IIfcObjectDefinition> WhereAssociatedWithClassification(this IEnumerable<IIfcObjectDefinition> ent, IfcClassificationFacet facet)
        {
            return ent.Where(e => e.HasAssociations.OfType<IIfcRelAssociatesClassification>().FilterByFacet(facet).Any());

        }

        public static IEnumerable<IIfcObjectDefinition> WhereAssociatedWithMaterial(this IEnumerable<IIfcObjectDefinition> ent, MaterialFacet facet)
        {
            return ent.Where(e => e.HasAssociations.OfType<IIfcRelAssociatesMaterial>().FilterByFacet(facet).Any());
        }

        public static IEnumerable<IIfcObject> WhereAssociatedWithProperty(this IEnumerable<IIfcObject> ent, IfcPropertyFacet facet)
        {
            return ent.Where(e => e.IsDefinedBy.OfType<IIfcRelDefinesByProperties>().FilterByFacet(facet).Any());
        }

        public static IEnumerable<IIfcTypeObject> WhereAssociatedWithProperty(this IEnumerable<IIfcTypeObject> ent, IfcPropertyFacet facet)
        {
            return ent.Where(e => e.HasPropertySets.OfType<IIfcPropertySet>().FilterByFacet(facet).Any());
        }


        private static IEnumerable<IIfcRelAssociatesMaterial> FilterByFacet(this IEnumerable<IIfcRelAssociatesMaterial> relAssociates, MaterialFacet facet)
        {
            // Note, keep in sync with GetMaterialsForEntity() above [or find a way to share the predicate]
            return relAssociates.Where(r =>
            
                // All permutations, where we match on Material.[Name|Category] and also equivalents on LayerSets, ProfileSets and ConsituentSets
                r.RelatingMaterial is IIfcMaterial m && MatchesMaterial(m, facet) ||
                r.RelatingMaterial is IIfcMaterialList l && l.Materials.Any(m => MatchesMaterial(m, facet)) ||
                r.RelatingMaterial is IIfcMaterialLayerSet layer && layer.MaterialLayers.Any(m => MatchesMaterial(m, facet)) ||
                r.RelatingMaterial is IIfcMaterialLayerSetUsage ls && ls.ForLayerSet.MaterialLayers
                    .Any(ml => MatchesMaterial(ml, facet) || MatchesMaterial(ml.Material, facet)) ||
                r.RelatingMaterial is IIfcMaterialProfile profile && MatchesMaterial(profile.Material, facet) ||
                r.RelatingMaterial is IIfcMaterialProfileSet profileSet && profileSet.MaterialProfiles
                    .Any(p => MatchesMaterial(p, facet) || MatchesMaterial(p.Material, facet)) ||
                r.RelatingMaterial is IIfcMaterialConstituent constituent && MatchesMaterial(constituent.Material, facet) ||
                r.RelatingMaterial is IIfcMaterialConstituentSet constituentSet && constituentSet.MaterialConstituents
                    .Any(c => MatchesMaterial(c, facet) || MatchesMaterial(c.Material, facet))
            );
        }

        private static IEnumerable<IIfcRelAssociatesClassification> FilterByFacet(this IEnumerable<IIfcRelAssociatesClassification> relAssociates, IfcClassificationFacet facet)
        {
            return relAssociates.Where(r =>
                (
                r.RelatingClassification is IIfcClassificationReference cr && HasMatchingIdentificationAncestor(cr, facet) &&
                cr.HasMatchingSytemAncestor(facet)
                ) 
                ||
                (   // Linked straight to a Classification. E.g. Project
                r.RelatingClassification is IIfcClassification cl2 && MatchesSystem(cl2, facet) && facet.Identification?.HasAnyAcceptedValue() != true
                )
            );
        }

        private static bool HasMatchingSytemAncestor(this IIfcClassificationReference reference, IfcClassificationFacet facet, HashSet<long>? ancestry = null)
        {
            if(ancestry == null)
            {
                ancestry = new HashSet<long>();
            }
            ancestry.Add(reference.EntityLabel);
            // Recursively walk up the Classification hierarchy to top. Maintain ancestry to shortcut if we hit a loop
            return reference.ReferencedSource is IIfcClassification cl && MatchesSystem(cl, facet) ||
                reference.ReferencedSource is IIfcClassificationReference parent && !ancestry.Contains(parent.EntityLabel) && parent.HasMatchingSytemAncestor(facet, ancestry);
        }


        internal static bool HasMatchingIdentificationAncestor(this IIfcClassificationReference reference, IfcClassificationFacet facet, HashSet<long>? ancestry = null)
        {
            if (facet.Identification == null) return true;
            if (ancestry == null)
            {
                ancestry = new HashSet<long>();
            }
            ancestry.Add(reference.EntityLabel);

            return (facet.Identification?.IsSatisfiedBy(reference.Identification?.Value, true) == true) ||
                // recurse up the ClassificationReference hierarchy. EF_20_25_30 => EF_20_25 => EF_20 
                reference.ReferencedSource is IIfcClassificationReference parent && !ancestry.Contains(parent.EntityLabel) && parent.HasMatchingIdentificationAncestor(facet, ancestry);
        }

        private static bool MatchesSystem(IIfcClassification classification, IfcClassificationFacet facet)
        {
            if (facet.ClassificationSystem == null) return true;

            return facet.ClassificationSystem?.IsSatisfiedBy(classification.Name.Value, true) == true;
        }


        private static bool MatchesMaterial(IIfcMaterial material, MaterialFacet facet)
        {
            if (facet.Value == null) return true;
            return facet.Value?.IsSatisfiedBy(material.Name.Value, true) == true || facet.Value?.IsSatisfiedBy(material.Category?.Value, true) == true;
        }

        private static bool MatchesMaterial(IIfcMaterialConstituent constituent, MaterialFacet facet)
        {
            if (facet.Value == null) return true;
            return facet.Value?.IsSatisfiedBy(constituent.Name?.Value, true) == true || facet.Value?.IsSatisfiedBy(constituent.Category?.Value, true) == true;
        }

        private static bool MatchesMaterial(IIfcMaterialLayer layer, MaterialFacet facet)
        {
            if (facet.Value == null) return true;
            return facet.Value?.IsSatisfiedBy(layer.Name?.Value, true) == true || facet.Value?.IsSatisfiedBy(layer.Category?.Value, true) == true;
        }

        private static bool MatchesMaterial(IIfcMaterialProfile profile, MaterialFacet facet)
        {
            if (facet.Value == null) return true;
            return facet.Value?.IsSatisfiedBy(profile.Name?.Value, true) == true || facet.Value?.IsSatisfiedBy(profile.Category?.Value, true) == true;
        }



        private static IEnumerable<IIfcRelDefinesByProperties> FilterByFacet(this IEnumerable<IIfcRelDefinesByProperties> relDefines,
            string psetName, string propName, string? propValue)
        {
            if (propValue == null)
            {
                return relDefines
                    .Where(r => r.RelatingPropertyDefinition is IIfcPropertySet ps && string.Equals(ps.Name, psetName, StringComparison.InvariantCultureIgnoreCase))
                    .Where(r => ((IIfcPropertySet)r.RelatingPropertyDefinition).HasProperties.Where(ps => string.Equals(ps.Name, propName, StringComparison.InvariantCultureIgnoreCase)).Any());
            }
            else
            {
                return relDefines
                    .Where(r => r.RelatingPropertyDefinition is IIfcPropertySet ps && string.Equals(ps.Name, psetName, StringComparison.InvariantCultureIgnoreCase))
                    .Where(r => ((IIfcPropertySet)r.RelatingPropertyDefinition)
                        .HasProperties.OfType<IIfcPropertySingleValue>().Where(ps =>
                        string.Equals(ps.Name, propName, StringComparison.InvariantCultureIgnoreCase) &&
                        string.Equals(ps.NominalValue.ToString(), propValue, StringComparison.InvariantCultureIgnoreCase)
                        ).Any());
            }
        }

        private static IEnumerable<IIfcRelDefinesByProperties> FilterByFacet(this IEnumerable<IIfcRelDefinesByProperties> relDefines,
            IfcPropertyFacet facet)
        {

            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }

            // Try Psets first
            // TODO: non PropertySingleValues
            var psets = relDefines
                .Where(r => r.RelatingPropertyDefinition is IIfcPropertySet ps && facet!.PropertySetName?.IsSatisfiedBy(ps.Name?.Value, true) == true)
                .Where(r => ((IIfcPropertySet)r.RelatingPropertyDefinition)
                    .HasProperties.OfType<IIfcPropertySingleValue>().Where(ps =>
                    facet!.PropertyName?.IsSatisfiedBy(ps.Name.Value, true) == true &&
                    (facet.PropertyValue?.HasAnyAcceptedValue() != true || facet!.PropertyValue?.IsSatisfiedBy(ps.NominalValue.Value, true) == true )
                    ).Any());

            // Append Quantities
            var quants = relDefines
                .Where(r => r.RelatingPropertyDefinition is IIfcElementQuantity eq && facet!.PropertySetName?.IsSatisfiedBy(eq.Name?.Value, true) == true)
                .Where(r => ((IIfcElementQuantity)r.RelatingPropertyDefinition)
                .Quantities.
                    Where(q =>
                    facet!.PropertyName?.IsSatisfiedBy(q.Name.Value, true) == true &&
                    (facet.PropertyValue?.HasAnyAcceptedValue() != true || 
                        PhysicalQuantitySatisifies(facet.PropertyValue, q))
                    ).Any());

            return psets.Concat(quants);

        }


        private static IEnumerable<IIfcPropertySet> FilterByFacet(this IEnumerable<IIfcPropertySet> relDefines,
           IfcPropertyFacet facet)
        {

            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }

            // Try Psets first
            // TODO: non PropertySingleValues
            var psets = relDefines
                .Where(ps => facet!.PropertySetName?.IsSatisfiedBy(ps.Name?.Value, true) == true)
                .Where(ps => ps
                    .HasProperties.OfType<IIfcPropertySingleValue>().Where(ps =>
                    facet!.PropertyName?.IsSatisfiedBy(ps.Name.Value, true) == true &&
                    (facet.PropertyValue?.HasAnyAcceptedValue() != true || facet!.PropertyValue?.IsSatisfiedBy(ps.NominalValue.Value, true) == true)
                    ).Any());

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
            if (quantity is IIfcPhysicalComplexQuantity comp)
                return default;


            throw new NotImplementedException(quantity.GetType().Name);
        }
    }
}
