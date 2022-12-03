using Xbim.Ifc4.Interfaces;

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



        ///// <summary>
        ///// Gets all <see cref="IIfcObjectDefinition"/>s defined by the propertyset, name and value
        ///// </summary>
        ///// <param name="relDefines"></param>
        ///// <param name="psetName"></param>
        ///// <param name="propName"></param>
        ///// <param name="propValue"></param>
        ///// <returns></returns>
        //public static IEnumerable<IIfcObjectDefinition> GetIfcPropertySingleValues(this IEnumerable<IIfcRelDefinesByProperties> relDefines,
        //    string psetName, string propName, string propValue)
        //{
        //    return relDefines.RelDefinesFilter(psetName, propName)
        //        .Where(p => ((IIfcPropertySet)p.RelatingPropertyDefinition).HasProperties.OfType<IIfcPropertySingleValue>()
        //            .Any(ps=> string.Equals(ps.NominalValue.ToString(), propValue, StringComparison.InvariantCultureIgnoreCase)))
        //        .SelectMany(r => r.RelatedObjects);

        //}

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


        //public static IEnumerable<IIfcPropertySingleValue> GetIfcPropertySingleValues(this IEnumerable<IIfcRelDefinesByProperties> relDefines,
        //    string psetName, string propName)
        //{
        //    return relDefines
        //            //.Where(r => r.RelatedObjects.Any(o => o.EntityLabel == entityLabel))
        //            .Where(r => r.RelatingPropertyDefinition is IIfcPropertySet ps && ps.Name == psetName)
        //            .SelectMany(p => ((IIfcPropertySet)p.RelatingPropertyDefinition)
        //                .HasProperties.Where(ps => ps.Name == propName)
        //                .OfType<IIfcPropertySingleValue>());
        //}

        //public static IEnumerable<IIfcPropertySingleValue> GetIfcPropertySingleValues(this IEnumerable<IIfcRelDefinesByProperties> relDefines,
        //    string psetName, string propName, string propValue)
        //{
        //    return relDefines.GetIfcPropertySingleValues(psetName, propName)
        //        .Where(p => p.NominalValue.Equals(propValue));
        //}
    }
}
