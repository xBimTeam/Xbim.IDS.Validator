
namespace Xbim.IDS.Validator.Common
{
    /// <summary>
    /// The results of an Entity being validated against a requirement
    /// </summary>
    /// <remarks>The ordering of the enum has meaning</remarks>
    public enum EntityValidationResult
    {
        /// <summary>
        /// Not tested
        /// </summary>
        NotTested,
        
        /// <summary>
        /// The entity met the requirement
        /// </summary>
        RequirementSatisfied,

        /// <summary>
        /// The entity failed to meet the requirement
        /// </summary>
        RequirementNotSatisfied,

        /// <summary>
        /// An error occurred testing the entity
        /// </summary>
        Error
    }

}
