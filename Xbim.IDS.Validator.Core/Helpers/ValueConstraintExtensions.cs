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
            
            var first = constraint?.AcceptedValues?.Single();
            Debug.Assert(first != null);

            switch(first)
            {
                case ExactConstraint ec:
                    return ec.Value;

                case PatternConstraint pc:
                    return pc.Pattern;

                case RangeConstraint rc:
                    return rc.ToString();

                case ValueConstraint vc:
                    return vc.ToString();

                default:
                    throw new NotImplementedException(first.GetType().Name);
            }
   
        }
    }
}
