using System.Diagnostics;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Helpers
{
    public static class ValueConstraintExtensions
    {
        /// <summary>
        /// Gets the first allowed value for a Constraint
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        public static string SingleValue(this ValueConstraint constraint)
        {
            
            var first = (constraint?.AcceptedValues?.Single() as ExactConstraint);
            Debug.Assert(first != null);
            return first.Value;
        }
    }
}
