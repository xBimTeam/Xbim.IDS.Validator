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
        static Expectation ExpectationMode { get; set; } = Expectation.Required;   // Default to Reequired, not Prohibited

        public static void SetExpectation(bool? isRequired)
        {
            ExpectationMode = isRequired == true ? Expectation.Required : isRequired == false ? Expectation.Prohibited : Expectation.Optional;
        }

        // Gets the status based on current Expectation mode - i.e. Failure to match in Prohibited model = Success
        private static ValidationStatus GetStatus(bool? success)
        {
            switch (ExpectationMode)
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

        public static ValidationMessage Success<T>([NotNull] T clause, [NotNull] Expression<Func<T, object>> memberField, object? actualResult, string? reason = default, IPersist? entity = null) where T: IFacet
        {
            // decode the field we're validating from the expression
            var member = (memberField.Body as MemberExpression)?.Member?.Name;
            var expected = memberField.Compile().Invoke(clause).ToString();
            return new ValidationMessage
            {
                Status = GetStatus(true),
                Clause = clause,
                ActualResult = actualResult,
                Reason = reason,
                ExpectedResult = expected,
                Expectation = ExpectationMode,
                ValidatedField = member,
                EntityAffected = entity
            };
        }

        public static ValidationMessage Failure<T>(T clause, Expression<Func<T, object>> memberField, object? actualResult, string? reason = default, IPersist? entity = null) where T: IFacet
        {
            var member = (memberField.Body as MemberExpression)?.Member?.Name;
            var expected = memberField.Compile().Invoke(clause).ToString();
            return new ValidationMessage
            {
                Status = GetStatus(false),
                Clause = clause,
                ActualResult = actualResult,
                Reason = reason,
                ExpectedResult = expected,
                Expectation = ExpectationMode,
                ValidatedField = member,
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

        //public string EntityLabel => EntityAffected != null ? $"#{EntityAffected.EntityLabel}={EntityAffected.GetType().Name}" : "n/a";

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
}
