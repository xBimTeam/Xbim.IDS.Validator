using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;
using static Xbim.InformationSpecifications.PartOfFacet;

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
            if (relDefines is null)
            {
                throw new ArgumentNullException(nameof(relDefines));
            }

            return relDefines.FilterByPropertyFacet(facet)
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

            return relAssociates.FilterByMaterialFacet(materialFacet)
                    .SelectMany(r => r.RelatedObjects).OfType<IIfcObjectDefinition>();
        }


        /// <summary>
        /// Selects all objects using a matching classification, inferring from Types where appropriate
        /// </summary>
        /// <remarks>The filtering also accounts for hierarchical classification schemes</remarks>
        /// <param name="relAssociates"></param>
        /// <param name="materialName"></param>
        /// <returns></returns>
        public static IEnumerable<IIfcObjectDefinition> GetIfcObjectsUsingClassification(this IEnumerable<IIfcRelAssociatesClassification> relAssociates, IfcClassificationFacet facet)
        {
            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }

            // When Getting objects we cast the net wide, starting at IIfcRelAssociatesClassification and traversing from Types to instances
            // This differs to filtering (WhereAssociatedToClassification), where we determine if existing selection meets classification criteria

            var matchingRels = relAssociates.FilterByClassificationFacet(facet);
            // Find objects with matching classifications
            IEnumerable<IIfcObjectDefinition> results = matchingRels
                .SelectMany(r => r.RelatedObjects).OfType<IIfcObject>();

            // Append Types with matching classifications
            var typeResults = matchingRels.SelectMany(r => r.RelatedObjects).OfType<IIfcTypeObject>();

            results = results.Union(typeResults);
            // Append instances of objects defined by matching types
            results = results.Union(typeResults.SelectMany(t => t.Types.SelectMany(tt => tt.RelatedObjects)));

            return results;

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
        /// <param name="facet"></param>
        /// <returns></returns>
        public static IEnumerable<IIfcClassificationSelect> GetClassificationReferences(this IEnumerable<IIfcRelAssociatesClassification> relAssociates, IfcClassificationFacet facet)
        {
            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }
            return relAssociates.FilterByClassificationFacet(facet)
                .Select(r => r.RelatingClassification).OfType<IIfcClassificationSelect>();

        }

        /// <summary>
        /// Selects all elements that are related to the Relationship as children, matching the criteria 
        /// </summary>
        /// <param name="rel"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<IIfcObjectDefinition> GetRelatedIfcObjects(this IEnumerable<IIfcRelationship> rel, PartOfFacet facet)
        {
            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }

            var innerType = TypeHelper.GetImplementedIEnumerableType(rel.GetType());
            
            // Note: PredefinedType verification only supports IfcObject.ObjectType
            // TODO: Support for IfcElementType.ElementType and IfcTypeProcess.ProcesType. Support for intrinsic Predefined Enum. Type Inheritance of PDT
            if (typeof(IIfcRelAggregates).IsAssignableFrom(innerType))
            {
                var items = rel.Cast<IIfcRelAggregates>();
                return items.Where(r => facet.EntityType?.IfcType?.IsSatisfiedBy(r.RelatingObject.GetType().Name, true) == true)
                    .Where(r => facet.EntityType?.PredefinedType == null || r.RelatingObject is IIfcObject p && facet.EntityType?.PredefinedType?.IsSatisfiedBy(p.ObjectType, true) == true)
                    .SelectMany(r => r.RelatedObjects).OfType<IIfcObjectDefinition>();
            }
            else if (typeof(IIfcRelNests).IsAssignableFrom(innerType))
            {
                var items = rel.Cast<IIfcRelNests>();
                return items.Where(r => facet.EntityType?.IfcType?.IsSatisfiedBy(r.RelatingObject.GetType().Name, true) == true)
                    .Where(r => facet.EntityType?.PredefinedType == null || r.RelatingObject is IIfcObject p && facet.EntityType?.PredefinedType?.IsSatisfiedBy(p.ObjectType, true) == true)
                    .SelectMany(r => r.RelatedObjects).OfType<IIfcObjectDefinition>();
            }
            else if (typeof(IIfcRelAssignsToGroup).IsAssignableFrom(innerType))
            {
                
                var items = rel.Cast<IIfcRelAssignsToGroup>();
                return items.Where(r => facet.EntityType?.IfcType?.IsSatisfiedBy(r.RelatingGroup.GetType().Name, true) == true)
                    .Where(r => facet.EntityType?.PredefinedType == null || r.RelatingGroup is IIfcObject p && facet.EntityType?.PredefinedType?.IsSatisfiedBy(p.ObjectType, true) == true)
                    .SelectMany(r => r.RelatedObjects).OfType<IIfcObjectDefinition>();
            }
            else if (typeof(IIfcRelContainedInSpatialStructure).IsAssignableFrom(innerType))
            {
                var items = rel.Cast<IIfcRelContainedInSpatialStructure>();
                return items.Where(r => facet.EntityType?.IfcType?.IsSatisfiedBy(r.RelatingStructure.GetType().Name, true) == true)
                    .Where(r => facet.EntityType?.PredefinedType == null || r.RelatingStructure is IIfcObject p && facet.EntityType?.PredefinedType?.IsSatisfiedBy(p.ObjectType, true) == true)
                    .SelectMany(r => r.RelatedElements).OfType<IIfcProduct>();
            }
            else
            {
                throw new NotSupportedException($"{innerType} is not a supported relationship");
            }
        }

        public static IEnumerable<IIfcObjectDefinition> WhereHasPartOfRelationship(this IEnumerable<IIfcObjectDefinition> ent, PartOfFacet facet)
        {
            // Where classification matches directly, or an object's Type matches classification

            var filtered = ent.Where(e => facet.EntityType?.IfcType?.IsSatisfiedBy(e.GetType().Name, true) == true);
            return (facet.GetRelation()) switch
            {
                PartOfRelation.IfcRelAggregates => filtered.Where(e => e.IsDecomposedBy.OfType<IIfcRelAggregates>().Any()),
                PartOfRelation.IfcRelNests => filtered.Where(e => e.IsDecomposedBy.OfType<IIfcRelNests>().Any()),
                PartOfRelation.IfcRelAssignsToGroup => filtered.Where(e => e.HasAssignments.OfType<IIfcRelAssignsToGroup>().Any()),
                PartOfRelation.IfcRelContainedInSpatialStructure => filtered.Where(e => e.IsDecomposedBy.OfType<IIfcRelAggregates>().Any()),    // TODO Validate this

                _ => ent
            }; 
        }

        /// <summary>
        /// Gets Parents associated to an entity that match the requirement
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="materialFacet"></param>
        /// <returns></returns>
        public static IEnumerable<IIfcObjectDefinition> GetPartsForEntity(IIfcObjectDefinition obj, PartOfFacet facet)
        {
            var relation = facet.GetRelation();

            
            var ancestry = relation switch
            {
                PartOfRelation.IfcRelAggregates => obj.Decomposes.Select(r => r.RelatingObject)
                    .UnionAncestry(relation),
                PartOfRelation.IfcRelNests => obj.Nests.Select(r => r.RelatingObject)
                    .UnionAncestry(relation),
                PartOfRelation.IfcRelAssignsToGroup => obj.HasAssignments.OfType<IIfcRelAssignsToGroup>().Select(r => r.RelatingGroup)
                    .UnionAncestry(relation),
                PartOfRelation.IfcRelContainedInSpatialStructure => obj is IIfcProduct pr 
                    ? pr.IsContainedIn != null 
                        ? new[] { pr.IsContainedIn }.UnionAncestry(relation) // Has Immediate link to SpatialElement
                        // Check for indirect link
                        : new[] { pr }.UnionAncestry(PartOfRelation.IfcRelAggregates).Cast<IIfcProduct>().Select(p => p.IsContainedIn).Where(c => c != null).UnionAncestry(relation)

                    : Enumerable.Empty<IIfcObjectDefinition>(),

                _ => Enumerable.Empty<IIfcObjectDefinition>()
            };

            return ancestry.Where(p => MatchesPart(p, facet));
            
        }

        private static bool MatchesPart(IIfcObjectDefinition obj, PartOfFacet facet)
        {
            if (facet.EntityType == null) return true;
            if(obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            // TODO: Limited support for Predefined Type only. Omits complex scenarios, Type Inheritance, Enums etc
            return (facet.EntityType?.IfcType?.IsSatisfiedBy(obj.GetType().Name, true) == true) &&
                (facet.EntityType?.PredefinedType == null || obj is IIfcObject p && facet.EntityType?.PredefinedType?.IsSatisfiedBy(p.ObjectType, true) == true)
                ;
        }

        private static IEnumerable<IIfcObjectDefinition> UnionAncestry(this IEnumerable<IIfcObjectDefinition> objs, PartOfRelation relation)
        {
            if (!objs.Any())
                return objs;
            return relation switch
            {
                PartOfRelation.IfcRelAggregates => objs.Union(objs.SelectMany(o => o.Decomposes.Select(r => r.RelatingObject)).UnionAncestry(relation)),
                PartOfRelation.IfcRelNests => objs.Union(objs.SelectMany(o => o.Nests.Select(r => r.RelatingObject)).UnionAncestry(relation)),
                PartOfRelation.IfcRelAssignsToGroup => objs.Union(objs.SelectMany(o => o.HasAssignments.OfType<IIfcRelAssignsToGroup>().Select(r => r.RelatingGroup)).UnionAncestry(relation)),
                PartOfRelation.IfcRelContainedInSpatialStructure => objs.Union(objs.Cast<IIfcSpatialStructureElement>().Select(sp => sp.IsContainedIn).Where(s=> s != null).UnionAncestry(relation)),

                _ => Enumerable.Empty<IIfcObjectDefinition>()
            };
            
            
        }



        public static IEnumerable<IIfcObjectDefinition> WhereAssociatedWithClassification(this IEnumerable<IIfcObjectDefinition> ent, IfcClassificationFacet facet)
        {
            // Where classification matches directly, or an object's Type matches classification
            IEnumerable<IIfcObjectDefinition> results = ent.Where(e => e.HasAssociations.OfType<IIfcRelAssociatesClassification>()
                .FilterByClassificationFacet(facet).Any() ||
                e is IIfcObject t && t.IsTypedBy.Any(r => r.RelatingType.HasAssociations.OfType<IIfcRelAssociatesClassification>()
                .FilterByClassificationFacet(facet).Any()));


            return results;
        }

        public static IEnumerable<IIfcObjectDefinition> WhereAssociatedWithMaterial(this IEnumerable<IIfcObjectDefinition> ent, MaterialFacet facet)
        {
            return ent.Where(e => e.HasAssociations.OfType<IIfcRelAssociatesMaterial>().FilterByMaterialFacet(facet).Any());
        }

        public static IEnumerable<IIfcObject> WhereAssociatedWithProperty(this IEnumerable<IIfcObject> ent, IfcPropertyFacet facet)
        {
            return ent.Where(e => e.IsDefinedBy.OfType<IIfcRelDefinesByProperties>().FilterByPropertyFacet(facet).Any()
            ||
            // Or we inherit from the type
            e.IsTypedBy.Any(r => r.RelatingType.HasPropertySets.OfType<IIfcPropertySet>().FilterByPropertyFacet(facet).Any()));
        }

        public static IEnumerable<IIfcTypeObject> WhereAssociatedWithProperty(this IEnumerable<IIfcTypeObject> ent, IfcPropertyFacet facet)
        {
            return ent.Where(e => e.HasPropertySets.OfType<IIfcPropertySet>().FilterByPropertyFacet(facet).Any());
        }


        private static IEnumerable<IIfcRelAssociatesMaterial> FilterByMaterialFacet(this IEnumerable<IIfcRelAssociatesMaterial> relAssociates, MaterialFacet facet)
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

        private static IEnumerable<IIfcRelAssociatesClassification> FilterByClassificationFacet(this IEnumerable<IIfcRelAssociatesClassification> relAssociates, IfcClassificationFacet facet)
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
            if (ancestry == null)
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
    }
}
