using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Common.Step21;
using Xbim.IDS.Validator.Core.Extensions;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;
using static Xbim.InformationSpecifications.RequirementCardinalityOptions;

namespace Xbim.IDS.Validator.Core.Binders
{
    /// <summary>
    /// Base class to help dynamically bind IDS Facets to IModel
    /// <see cref="IQueryable{T}"/> and <see cref="IEnumerable{T}"/> <see cref="Expression"/>s
    /// enabling late-bound querying and filtering of Entities
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class FacetBinderBase<T> : IFacetBinder<T> where T: IFacet
    {
        private readonly ILogger<FacetBinderBase<T>> logger;

        /// <summary>
        /// Constructs a new <see cref="FacetBinderBase{T}"/>
        /// </summary>
        /// <param name="binderContext"></param>
        /// <param name="logger"></param>
        public FacetBinderBase(BinderContext binderContext, ILogger<FacetBinderBase<T>> logger)
        {
            BinderContext = binderContext ?? throw new ArgumentNullException(nameof(binderContext));
            this.logger = logger;
        }

        public BinderContext BinderContext { get; }

        /// <summary>
        /// The model being queried
        /// </summary>
        public IModel Model { get => BinderContext.Model ?? throw new ArgumentNullException(); }

        /// <summary>
        /// Applies a Selection and Filter predicate to the supplied <paramref name="baseExpression"/> from the <paramref name="facet"/>
        /// </summary>
        /// <param name="baseExpression"></param>
        /// <param name="facet"></param>
        /// <returns></returns>

        public abstract Expression BindSelectionExpression(Expression baseExpression, T facet);

        public abstract Expression BindWhereExpression(Expression baseExpression, T facet);

        /// <summary>
        /// Validates an entity against the requirements in this Facet
        /// </summary>
        /// <param name="item"></param>
        /// <param name="requirement"></param>
        /// <param name="result"></param>
        /// <param name="facet"></param>
        public abstract void ValidateEntity(IPersistEntity item, T facet, Cardinality requirement, IdsValidationResult result);


        protected static bool ExpressTypesAreValid(IEnumerable<ExpressType> expressTypes)
        {
            if(!expressTypes.Any())
            {
                return false;
            }
            foreach (var expressType in expressTypes)
            {
                if (!ExpressTypeIsValid(expressType))
                    return false;
            }
            return true;
        }

        protected static bool ExpressTypeIsValid(ExpressType expressType)
        {
            // Exclude invalid schema items (including un-rooted entity types like IfcLabel)
            return !(expressType == null || expressType.Properties.Count == 0);
        }

        protected ExpressMetaProperty? GetMatchingProperty(ExpressType expressType, string propName)
        {
            if (expressType == null) return null;
            return expressType.Properties.FirstOrDefault(p => p.Value.Name == propName).Value;
        }

        /// <summary>
        /// Gets all properties on an ExpressType, including Derived properties.
        /// </summary>
        /// <param name="expressType"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        protected static IEnumerable<ExpressMetaProperty> GetAllProperties(ExpressType expressType, VerificationOptions options)
        {
            if(options?.AllowDerivedAttributes == true)
            {
                return expressType.Inverses.Union(
                    expressType.Derives.Union(
                        expressType.Properties.Select(p => p.Value)
                        ));
            }
            return expressType.Properties.Select(p => p.Value);
        }

        /// <summary>
        /// Filter the supplied expression to return only types matching the supplied <paramref name="expressType"/>
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="expressType"></param>
        /// <param name="includeSubTypes"></param>
        /// <returns></returns>
        protected Expression BindIfcExpressType(Expression expression, ExpressType expressType, bool includeSubTypes = false)
        {
            if (expressType is null)
            {
                throw new ArgumentNullException(nameof(expressType));
            }


            var ofTypeMethod = ExpressionHelperMethods.EntityCollectionOfGenericType.MakeGenericMethod(expressType.Type);

            // call .OfType<IfcWall>(activated: true)
            expression = Expression.Call(expression, ofTypeMethod, Expression.Constant(true)); 
            // Now we've found all Ifc Types implementing the Type, cast to the type
            // This will include subclasses. 
            if(!includeSubTypes)
            {
                // Build expression `.Where(e => e.GetType() == expression.Type)`

                // Get underlying collection type for generic
                var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);

                // build IEnumerable.Where<TEntity>(...)
                var whereMethod = ExpressionHelperMethods.EnumerableWhereGeneric.MakeGenericMethod(collectionType);

                // build lambda param 'ent => ...'
                ParameterExpression ifcTypeParam = Expression.Parameter(collectionType, "ent");

                // build 'ent.GetType()'
                var getTypeMethod = ExpressionHelperMethods.GetTypeMethod;
                Expression entityTypeProperty = Expression.Call(ifcTypeParam, getTypeMethod);

                // 
                ConstantExpression expectedTypeExpression = Expression.Constant(expressType.Type);

                // e.GetType() == expression.Type
                Expression queryBody = Expression.Equal(entityTypeProperty, expectedTypeExpression);

                // Build Lambda expression for filter predicate (Func<T,bool>)
                var filterExpression = Expression.Lambda(queryBody, ifcTypeParam);

                // Bind Lambda to Where method
                expression =  Expression.Call(null, whereMethod, new[] { expression, filterExpression });

            }
            


            return expression;
        }

        /// <summary>
        /// Filter the supplied expression to return only types that are Defined by the supplied selection
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="selection"></param>
        /// <returns></returns>
        protected Expression BindDefiningType(Expression expression, EntitySelectionCriteria selection)
        {
            if (selection is null)
            {
                throw new ArgumentNullException(nameof(selection));
            }

            if (selection.DefiningExpressType is null)
            {
                throw new ArgumentNullException(nameof(selection.DefiningExpressType));
            }

            // Build expression `.Where(e => e.IsTypedBy != null && e.IsTypedBy.GetType() == selection.DefiningType.Type)`

            // Get underlying collection type for generic
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);

            // build IEnumerable.Where<TEntity>(...)
            var whereMethod = ExpressionHelperMethods.EnumerableWhereGeneric.MakeGenericMethod(collectionType);

            // build lambda param 'ent => ...'
            ParameterExpression ifcTypeParam = Expression.Parameter(collectionType, "ent");

            // build 'ent.IsTypedBy.GetType()'

            var getTypeMethod = ExpressionHelperMethods.GetTypeMethod;
            var isTypedByExpr = Expression.Property(ifcTypeParam, "IsTypedBy");
           
            Expression entityTypeProperty =  Expression.Call(isTypedByExpr, getTypeMethod);

            // => ent.IsDefinedBy.FirstOrDefault().GetType() == expression.Type
            Expression queryBody = Expression.Equal(entityTypeProperty, Expression.Constant(selection.DefiningExpressType.Type));

            var nullcheckExpr = Expression.NotEqual(isTypedByExpr, Expression.Constant(null, typeof(object)));

            queryBody = Expression.AndAlso(nullcheckExpr, queryBody);

            // Build Lambda expression for filter predicate (Func<T,bool>)
            var filterExpression = Expression.Lambda(queryBody, ifcTypeParam);

            // Bind Lambda to Where method
            expression = Expression.Call(null, whereMethod, new[] { expression, filterExpression });

            



            return expression;
        }

        internal Expression BindIfcExpressTypes(Expression expression, IEnumerable<string> rootTypes)
        {
            IEnumerable<ExpressType> expressTypes = GetExpressTypes(rootTypes);
            if(!ExpressTypesAreValid(expressTypes))
            {
                var types = string.Join(',', rootTypes);
                throw new InvalidOperationException($"Invalid IFC Types '{types}'");
            }

            var baseExpression = expression;
            bool doConcat = false;
            foreach (var expressType in expressTypes)
            {
                
                var rightExpr = BindIfcExpressType(baseExpression, expressType, true);
               

                // Union to main expression.
                if (doConcat)
                {
                    expression = BindConcat(expression, rightExpr);
                }
                else
                {
                    expression = rightExpr;
                    doConcat = true;
                }
            }

            return expression;
        }

        /// <summary>
        /// Amends an expression to return no results
        /// </summary>
        /// <remarks>Used when a criteria may be invalid and so expression cannot be express.
        /// E.g. Find all PropertySets with Classification 
        /// </remarks>
        /// <param name="expression">The expression to bind to</param>
        /// <returns></returns>
        protected static Expression BindNotFound(Expression expression)
        {
            return BindNotFound(expression, TypeHelper.GetImplementedIEnumerableType(expression.Type));
        }
        /// <summary>
        /// Amends an expression to return no results
        /// </summary>
        /// <remarks>Used when a criteria may be invalid and so expression cannot be express.
        /// E.g. Find all PropertySets with Classification 
        /// </remarks>
        /// <param name="expression">The expression to bind to</param>
        /// <param name="elementType">The type of the underlying expression's Enumerable</param>
        /// <returns></returns>
        protected static Expression BindNotFound(Expression expression, Type elementType)
        {
            var whereMethod = ExpressionHelperMethods.EnumerableWhereGeneric.MakeGenericMethod(elementType);
            // build lambda param 'ent => ...'
            ParameterExpression entParam = Expression.Parameter(elementType, "ent");

            //  Func (ent => false)
            var filterExpression = Expression.Lambda(Expression.Constant(false), entParam);

            return Expression.Call(null, whereMethod, new[] { expression, filterExpression });
        }

        protected IEnumerable<ExpressType> GetExpressTypes(IEnumerable<string> ifcTypes)
        {
            foreach(var type in ifcTypes)
            {
                yield return Model.Metadata.ExpressType(type.ToUpperInvariant());
            }
        }

        /// <summary>
        /// Concatenate two Enumerable expressions together casting to the highest common type
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="right"></param>
        /// <returns>The concatenated IEnumerable</returns>
        protected Expression BindConcat(Expression expression, Expression right)
        {

            // e.g. Concat an IfcObjectDefinition + IfcTypeObject => IfcObject
            Type highestCommonType = GetCommonAncestor(expression, right);
            expression = BindCast(expression, highestCommonType);

            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableConcatGeneric.MakeGenericMethod(highestCommonType), expression, right);
            return expression;
        }

        /// <summary>
        /// Casts a enumerable collection to a type
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        protected Expression BindCast(Expression expression, Type type)
        {
            return Expression.Call(null, ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(type), expression);
        }

        private Type GetCommonAncestor(Expression left, Expression right)
        {
            var leftType = TypeHelper.GetImplementedIEnumerableType(left.Type);
            var rightType = TypeHelper.GetImplementedIEnumerableType(right.Type);

            var ancestors = new HashSet<ExpressType>();
            var express = Model.Metadata.ExpressType(leftType);
            while(express != null)
            {
                ancestors.Add(express);
                express = express.SuperType;
            }
            express = Model.Metadata.ExpressType(rightType);
            while (express != null)
            {
                if(ancestors.Contains(express))
                {
                    return express.Type;
                }
                express = express.SuperType;
            }
            return typeof(IPersistEntity);
        }

        protected IIfcValue? UnwrapQuantity(IIfcPhysicalQuantity quantity)
        {
            return quantity.UnwrapQuantity();
        }

        /// <summary>
        /// Gets the primitive value in the standard IDS ISO units.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected object GetNormalisedValue(IIfcValue? value)
        {
            object result = HandleBoolConventions(value);
            if (result is IIfcMeasureValue measure)
            {
                var units = GetUnits();
                result = measure.NormaliseUnits(units);
            }

            // TODO: Review if we have to do anything with derived
            //else if(result is IIfcDerivedMeasureValue derived)
            //{
            //    var units = GetUnits();
            //    result = derived.NormaliseUnits(units);
            //}
            if (result is IIfcValue v)
            {
                result = v.Value;   // Unpack the primitive object
            }

            return result;
        }

        protected IIfcUnitAssignment? GetUnits()
        {
            var project = Model.Instances.OfType<IIfcProject>().First();
            

            return project?.UnitsInContext ?? default;
        }

        protected bool IsIfc2x3Model()
        {
            return Model.SchemaVersion == XbimSchemaVersion.Ifc2X3;
        }

        protected bool IsIfc4x3Model()
        {
#if XbimV6
            return Model.SchemaVersion == XbimSchemaVersion.Ifc4x3;
#else
            return false;
#endif
        }

        protected bool IsCobieModel()
        {
            return Model.SchemaVersion == XbimSchemaVersion.Cobie2X4;
        }

        protected static object HandleBoolConventions(object attrvalue)
        {
            if (attrvalue is IExpressBooleanType ifcbool)
            {
                // IDS Specs expect bools to be lower case
                attrvalue = ifcbool.Value.ToString().ToLowerInvariant();
            }

            return attrvalue;
        }

        protected static bool IsValueRelevant(object? value)
        {
            if (value == null) return false;
            if (value is IIfcSimpleValue sv && string.IsNullOrEmpty(sv.Value?.ToString())) return false;
            if (value is string str && string.IsNullOrEmpty(str)) return false;
            if (value is IList list && list.Count == 0) return false;

            return true;
        }

        // We should not attempt pattern matches on anything but strings
        protected static bool IsTypeAppropriateForConstraint(ValueConstraint attributeValue, object? attrvalue)
        {
            
            if (!attributeValue.IsNullOrEmpty() && attributeValue.AcceptedValues.Any(v => v is PatternConstraint))
            {
                return (attrvalue is string || attrvalue is IExpressStringType);
            }
            return true;
        }

        /// <summary>
        /// Creates a context we use to track shared validation info for results
        /// </summary>
        /// <param name="cardinality"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        public ValidationContext<T> CreateValidationContext(Cardinality? cardinality, T facet)
        {
            return new ValidationContext<T>(facet, cardinality ?? Cardinality.Expected);
        }

        Expression IFacetBinder.BindSelectionExpression(Expression baseExpression, IFacet facet)
        {
            return BindSelectionExpression(baseExpression, (T)facet);
        }

        Expression IFacetBinder.BindWhereExpression(Expression baseExpression, IFacet facet)
        {
            return BindWhereExpression(baseExpression, (T)facet);
        }

        void IFacetBinder.ValidateEntity(IPersistEntity item, IFacet facet, Cardinality cardinality, IdsValidationResult result)
        {
            ValidateEntity(item, (T)facet, cardinality, result);
        }

    }
}
