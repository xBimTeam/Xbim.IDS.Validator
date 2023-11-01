using System;
using Xbim.Common.Metadata;

namespace Xbim.IDS.Validator.Core.Extensions
{
    internal static class ExpressMetaDataExtensions
    {
        /// <summary>
        /// Gets the <see cref="ExpressType"/> accounting for circumstances when we only have an IFC4 interface
        /// </summary>
        /// <param name="metaData"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ExpressType GetExpressType(this ExpressMetaData metaData, Type type)
        {
            if (type.IsInterface && type.Name.StartsWith("IIfc"))
            {
                // Strip the leading 'I' to get the type name from Interface
                var name = type.Name.Substring(1).ToUpperInvariant();
                return metaData.ExpressType(name);
            }
            else
            {
                return metaData.ExpressType(type);
            }
        }
    }
}
