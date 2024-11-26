using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xbim.Common.Metadata;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Extensions;

namespace Xbim.IDS.Validator.Core.Helpers
{
    public static class ModelSchemaHelper
    {

        /// <summary>
        /// Gets the highest common IFC type for the two <see cref="IEnumerable{T}"/> <see cref="Expression"/>s
        /// returning IPersistEntity when non exists (e.g. un-rooted types)
        /// </summary>
        /// <param name="model"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Type GetCommonAncestorType(this IModel model, Expression left, Expression right)
        {
            var leftType = TypeHelper.GetImplementedIEnumerableType(left.Type);

            var rightType = TypeHelper.GetImplementedIEnumerableType(right.Type);

            var ancestors = new HashSet<ExpressType>();
            var express = model.Metadata.GetExpressType(leftType);
            while (express != null)
            {
                ancestors.Add(express);
                express = express.SuperType;
            }
            express = model.Metadata.GetExpressType(rightType);
            while (express != null)
            {
                if (ancestors.Contains(express))
                {
                    return express.Type;
                }
                express = express.SuperType;
            }
            return typeof(IPersistEntity);
        }
    }
}
