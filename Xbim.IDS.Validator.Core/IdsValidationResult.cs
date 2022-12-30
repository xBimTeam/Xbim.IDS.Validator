using System.Diagnostics;
using System.Linq.Expressions;
using Xbim.Common;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core
{

    /// <summary>
    /// Represents the result of validating a specific entity against an IDS requirement
    /// </summary>
    /// 
    [DebuggerDisplay("{ValidationStatus}: {Entity}. {Messages.Count} messages")]
    public class IdsValidationResult
    {
        public IdsValidationResult(IPersistEntity? entity, FacetGroup? requirement)
        {
       
            ValidationStatus = ValidationStatus.Inconclusive;
            Entity = entity;
            Requirement = requirement;
        }

        /// <summary>
        /// The validation status of an individual entity for a requirement
        /// </summary>
        public ValidationStatus ValidationStatus { get; internal set; }
        /// <summary>
        /// A model <see cref="IPersistEntity"/> being tested against defined requirements
        /// </summary>
        public IPersistEntity? Entity { get; internal set; }

        /// <summary>
        /// The set of messages raised by the validation process against this entity
        /// </summary>
        public IList<ValidationMessage> Messages { get; } = new List<ValidationMessage>();

        /// <summary>
        /// The set of all success messages for this entity
        /// </summary>
        public IEnumerable<string?> Successful { get => Messages.Where(m => m.Status == ValidationStatus.Success).Select(m => m.ToString()); } 
        /// <summary>
        /// The set of all failure messages for this entity
        /// </summary>
        public IEnumerable<string?> Failures { get => Messages.Where(m => m.Status == ValidationStatus.Failed).Select(m=> m.ToString()); }
        /// <summary>
        /// The requirement the entity is tested against
        /// </summary>
        public FacetGroup? Requirement { get; set; }
    }

    public class ValidationMessage
    {
        


        // Gets the status based on current Expectation mode - i.e. Failure to match in Prohibited model = Success
        private static ValidationStatus GetStatus(RequirementCardinalityOptions expectation, bool? success)
        {
            switch (expectation)
            {
                case RequirementCardinalityOptions.Expected:
                    return success == true ? ValidationStatus.Success : success == false ? ValidationStatus.Failed : ValidationStatus.Inconclusive;

                case RequirementCardinalityOptions.Prohibited:
                    return success == true ? ValidationStatus.Failed : success == false ? ValidationStatus.Success : ValidationStatus.Inconclusive;

                case RequirementCardinalityOptions.Optional:
                default:
                    return ValidationStatus.Inconclusive;

            }
           
            
        }

        public override string ToString()
        {
            if(Status == ValidationStatus.Failed)
            {
                return $"[{Status}] {Expectation} {Clause?.GetType().Name}.{ValidatedField} : {Reason}. Constrained to \"{ExpectedResult}\" but found \"{ActualResult}\" at {Entity}";
            }
            else
            {
                return $"[{Status}] {Expectation} {Clause?.GetType().Name}.{ValidatedField} : {Reason} Constraint \"{ExpectedResult}\" with \"{ActualResult}\" at {Entity}";
            }
        }

        public static ValidationMessage Success<T>(ValidationContext<T> context, [NotNull] Expression<Func<T, object>> memberField, object? actualResult, string? reason = default, IPersist? entity = null) where T: IFacet
        {
            return new ValidationMessage
            {
                Status = GetStatus(context.ExpectationMode, true),
                Clause = context.Clause,
                ActualResult = actualResult,
                Reason = reason,
                ExpectedResult = context.GetExpected(memberField),
                Expectation = context.ExpectationMode,
                ValidatedField = context.GetMember(memberField),
                EntityAffected = entity
            };
        }

        public static ValidationMessage Failure<T>(ValidationContext<T> context, Expression<Func<T, object>> memberField, object? actualResult, string? reason = default, IPersist? entity = null) where T: IFacet
        {

            return new ValidationMessage
            {
                Status = GetStatus(context.ExpectationMode, false),
                Clause = context.Clause,
                ActualResult = actualResult,
                Reason = reason,
                ExpectedResult = context.GetExpected(memberField),
                Expectation = context.ExpectationMode,
                ValidatedField = context.GetMember(memberField),
                EntityAffected = entity
            };
        }

        public ValidationMessage()
        {
        }
        public ValidationMessage(ValidationStatus status, RequirementCardinalityOptions expectation, IFacet clause, string? reason = null, object? expectedResult = null, object? actualResult = null)
        {
            Status = status;
            Reason = reason;
            ExpectedResult = expectedResult;
            ActualResult = actualResult;
            Expectation = expectation;
            Clause = clause;

        }

        public string? Entity => (EntityAffected != null) ? EntityAffected.ToString() : "n/a";

        public ValidationStatus Status { get; set; }
        public string? Reason { get; set; }
        public object? ExpectedResult { get; set; }
        public object? ActualResult { get; set; }
        public RequirementCardinalityOptions Expectation { get; set; }

        public IFacet? Clause { get; set; }
        public string? ValidatedField { get; set; }
        public IPersist? EntityAffected { get; set; }
     
    }

    public enum ValidationStatus
    {
        Success,
        Failed,
        Inconclusive
    }

    public enum Expectation
    {
        Optional,
        Required,
        Prohibited,
    }

    public enum FacetType
    {
        IfcType,
        Attribute,
        Property,
        Relation,
        Document,
        Material,
        PartOf
    }

    /// <summary>
    /// Class to hold validation context when executing a validation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ValidationContext<T> where T: IFacet
    {
        public ValidationContext(T clause, RequirementCardinalityOptions expectationMode)
        {
            ExpectationMode = expectationMode;
            Clause = clause;
        }

        public string GetExpected(Expression<Func<T, object>> memberField)
        {
            if (memberField is null)
            {
                throw new ArgumentNullException(nameof(memberField));
            }

            // decode the field we're validating from the expression
            return memberField!.Compile().Invoke(Clause)?.ToString() ?? "";
        }

        public string GetMember(Expression<Func<T, object>> memberField)
        {
            if (memberField is null)
            {
                throw new ArgumentNullException(nameof(memberField));
            }

            return (memberField!.Body as MemberExpression)?.Member?.Name ?? "";
            
        }

        public RequirementCardinalityOptions ExpectationMode { get; set; } = RequirementCardinalityOptions.Expected;   // Default to Required, not Prohibited
        public T Clause { get; set; }
    }
}
