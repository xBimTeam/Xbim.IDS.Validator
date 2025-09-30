using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Extensions
{
    /// <summary>
    /// Extension Methods to help build xbim queries for <see cref="IfcTypeFacet"/>s
    /// </summary>
    public static class IfcEntityTypeExtensions
    {

        /// <summary>
        /// Filters objects to those matching the PredefinedType constraint, deferring to defined ObjectType, and falling back to the Type's PDT and user-defined Type
        /// when specified
        /// </summary>
        /// <param name="objects"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        public static IEnumerable<T> WhereHasPredefinedType<T>(this IEnumerable<T> objects, IfcTypeFacet facet) where T: IIfcObjectDefinition
        {
            return objects.Where(o => MatchesPredefinedType(o, facet));
        }


        /// <summary>
        /// Determines
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        public static bool MatchesPredefinedType(IIfcObjectDefinition obj, IfcTypeFacet facet)
        {
            if (obj is null)
            {
                return false;
            }

            if (facet?.PredefinedType == null)
            {
                return true;
            }
            var predefinedType = GetPredefinedType(obj, out bool isUserDefined);
            
            return facet.PredefinedType?.IsSatisfiedBy(predefinedType, true) == true ||
                (isUserDefined && facet.PredefinedType?.IsSatisfiedBy("USERDEFINED", true) == true);
        }

        public static string? GetPredefinedType(IIfcObjectDefinition obj, out bool isUserdefined)
        {
            // Based on https://github.com/CBenghi/IDS/blob/development/Documentation/facet-configurations.md
            if (obj is IIfcObject o)
            {
                var definingType = o.IsTypedBy.FirstOrDefault()?.RelatingType;
                if (definingType != null)
                {
                    // Type over-rides entities PDT
                    var typeValue = GetSubType(definingType, out isUserdefined);
                    if (typeValue == null || typeValue == "USERDEFINED" || typeValue == "NOTDEFINED")
                    {
                        // Try the object if meaningless
                        return GetSubType(o, out isUserdefined);
                    }
                    return typeValue;
                }
                else
                {
                    // No type - use object
                    return GetSubType(o, out isUserdefined);
                }
            }
            else
            {
                // Get for Type/Process etc
                return GetSubType(obj, out isUserdefined);
            }
        }

        /// <summary>
        /// Gets the subtype - either the PredefinedType value or the user-defined value when USERDEFINED
        /// </summary>
        /// <param name="definingType"></param>
        /// <param name="isUserDefined">Indicates if the pre-defined type is USERDEFINED</param>
        /// <returns></returns>
        private static string? GetSubType(IIfcObjectDefinition definingType, out bool isUserDefined)
        {
            isUserDefined = false;
#if XbimV6
            var pdt = definingType.GetPredefinedTypeValue();
#else
            string pdt = "";    // GetPredefinedType Not supported in v5.1 Toolkit
#endif
            if (pdt == null || pdt == "USERDEFINED")
            {
                if (pdt == "USERDEFINED")
                    isUserDefined = true;
                return GetObjectType((IIfcObjectDefinition?)definingType) ?? pdt;
            }
            return pdt;
        }

        /// <summary>
        /// Gets the User-defined ObjectType from the Object, ElementType or TypeProcess
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private static string? GetObjectType(IIfcObjectDefinition entity)
        {
            if (entity is IIfcObject obj)
                return obj.ObjectType?.Value?.ToString();
            else if (entity is IIfcElementType type)
                return type.ElementType?.Value?.ToString();
            else if (entity is IIfcTypeProcess process)
                return process.ProcessType?.Value?.ToString();
            else
                return null;
        }

    }
}
