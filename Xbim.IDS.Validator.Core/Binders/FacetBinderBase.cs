using Microsoft.Extensions.Logging;
using System.Collections;
using System.Linq.Expressions;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.IDS.Validator.Core.Extensions;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;
using Xbim.InformationSpecifications;

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
        /// <summary>
        /// Constructs a new <see cref="FacetBinderBase{T}"/>
        /// </summary>
        /// <param name="model"></param>
        public FacetBinderBase(IModel model)
        {
            Model = model;
        }

        /// <summary>
        /// The model being queried
        /// </summary>
        public IModel Model { get; }

        /// <summary>
        /// Applies a Filter predicate to the supplied <paramref name="baseExpression"/> from the <paramref name="facet"/>
        /// </summary>
        /// <param name="baseExpression"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        public abstract Expression BindFilterExpression(Expression baseExpression, T facet);

        /// <summary>
        /// Validates an entity against the requirements in this Facet
        /// </summary>
        /// <param name="item"></param>
        /// <param name="requirement"></param>
        /// <param name="logger"></param>
        /// <param name="result"></param>
        /// <param name="facet"></param>
        public abstract void ValidateEntity(IPersistEntity item, FacetGroup requirement, ILogger logger, IdsValidationResult result, T facet);


        protected static void ValidateExpressTypes(IEnumerable<ExpressType> expressTypes)
        {
            foreach (var expressType in expressTypes)
            {
                ValidateExpressType(expressType);
            }
        }

        protected static void ValidateExpressType(ExpressType expressType)
        {
            // Exclude invalid schema items (including un-rooted entity types like IfcLabel)
            if (expressType == null || expressType.Properties.Count == 0)
            {
                throw new InvalidOperationException($"Invalid IFC Type '{expressType?.Name}'");
            }
        }

        /// <summary>
        /// Filter the supplied expression to return only types matching the supplied <paramref name="expressType"/>
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="expressType"></param>
        /// <returns></returns>
        protected Expression BindIfcExpressType(Expression expression, ExpressType expressType)
        {
            if (expressType is null)
            {
                throw new ArgumentNullException(nameof(expressType));
            }

            var ofTypeMethod = ExpressionHelperMethods.EntityCollectionOfType;

            var entityTypeName = Expression.Constant(expressType.Name, typeof(string));
            var activate = Expression.Constant(true, typeof(bool));
            // call .OfType("IfcWall", true)
            expression = Expression.Call(expression, ofTypeMethod, entityTypeName, activate);   // TODO: switch to Generic sig

            // TODO: Currently requred just to pass the Type downstream for other Facets
            // call .Cast<EntityType>()
            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(expressType.Type), expression);

            return expression;
        }

        internal Expression BindIfcExpressTypes(Expression expression, string[] rootTypes)
        {
            IEnumerable<ExpressType> expressTypes = GetExpressTypes(rootTypes);
            ValidateExpressTypes(expressTypes);

            bool doConcat = false;
            foreach (var expressType in expressTypes)
            {
                //var rightExpr = expression;
                var rightExpr = BindIfcExpressType(expression, expressType);
               

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

        private IEnumerable<ExpressType> GetExpressTypes(string[] ifcTypes)
        {
            foreach(var type in ifcTypes)
            {
                yield return Model.Metadata.ExpressType(type.ToUpperInvariant());
            }
        }

        /// <summary>
        /// Concatenate two Enumerable expressions together
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="right"></param>
        /// <returns>The concatenated IEnumerable</returns>
        protected Expression BindConcat(Expression expression, Expression right)
        {
            // TODO: check is IfcRoot Element

            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(typeof(IIfcRoot)), expression);

            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableConcatGeneric.MakeGenericMethod(typeof(IIfcRoot)), expression, right);
            return expression;
        }

        protected object? ApplyWorkarounds([MaybeNull] object? value)
        {
            // Workaround for a bug in XIDS Satisfied test where we don't coerce numeric types correctly
            if (value is long l)
                return Convert.ToDouble(l);


            if (value is int i)
                return Convert.ToDouble(i);

            return value;
        }

        protected IIfcValue? UnwrapQuantity(IIfcPhysicalQuantity quantity)
        {
            if (quantity is IIfcQuantityCount c)
                return c.CountValue;
            if (quantity is IIfcQuantityArea area)
                return area.AreaValue;
            else if (quantity is IIfcQuantityLength l)
                return l.LengthValue;
            else if (quantity is IIfcQuantityVolume v)
                return v.VolumeValue;
            if (quantity is IIfcQuantityWeight w)
                return w.WeightValue;
            if (quantity is IIfcQuantityTime t)
                return t.TimeValue;
            if (quantity is IIfcPhysicalComplexQuantity comp)
                return default;


            throw new NotImplementedException(quantity.GetType().Name);
        }

        protected object UnwrapValue(IIfcValue? value)
        {
            object result = HandleBoolConventions(value);
            if (result is IIfcMeasureValue)
            {
                result = HandleUnitConversion(value);
            }
            if (result is IIfcValue v)
            {
                result = v.Value;
            }

            return result;
        }

        protected IIfcUnitAssignment GetUnits()
        {
            var project = Model.Instances.OfType<IIfcProject>().First();

            return project.UnitsInContext;
        }

        protected IIfcValue HandleUnitConversion(IIfcValue value)
        {
            var units = GetUnits() as IfcUnitAssignment;

            if (units == null) return value;

            // TODO: handle 2x3
            if (value is IfcCountMeasure c)
                return c;
            if (value is IfcAreaMeasure area)
            {
                var unit = units.AreaUnit;
                if (unit is IIfcSIUnit si)
                {
                    return new IfcAreaMeasure(area * si.Power);
                }
                return area;
            }
            else if (value is IfcLengthMeasure l)
            {
                var unit = units.LengthUnit;
                if (unit is IIfcSIUnit si)
                {
                    return new IfcLengthMeasure(l * si.Power);
                }
                return l;
            }
            else if (value is IfcVolumeMeasure v)
            {
                var unit = units.VolumeUnit;
                if (unit is IIfcSIUnit si)
                {
                    return new IfcMassMeasure(v * si.Power);
                }
                return v;
            }

            //if (value is IfcMassMeasure w)
            //{
            //    var unit = units.GetUnitFor(w);
            //    if (unit is IIfcSIUnit si)
            //    {
            //        return new IfcMassMeasure(v * si.Power);
            //    }
            //    return w;
            //}

            if (value is IfcTimeMeasure t)
                return t;
            else
                return value;
            //throw new NotImplementedException(value.GetType().Name);
        }

        protected static object HandleBoolConventions(object attrvalue)
        {
            if (attrvalue is IExpressBooleanType ifcbool)
            {
                // IDS Specs expect bools to be upper case
                attrvalue = ifcbool.Value.ToString().ToUpperInvariant();
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

        /// <summary>
        /// Creates a context we use to track shared validation info for results
        /// </summary>
        /// <param name="requirement"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        public ValidationContext<T> CreateValidationContext(FacetGroup requirement, T facet)
        {
            // Set the Requirement expectation - Required, Optional, Prohibit so we negate Success/Failure

            var required = requirement.IsRequired(facet);
            var expectation = required == true ? Expectation.Required : required == false ? Expectation.Prohibited : Expectation.Optional;
            return new ValidationContext<T>(facet, expectation);
        }
    }
}
