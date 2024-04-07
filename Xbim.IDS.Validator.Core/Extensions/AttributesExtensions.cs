using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Metadata;
using Xbim.Common.Model;

namespace Xbim.IDS.Validator.Core.Extensions
{
    public static class AttributesExtensions
    {
        public static IEnumerable<string> FilterToBaseType(this IEnumerable<string> classes, string baseType, ExpressMetaData metaData)
        {

            var root = metaData.ExpressType(baseType.ToUpperInvariant());
            if (root == null)
                throw new ArgumentOutOfRangeException(baseType);

            foreach(var cls in classes)
            {
                
                var type = metaData.ExpressType(cls);
                while (type != null)
                {
                    if (type == root)
                    {
                        yield return cls;
                        break;
                    }
                    type = type.SuperType;
                }

            }
        }
    }
}
