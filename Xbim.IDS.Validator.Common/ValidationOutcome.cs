using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IDS.Validator.Core;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Common
{
    /// <summary>
    /// Represents the top level outcome of an IDS validation run
    /// </summary>
    public class ValidationOutcome
    {
        /// <summary>
        /// Constructs a new Outcome
        /// </summary>
        /// <param name="idsDocument"></param>
        public ValidationOutcome(Xids idsDocument)
        {
            IdsDocument = idsDocument;
        }

        /// <summary>
        /// The IDS specification object
        /// </summary>
        public Xids IdsDocument { get; private set; }

        /// <summary>
        /// The high level results of the requirements defined in the executed as part of this validation run
        /// </summary>
        public IList<ValidationRequirement> ExecutedRequirements { get; private set; } = new List<ValidationRequirement>();

        /// <summary>
        /// The overall status of the Validation run
        /// </summary>
        public ValidationStatus Status { get; set; } = ValidationStatus.Inconclusive;

        /// <summary>
        /// The overall message from the run
        /// </summary>
        public string? Message { get; private set; }

        /// <summary>
        /// Marks the outcome as catestrophically failed
        /// </summary>
        /// <param name="mesg"></param>
        public void MarkCompletelyFailed(string mesg)
        {
            Status = ValidationStatus.Error;
            Message = mesg;
        }
    }

    /// <summary>
    /// Represents a requirement result after validation
    /// </summary>
    /// <remarks>E.g. all Doors must have a Firerating</remarks>
    public class ValidationRequirement
    {
        /// <summary>
        /// Constructs a new requirement
        /// </summary>
        /// <param name="spec"></param>
        public ValidationRequirement(Specification spec)
        {
            Specification = spec;
        }

        /// <summary>
        ///  The status of this requirement
        /// </summary>
        public ValidationStatus Status { get; set; } = ValidationStatus.Inconclusive;


        /// <summary>
        /// The IDS specification of this Requirement
        /// </summary>
        public Specification Specification { get; }

        /// <summary>
        /// The results of testing this specification against applicable entities in the model
        /// </summary>
        public IList<IdsValidationResult> ApplicableResults { get; private set; } = new List<IdsValidationResult>();

        /// <summary>
        /// Gets the results where the requirement failed, accounting for Prohibited requirements
        /// </summary>
        public IEnumerable<IdsValidationResult> FailedResults
        {
            get
            {
                return ApplicableResults.Where(a => a.ValidationStatus == ValidationStatus.Fail);
            }

        }

        /// <summary>
        /// Gets the results where the requirement passed, accounting for Prohibited requirements
        /// </summary>
        public IEnumerable<IdsValidationResult> PassedResults
        {
            get
            {
                return ApplicableResults.Where(a => a.ValidationStatus == ValidationStatus.Pass);
            }

        }

        /// <summary>
        /// Indicates if the result has failed the requirements
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool IsFailure(IdsValidationResult result)
        {
            return result.ValidationStatus == ValidationStatus.Fail;
        }

        /// <summary>
        /// Indicates if the result has met the requirements
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool IsSuccess(IdsValidationResult result)
        {
            return result.ValidationStatus == ValidationStatus.Pass;
        }

    }
}
