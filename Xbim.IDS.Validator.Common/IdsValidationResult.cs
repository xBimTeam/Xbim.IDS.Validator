using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        /// <summary>
        /// Creates a validation result for the supplied Ifc entity and set of requirements.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="requirement"></param>
        /// <param name="logFullEntity">Determines if the full xbim entity is returned in the result, or just the entityId</param>
        public IdsValidationResult(IPersistEntity? entity, FacetGroup? requirement, bool logFullEntity = false)
        {
       
            ValidationStatus = ValidationStatus.Inconclusive;
            Entity = entity?.EntityLabel;
            if(logFullEntity)
                FullEntity = entity;
            Requirement = requirement;
        }

        /// <summary>
        /// The validation status of an individual entity for a requirement
        /// </summary>
        public ValidationStatus ValidationStatus { get; set; }
        
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
        /// The set of messages raised by the validation process against this entity
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
    }

    /// <summary>
    /// A message from the IDS validation run
    /// </summary>
    public class ValidationMessage
    {
        


        // Gets the status based on current Expectation mode - i.e. Failure to match in Prohibited model = Success
        private static ValidationStatus GetStatus(RequirementCardinalityOptions expectation, bool? success)
        {
            switch (expectation)
            {
                case RequirementCardinalityOptions.Expected:
                    return success == true ? ValidationStatus.Pass : success == false ? ValidationStatus.Fail : ValidationStatus.Inconclusive;

                case RequirementCardinalityOptions.Prohibited:
                    return success == true ? ValidationStatus.Fail : success == false ? ValidationStatus.Pass : ValidationStatus.Inconclusive;

                case RequirementCardinalityOptions.Optional:
                default:
                    return ValidationStatus.Inconclusive;

            }
           
            
        }


        /// <inheritDoc/>
        public override string ToString()
        {
            if(Status == ValidationStatus.Fail)
            {
                return $"[{Status}] {Expectation} {Clause?.GetType().Name}.{ValidatedField} to be {ExpectedResult} - but actually found '{ActualResult}'";
            }
            else
            {
                return $"[{Status}] {Expectation} {Clause?.GetType().Name}.{ValidatedField} to be {ExpectedResult} and found '{ActualResult}'";
            }
        }

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
                Expectation = context.ExpectationMode,
                ValidatedField = context.GetMember(memberField),
                EntityAffected = entity
            };
        }

        /// <summary>
        /// Builds a messge representing an Error when validating
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public static ValidationMessage Error(string reason)
        {
            return new ValidationMessage
            {
                Status = ValidationStatus.Error,
                Reason = reason,
                Expectation = RequirementCardinalityOptions.Expected
            };
        }

        /// <summary>
        /// Default Constructot
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
        public ValidationMessage(ValidationStatus status, RequirementCardinalityOptions expectation, IFacet clause, string? reason = null, object? expectedResult = null, object? actualResult = null)
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
        public ValidationStatus Status { get; set; }
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
        public RequirementCardinalityOptions Expectation { get; set; }
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
        public string FormatedActualResult => string.IsNullOrEmpty(ActualResult?.ToString()) ? "<nothing>" : ActualResult.ToString()


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
        /// A result could not be determined
        /// </summary>
        Inconclusive,
        /// <summary>
        /// A system or configuration failure prevented the specification from being checked
        /// </summary>
        Error
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
        /// <param name="expectationMode"></param>
        public ValidationContext(T clause, RequirementCardinalityOptions expectationMode)
        {
            ExpectationMode = expectationMode;
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
        /// Gets and sets the expectation mode
        /// </summary>
        public RequirementCardinalityOptions ExpectationMode { get; set; } = RequirementCardinalityOptions.Expected;   // Default to Required, not Prohibited
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
