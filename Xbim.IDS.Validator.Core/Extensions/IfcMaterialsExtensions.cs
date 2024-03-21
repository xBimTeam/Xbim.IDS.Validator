using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Extensions
{
    /// <summary>
    /// Extension methods to help build xbim queries for <see cref="MaterialFacet"/>
    /// </summary>
    internal static class IfcMaterialsExtensions
    {

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
        /// Filters a set of <see cref="IIfcObjectDefinition"/> entries to those satisifying the material facet
        /// </summary>
        /// <param name="ent"></param>
        /// <param name="facet"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        public static IEnumerable<IIfcObjectDefinition> WhereAssociatedWithMaterial(this IEnumerable<IIfcObjectDefinition> ent, MaterialFacet facet)
        {
            return ent.Where(e => e.HasAssociations.OfType<IIfcRelAssociatesMaterial>().FilterByMaterialFacet(facet).Any());
        }

        /// <summary>
        /// Gets the set of Material values for a <see cref="IIfcMaterialSelect"/> that are candidates for checking
        /// </summary>
        /// <param name="material"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetMaterialNames(IIfcMaterialSelect material, ILogger? logger = null)
        {
            if (material == null)
                return Enumerable.Empty<string>();
            switch (material)
            {
                case IIfcMaterialDefinition md:
                    return GetMaterialDefinitionNames(md, logger);
                case IIfcMaterialUsageDefinition mud:
                    return GetMaterialUsageNames(mud);
                case IIfcMaterialList ml:
                    return GetMaterialNames(ml);
                default:
                    logger.LogNotImplemented($"MaterialSelect not implemented: {material.GetType().Name}");
                    return Enumerable.Empty<string>();
            }
        }

        private static IEnumerable<string> GetMaterialDefinitionNames(IIfcMaterialDefinition definition, ILogger? logger = null)
        {
            switch (definition)
            {
                case IIfcMaterial m:
                    return GetMaterialNames(m);
                case IIfcMaterialConstituent mc:
                    return GetMaterialNames(mc);
                case IIfcMaterialConstituentSet mcs:
                    return GetMaterialNames(mcs);
                case IIfcMaterialLayer ml:
                    return GetMaterialNames(ml);
                case IIfcMaterialLayerSet mls:
                    return GetMaterialNames(mls);
                case IIfcMaterialProfile mp:
                    return GetMaterialNames(mp);
                case IIfcMaterialProfileSet mps:
                    return GetMaterialNames(mps);
                default:
                    logger.LogNotImplemented($"MaterialDefinition not implemented: {definition.GetType().Name}");
                    return Enumerable.Empty<string>();
            }

        }

        private static IEnumerable<string> GetMaterialUsageNames(IIfcMaterialUsageDefinition ifcMaterialUsage, ILogger? logger = null)
        {
            switch (ifcMaterialUsage)
            {
                case IIfcMaterialLayerSetUsage mlsu:
                    return GetMaterialNames(mlsu.ForLayerSet);
                case IIfcMaterialProfileSetUsage mpsu:
                    return GetMaterialNames(mpsu.ForProfileSet);
                default:
                    logger.LogNotImplemented($"MaterialUsageDefinition not implemented: {ifcMaterialUsage.GetType().Name}");
                    return Enumerable.Empty<string>();
            }
        }

        private static IEnumerable<string> GetMaterialNames(IIfcMaterialList relatingMaterial)
        {
            foreach (var mat in relatingMaterial.Materials)
            {
                foreach (var name in GetMaterialNames(mat))
                    yield return name;
            }
        }


        private static IEnumerable<string> GetMaterialNames(IIfcMaterial material)
        {
            if (IsFilled(material.Name)) yield return material.Name.ToString();
            if (IsFilled(material.Category)) yield return material.Category.ToString();
        }

        private static IEnumerable<string> GetMaterialNames(IIfcMaterialLayerSet material)
        {

            if (IsFilled(material.LayerSetName)) yield return material.LayerSetName.ToString();
            foreach (IIfcMaterialLayer? layer in material.MaterialLayers)
            {
                foreach (var name in GetMaterialNames(layer))
                    yield return name;
            }
        }

        private static IEnumerable<string> GetMaterialNames(IIfcMaterialLayer layer)
        {
            if (IsFilled(layer.Name)) yield return layer.Name.ToString();
            if (IsFilled(layer.Category)) yield return layer.Category.ToString();

            foreach (var item in GetMaterialNames(layer.Material))
            {
                yield return item;
            }
        }

        private static IEnumerable<string> GetMaterialNames(IIfcMaterialProfileSet profileSet)
        {
            if (IsFilled(profileSet.Name)) yield return profileSet.Name.ToString();

            foreach (var profile in profileSet.MaterialProfiles)
            {
                foreach (var name in GetMaterialNames(profile))
                {
                    yield return name;
                }
            }
        }

        private static IEnumerable<string> GetMaterialNames(IIfcMaterialProfile profile)
        {
            if (IsFilled(profile.Name)) yield return profile.Name.ToString();
            if (IsFilled(profile.Category)) yield return profile.Category.ToString();
            foreach (var name in GetMaterialNames(profile.Material))
            {
                yield return name;
            }
        }

        private static IEnumerable<string> GetMaterialNames(IIfcMaterialConstituentSet constituentSet)
        {
            if (IsFilled(constituentSet.Name)) yield return constituentSet.Name.ToString();

            foreach (var constituent in constituentSet.MaterialConstituents)
            {
                foreach (var name in GetMaterialNames(constituent))
                {
                    yield return name;
                }
            }
        }

        private static IEnumerable<string> GetMaterialNames(IIfcMaterialConstituent constituent)
        {
            if (IsFilled(constituent.Name)) yield return constituent.Name.ToString();
            if (IsFilled(constituent.Category)) yield return constituent.Category.ToString();

            foreach (var name in GetMaterialNames(constituent.Material))
            {
                yield return name;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsFilled(IfcLabel? value)
        {
            return (!string.IsNullOrEmpty(value?.Value?.ToString()));
        }




        private static IEnumerable<IIfcRelAssociatesMaterial> FilterByMaterialFacet(this IEnumerable<IIfcRelAssociatesMaterial> relAssociates, MaterialFacet facet)
        {
            return relAssociates.Where(r => r.RelatingMaterial.MaterialIsSatisfiedBy(facet));
        }


        private static bool MaterialIsSatisfiedBy(this IIfcMaterialSelect material, MaterialFacet facet)
        {
            if (facet.Value != null)
            {
                var candidates = GetMaterialNames(material);
                foreach (var candidate in candidates)
                {
                    if (facet.Value.IsSatisfiedBy(candidate, true))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
