using System.Linq;
using System.Linq.Expressions;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Extensions;
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

        public override string ToString()
        {
            if(Status == ValidationStatus.Failed)
            {
                return $"[{Status}] {Clause?.GetType().Name}.{ValidatedField} : {Reason}. Expected '{Clause?.Short()}' but found '{ActualResult}' at {EntityLabel}";
            }
            else
            {
                return $"[{Status}] {Clause?.GetType().Name}.{ValidatedField} : {Reason} '{ActualResult}' at {EntityLabel}";
            }
        }

        public static ValidationMessage Success<T>([NotNull] T clause, [NotNull] Expression<Func<T, object>> memberField, object? actualResult, string? reason = default, IPersistEntity? entity = null) where T: IFacet
        {
            // decode the field we're validating from the expression
            var member = (memberField.Body as MemberExpression)?.Member?.Name;
            return new ValidationMessage
            {
                Status = ValidationStatus.Success,
                Clause = clause,
                //Expectation = requirement.IsRequired() ? Expectation.Required : requirement.IsOptional() ? Expectation.Optional : Expectation.Prohibited,
                ActualResult = actualResult,
                Reason = reason,
                ExpectedResult = clause.ToString(),
                ValidatedField = member,
                EntityAffected = entity
            };
        }

        public static ValidationMessage Failure<T>(T clause, Expression<Func<T, object>> memberField, object? actualResult, string? reason = default, IPersistEntity? entity = null) where T: IFacet
        {
            var member = (memberField.Body as MemberExpression)?.Member?.Name;
            return new ValidationMessage
            {
                Status = ValidationStatus.Failed,
                Clause = clause,
                //Expectation = requirement.IsRequired() ? Expectation.Required : requirement.IsOptional() ? Expectation.Optional : Expectation.Prohibited,
                ActualResult = actualResult,
                Reason = reason,
                ExpectedResult = clause.ToString(),
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

        public string EntityLabel => EntityAffected != null ? $"#{EntityAffected.EntityLabel}={EntityAffected.GetType().Name}" : "n/a";

        public ValidationStatus Status { get; set; }
        public string? Reason { get; set; }
        public object? ExpectedResult { get; set; }
        public object? ActualResult { get; set; }
        public Expectation Expectation { get; set; }

        public IFacet? Clause { get; set; }
        public string? ValidatedField { get; set; }
        public IPersistEntity? EntityAffected { get; set; }

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
