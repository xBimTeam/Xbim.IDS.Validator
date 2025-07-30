using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Xbim.Common;
using Xbim.IDS.Validator.Common;
using Xbim.InformationSpecifications;
using static Xbim.InformationSpecifications.RequirementCardinalityOptions;

namespace Xbim.IDS.Validator.Core
{

    /// <summary>
    /// Represents the result of validating a specific entity against an IDS requirement
    /// </summary>
    /// 
    [DebuggerDisplay("{ValidationStatus}: {Entity}. {Messages.Count} messages")]
    public class IdsValidationResult
    {
        /// <summary>
        /// Creates a validation result for the supplied Ifc entity and set of requirements.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="requirement"></param>
        /// <param name="logFullEntity">Determines if the full xbim entity is returned in the result, or just the entityId</param>
        public IdsValidationResult(IPersistEntity? entity, FacetGroup? requirement, bool logFullEntity = false)
        {
       
            
            Entity = entity?.EntityLabel;
            if(logFullEntity)
                FullEntity = entity;
            Requirement = requirement;
        }

        /// <summary>
        /// The validation status of an individual entity for a requirement
        /// </summary>
        public ValidationStatus ValidationStatus { get => TestStatus switch
        {
            EntityValidationResult.NotTested => ValidationStatus.Inconclusive,
            EntityValidationResult.Excluded => ValidationStatus.Skipped,
            EntityValidationResult.RequirementSatisfied => ValidationStatus.Pass,
            EntityValidationResult.RequirementNotSatisfied => ValidationStatus.Fail,
            EntityValidationResult.Error => ValidationStatus.Error,
            _ => throw new NotImplementedException(),
        };
                }
        
        /// <summary>
        /// The Id of the model entity being tested against defined requirements
        /// </summary>
        public int? Entity { get; internal set; }

        /// <summary>
        /// The Id of the model entity being tested against defined requirements
        /// </summary>
        public int? ParentEntity { get; set; }

        /// <summary>
        ///  The Full entity being tested against defined requirements
        /// </summary>
        public IPersist? FullEntity { get; internal set; }

        /// <summary>
        /// The set of diagnostic messages raised by the validation process against this entity
        /// </summary>

        public IList<ValidationMessage> Messages { get; } = new List<ValidationMessage>();

        /// <summary>
        /// The set of all success messages for this entity
        /// </summary>
        public IEnumerable<string?> Successful { get => Messages.Where(m => m.Status == ValidationStatus.Pass).Select(m => m.ToString()); } 
        /// <summary>
        /// The set of all failure messages for this entity
        /// </summary>
        public IEnumerable<string?> Failures { get => Messages.Where(m => m.Status == ValidationStatus.Fail).Select(m=> m.ToString()); }
        /// <summary>
        /// The requirement the entity is tested against
        /// </summary>
        public FacetGroup? Requirement { get; set; }

        /// <summary>
        /// The status of the test
        /// </summary>
        internal EntityValidationResult TestStatus { get; private set; } = EntityValidationResult.NotTested;

        /// <summary>
        /// Marks the test as in error
        /// </summary>
        public void FailWithError(ValidationMessage error)
        {
            error.Status = ValidationStatus.Error;
            Messages.Add(error);
            MarkStatus(EntityValidationResult.Error);
        }

        /// <summary>
        /// Mark this test as Not Satisified
        /// </summary>
        public void Fail(ValidationMessage failureReason)
        {
            failureReason.Status = ValidationStatus.Fail;
            Messages.Add(failureReason);
            MarkStatus(EntityValidationResult.RequirementNotSatisfied);
        }

        /// <summary>
        /// Mark this test as Satisified
        /// </summary>
        public void MarkSatisified(ValidationMessage justification)
        {
            justification.Status = ValidationStatus.Pass;
            Messages.Add(justification);
            MarkStatus(EntityValidationResult.RequirementSatisfied);
        }

        /// <summary>
        /// Marks the test with the given status, where that status
        /// </summary>
        /// <param name="status"></param>
        public void MarkStatus(EntityValidationResult status)
        {
            // Upgrade/Change the status if the new status is more significant than prior status
            // Allows Failures to over-ride Success, and Errors to override Failures. But Success will never trump Failure.
            if(TestStatus < status) TestStatus = status;
        }
    }

    /// <summary>
    /// A diagnostic message from the IDS validation run
    /// </summary>
    public class ValidationMessage
    {
        


        private static ValidationStatus GetStatus(bool? success)
        {
            return success == true ? ValidationStatus.Pass : success == false ? ValidationStatus.Fail : ValidationStatus.Inconclusive;
        }


        /// <inheritDoc/>
        public override string ToString()
        {
            if(Status == ValidationStatus.Fail)
            {
                if (Expectation == Cardinality.Prohibited)
                {
                    return $"[{Status}] {Reason}: {Expectation} - found {Entity}";
                }
                else
                {
                    return $"[{Status}] {Reason}: {Expectation} {FacetType}{ValidatedField} to be '{ExpectedResult}' - but found {FormatedActualResult}";
                }
            }
            else if (Status == ValidationStatus.Skipped)
            {
                return $"[{Status}] {Reason}";
            }
            else
            {
                return $"[{Status}] {Reason}: {Expectation} {FacetType}{ValidatedField} to be '{ExpectedResult}' and found {FormatedActualResult}";
            }
        }

        /// <summary>
        /// Helper to ensure Facet field names are qualified to be less ambiguous
        /// </summary>
        /// <remarks>E.g. Material has a Value property, where Attribute has AttributeValue and Properties have PropertyValue</remarks>
        private string FacetType => Clause switch
        {
            MaterialFacet prop => "Material",
            PartOfFacet prop => "Related",
            IfcClassificationFacet prop => "Classification ",
            _ => "",
        };

        /// <summary>
        /// Builds a message representing a successful check
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="memberField"></param>
        /// <param name="actualResult"></param>
        /// <param name="reason"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static ValidationMessage Success<T>(ValidationContext<T> context, [NotNull] Expression<Func<T, object>> memberField, object? actualResult, string? reason = default, IPersist? entity = null) where T: IFacet
        {
            return new ValidationMessage
            {
                Status = GetStatus( true),
                Clause = context.Clause,
                ActualResult = actualResult,
                Reason = reason,
                ExpectedResult = context.GetExpected(memberField),
                Expectation = context.FacetCardinality,
                ValidatedField = context.GetMember(memberField),
                EntityAffected = entity
            };
        }

        /// <summary>
        /// Builds message representing a failed check
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="memberField"></param>
        /// <param name="actualResult"></param>
        /// <param name="reason"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static ValidationMessage Failure<T>(ValidationContext<T> context, Expression<Func<T, object>> memberField, object? actualResult, string? reason = default, IPersist? entity = null) where T: IFacet
        {

            return new ValidationMessage
            {
                Status = GetStatus(false),
                Clause = context.Clause,
                ActualResult = actualResult,
                Reason = reason,
                ExpectedResult = context.GetExpected(memberField),
                Expectation = context.FacetCardinality,
                ValidatedField = context.GetMember(memberField),
                EntityAffected = entity
            };
        }

        /// <summary>
        /// Builds a message representing an inconclusive check
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="memberField"></param>
        /// <param name="actualResult"></param>
        /// <param name="reason"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static ValidationMessage Inconclusive<T>(ValidationContext<T> context, Expression<Func<T, object>> memberField, object? actualResult, string? reason = default, IPersist? entity = null) where T : IFacet
        {

            return new ValidationMessage
            {
                Status = ValidationStatus.Inconclusive,
                Clause = context.Clause,
                ActualResult = actualResult,
                Reason = reason,
                ExpectedResult = context.GetExpected(memberField),
                Expectation = context.FacetCardinality,
                ValidatedField = context.GetMember(memberField),
                EntityAffected = entity
            };
        }

        /// <summary>
        /// Builds a message representing an skipped check
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static ValidationMessage Skipped(string? reason = default, IPersist? entity = null)
        {

            return new ValidationMessage
            {
                Status = ValidationStatus.Skipped,
                Reason = reason,
                EntityAffected = entity
            };
        }

        /// <summary>
        /// Builds a message representing an Error when validating
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public static ValidationMessage Error(string reason)
        {
            return new ValidationMessage
            {
                Status = ValidationStatus.Error,
                Reason = reason,
                Expectation = Cardinality.Expected
            };
        }

        /// <summary>
        /// Builds a message representing a prohibited entity for this specification
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static ValidationMessage Prohibited(IPersist entity)
        {
            return new ValidationMessage
            {
                Status = ValidationStatus.Fail,
                Reason = "Entity prohibited",
                Expectation = Cardinality.Prohibited,
                EntityAffected = entity
            };
        }

        /// <summary>
        /// Builds a message representing a successfully matched entity for the specification
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static ValidationMessage Success(IPersist entity)
        {
            return new ValidationMessage
            {
                Status = ValidationStatus.Pass,
                Reason = "Applicable Entity found",
                Expectation = Cardinality.Expected,
                EntityAffected = entity
            };
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public ValidationMessage()
        {
        }

        /// <summary>
        /// Constructs a new message with the provided parameters
        /// </summary>
        /// <param name="status"></param>
        /// <param name="expectation"></param>
        /// <param name="clause"></param>
        /// <param name="reason"></param>
        /// <param name="expectedResult"></param>
        /// <param name="actualResult"></param>
        public ValidationMessage(ValidationStatus status, Cardinality expectation, IFacet clause, string? reason = null, object? expectedResult = null, object? actualResult = null)
        {
            Status = status;
            Reason = reason;
            ExpectedResult = expectedResult;
            ActualResult = actualResult;
            Expectation = expectation;
            Clause = clause;

        }

        /// <summary>
        /// String represenying the affected entity
        /// </summary>
        public string? Entity => (EntityAffected != null) ? EntityAffected.ToString() : "n/a";

        /// <summary>
        /// The message status
        /// </summary>
        public ValidationStatus Status { get; set;}
        /// <summary>
        /// The reason for success or failure
        /// </summary>
        public string? Reason { get; set; }
        /// <summary>
        /// Object representing the expected result
        /// </summary>
        public object? ExpectedResult { get; set; }
        /// <summary>
        /// The actual result found
        /// </summary>
        public object? ActualResult { get; set; }
        /// <summary>
        /// Represents the Expectation - Required/Prohibited etc
        /// </summary>
        public Cardinality Expectation { get; set; }
        /// <summary>
        /// A link to the <see cref="IFacet"/> tested
        /// </summary>
        public IFacet? Clause { get; set; }
        /// <summary>
        /// The facet field checked
        /// </summary>
        public string? ValidatedField { get; set; }

        /// <summary>
        /// A reference to the xbim entity of the affected item
        /// </summary>
        public IPersist? EntityAffected { get; set; }

        /// <summary>
        /// A formatted string presenting the actual result
        /// </summary>
        public string FormatedActualResult => string.IsNullOrEmpty(ActualResult?.ToString()) ? "nothing" : $"'{ActualResult}'"


;    }

    /// <summary>
    /// Represents the validation status of a specification or one of its requirements
    /// </summary>
    public enum ValidationStatus
    {
        /// <summary>
        /// All applicable items passed the requirements
        /// </summary>
        Pass,
        /// <summary>
        /// One or more applicable items did not meet the requirement
        /// </summary>
        Fail,
        /// <summary>
        /// A result has not been determined
        /// </summary>
        Inconclusive,
        /// <summary>
        /// A system or configuration failure prevented the specification from being checked
        /// </summary>
        Error,
        /// <summary>
        /// The specification was not run
        /// </summary>
        Skipped
    }
    

    /// <summary>
    /// Class to hold validation context when executing a validation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ValidationContext<T> where T: IFacet
    {
        private static readonly Dictionary<Expression<Func<T, object>>, Func<T, object>> compiledCache = 
            new Dictionary<Expression<Func<T, object>>, Func<T, object>>(new MemberExpressionComparer());

        /// <summary>
        /// Constructs a new <see cref="ValidationContext{T}"/>
        /// </summary>
        /// <param name="clause"></param>
        /// <param name="cardinality"></param>
        public ValidationContext(T clause, Cardinality cardinality)
        {
            FacetCardinality = cardinality;
            Clause = clause;
        }

        /// <summary>
        /// Gets the expected string value of member on a Facet
        /// </summary>
        /// <param name="memberField">The Facet Constraint</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public string GetExpected(Expression<Func<T, object>> memberField)
        {
            if (memberField is null)
            {
                throw new ArgumentNullException(nameof(memberField));
            }

            // decode the field we're validating from the expression
            var accessor = GetCompiled(memberField);
            return accessor?.Invoke(Clause)?.ToString() ?? "<any>";
        }

        /// <summary>
        /// Gets the name of the Facet Constraint being validated
        /// </summary>
        /// <param name="memberField">The Facet Constraint</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public string GetMember(Expression<Func<T, object>> memberField)
        {
            if (memberField is null)
            {
                throw new ArgumentNullException(nameof(memberField));
            }

            return (memberField!.Body as MemberExpression)?.Member?.Name ?? "";
            
        }
        /// <summary>
        /// Gets the Facet cardinality
        /// </summary>
        public Cardinality FacetCardinality { get; private set; } = Cardinality.Expected;   // Default to Required, not Prohibited
        /// <summary>
        /// The Facet clause being validated
        /// </summary>
        public T Clause { get; set; }

        private Func<T, object> GetCompiled([NotNull]Expression<Func<T, object>> expression)
        {
            if (compiledCache.TryGetValue(expression, out var compiled))
                return compiled;

            compiled = expression.Compile();
            compiledCache.Add(expression, compiled);
            return compiled;
        }

        private class MemberExpressionComparer : IEqualityComparer<Expression<Func<T, object>>>
        {
            public bool Equals(Expression<Func<T, object>> x, Expression<Func<T, object>> y)
            {
                return
                    (x!.Body as MemberExpression)?.Member?.Name == (y!.Body as MemberExpression)?.Member?.Name &&
                    (x!.Body as MemberExpression)?.Member?.DeclaringType == (y!.Body as MemberExpression)?.Member?.DeclaringType;
            }

            public int GetHashCode(Expression<Func<T, object>> obj)
            {
                var hash1 = (obj!.Body as MemberExpression)?.Member?.Name?.GetHashCode() ?? 0;
                var hash2 = (obj!.Body as MemberExpression)?.Member.DeclaringType?.GetHashCode() ?? 0;
                return HashCode.Combine(hash1, hash2);
            }
        }
    }
}
