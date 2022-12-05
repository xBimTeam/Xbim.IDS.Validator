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
        public static IEnumerable<IIfcObjectDefinition> GetIfcPropertySingleValues(this IEnumerable<IIfcRelDefinesByProperties> relDefines,
            string psetName, string propName, string? propValue)
        {
            return relDefines.RelDefinesFilter(psetName, propName, propValue)
                    .SelectMany(r => r.RelatedObjects);
        }

        /// <summary>
        /// Selects all objects using a material
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

            return relAssociates.Where((r => 
            (
                (r.RelatingMaterial is IIfcMaterialList l && l.Materials.Any(m => materialFacet?.Value?.IsSatisfiedBy(m.Name, true) == true)) || 
                (r.RelatingMaterial is IIfcMaterial m && materialFacet?.Value?.IsSatisfiedBy(m.Name, true) == true) ||
                (r.RelatingMaterial is IIfcMaterialLayerSetUsage ls && ls.ForLayerSet.MaterialLayers.Any(mls =>  materialFacet?.Value?.IsSatisfiedBy(mls.Material.Name, true) == true))
            )))
                    .SelectMany(r => r.RelatedObjects).OfType<IIfcObjectDefinition>();
        }



        private static IEnumerable<IIfcRelDefinesByProperties> RelDefinesFilter(this IEnumerable<IIfcRelDefinesByProperties> relDefines,
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

    }
}
