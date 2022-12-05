using System.Linq.Expressions;
using Xbim.Common;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core
{
    public class IdsValidationResult
    {

        public string? FileName { get; set; }
        public ValidationStatus ValidationStatus { get; internal set; }
        public IPersistEntity? Entity { get; internal set; }

        public IList<ValidationMessage> Messages { get; } = new List<ValidationMessage>();

        public IEnumerable<string?> Successful { get => Messages.Where(m => m.Status == ValidationStatus.Success).Select(m => m.ToString()); } 
        public IEnumerable<string?> Failures { get => Messages.Where(m => m.Status == ValidationStatus.Failed).Select(m=> m.ToString()); }

        public FacetGroup? Requirement { get; set; }
    }

    public class ValidationMessage
    {
        


        // Gets the status based on current Expectation mode - i.e. Failure to match in Prohibited model = Success
        private static ValidationStatus GetStatus(Expectation expectation, bool? success)
        {
            switch (expectation)
            {
                case Expectation.Required:
                    return success == true ? ValidationStatus.Success : success == false ? ValidationStatus.Failed : ValidationStatus.Inconclusive;

                case Expectation.Prohibited:
                    return success == true ? ValidationStatus.Failed : success == false ? ValidationStatus.Success : ValidationStatus.Inconclusive;

                case Expectation.Optional:
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
        public ValidationMessage(ValidationStatus status, Expectation expectation, IFacet clause, string? reason = null, object? expectedResult = null, object? actualResult = null)
        {
            Status = status;
            Reason = reason;
            ExpectedResult = expectedResult;
            ActualResult = actualResult;
            Expectation = expectation;
            Clause = clause;

        }

        public string Entity => EntityAffected != null ? EntityAffected!.ToString() : "n/a";

        public ValidationStatus Status { get; set; }
        public string? Reason { get; set; }
        public object? ExpectedResult { get; set; }
        public object? ActualResult { get; set; }
        public Expectation Expectation { get; set; }

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
        public ValidationContext(T clause, Expectation expectationMode)
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

        public Expectation ExpectationMode { get; set; } = Expectation.Required;   // Default to Required, not Prohibited
        public T Clause { get; set; }
    }
}
