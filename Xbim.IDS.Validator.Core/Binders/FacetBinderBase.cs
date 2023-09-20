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
        public FacetBinderBase(BinderContext binderContext)
        {
            BinderContext = binderContext ?? throw new ArgumentNullException(nameof(binderContext));

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
        /// <param name="logger"></param>
        /// <param name="result"></param>
        /// <param name="facet"></param>
        public abstract void ValidateEntity(IPersistEntity item, T facet, RequirementCardinalityOptions requirement, IdsValidationResult result);


        protected static bool ExpressTypesAreValid(IEnumerable<ExpressType> expressTypes)
        {
            if(!expressTypes.Any())
            {
                throw new InvalidOperationException($"No matching IFC type");
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

        /// <summary>
        /// Filter the supplied expression to return only types matching the supplied <paramref name="expressType"/>
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="expressType"></param>
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

        internal Expression BindIfcExpressTypes(Expression expression, string[] rootTypes)
        {
            IEnumerable<ExpressType> expressTypes = GetExpressTypes(rootTypes);
            if(!ExpressTypesAreValid(expressTypes))
            {
                var types = string.Join(',', rootTypes);
                throw new InvalidOperationException($"Invalid IFC Types '{types}'");
            }

            bool doConcat = false;
            foreach (var expressType in expressTypes)
            {
                //var rightExpr = expression;
                var rightExpr = BindIfcExpressType(expression, expressType, true);
               

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

            // e.g. Concat an IfcObjectDefinition + IfcTypeObject => IfcObject
            Type highestCommonType = GetCommonAncestor(expression, right);
            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(highestCommonType), expression);

            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableConcatGeneric.MakeGenericMethod(highestCommonType), expression, right);
            return expression;
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
            return typeof(IIfcRoot);
        }

        protected object? ApplyWorkarounds([MaybeNull] object? value, ValueConstraint constraint)
        {
            // Workaround for a bug in XIDS Satisfied test where we don't coerce numeric types correctly
            switch(constraint.BaseType)
            {
                case NetTypeName.Integer:
                    {
                        if (value is double || value is float)
                        {
                            return Convert.ToInt32(value);
                        }
                        break;
                    }

                case NetTypeName.Double:
                case NetTypeName.Undefined:
                case NetTypeName.Floating:
                    {
                        if (value is long l)
                            return Convert.ToDouble(l);


                        if (value is int i)
                            return Convert.ToDouble(i);
                        break;
                    }

            }
            

            return value;
        }

        protected IIfcValue? UnwrapQuantity(IIfcPhysicalQuantity quantity)
        {
            return quantity.UnwrapQuantity();
        }

        protected object UnwrapValue(IIfcValue? value)
        {
            object result = HandleBoolConventions(value);
            if (result is IIfcMeasureValue)
            {
                if(IsIfc2x3Model())
                {
                    result = HandleUnitConversionIfc2x3(value);
                }
                else if(IsIfc4x3Model())
                {
                    result = HandleUnitConversionIfc4x3(value);
                }
                else
                {
                    result = HandleUnitConversionIfc4(value);
                }
            }
            if (result is IIfcValue v)
            {
                result = v.Value;
            }

            return result;
        }

        protected IIfcUnitAssignment? GetUnits()
        {
            var project = Model.Instances.OfType<IIfcProject>().First();
            

            return project?.UnitsInContext ?? default;
        }

        
        protected IIfcValue HandleUnitConversionIfc4(IIfcValue value)
        {
            var units = GetUnits() as IfcUnitAssignment;

            if (units == null) return value;

            
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

            // TODO Add remaining measures
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

        protected IIfcValue HandleUnitConversionIfc4x3(IIfcValue value)
        {
            var units = GetUnits() as Ifc4x3.MeasureResource.IfcUnitAssignment;

            if (units == null) return value;


            if (value is IfcCountMeasure c)
                return c;
            if (value is IfcAreaMeasure area)
            {
                var unit = units.AreaUnit();
                if (unit is IIfcSIUnit si)
                {
                    return new IfcAreaMeasure(area * si.Power);
                }
                return area;
            }
            else if (value is IfcLengthMeasure l)
            {
                var unit = units.LengthUnit();
                if (unit is IIfcSIUnit si)
                {
                    return new IfcLengthMeasure(l * si.Power);
                }
                return l;
            }
            else if (value is IfcVolumeMeasure v)
            {
                var unit = units.VolumeUnit();
                if (unit is IIfcSIUnit si)
                {
                    return new IfcMassMeasure(v * si.Power);
                }
                return v;
            }

            // TODO Add remaining measures
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


        protected IIfcValue HandleUnitConversionIfc2x3(IIfcValue value)
        {
            var units = GetUnits() as Ifc2x3.MeasureResource.IfcUnitAssignment;

            if (units == null) return value;


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

            // TODO Add remaining measures
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

        protected bool IsIfc2x3Model()
        {
            return Model.SchemaVersion == XbimSchemaVersion.Ifc2X3;
        }

        protected bool IsIfc4x3Model()
        {
            return Model.SchemaVersion == XbimSchemaVersion.Ifc4x3;
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

        // We should not attempt pattern matches on anything but strings
        protected static bool IsTypeAppropriateForConstraint(ValueConstraint attributeValue, object? attrvalue)
        {
            if (attributeValue.AcceptedValues.Any(v => v is PatternConstraint))
            {
                return (attrvalue is string);
            }
            return true;
        }

        /// <summary>
        /// Creates a context we use to track shared validation info for results
        /// </summary>
        /// <param name="requirement"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        public ValidationContext<T> CreateValidationContext(RequirementCardinalityOptions requirement, T facet)
        {
            // Set the Requirement expectation - Required, Optional, Prohibit so we negate Success/Failure

            //var required = requirement.IsRequired(facet);
            //var expectation = required == true ? Expectation.Required : required == false ? Expectation.Prohibited : Expectation.Optional;
            return new ValidationContext<T>(facet, requirement);
        }

        Expression IFacetBinder.BindSelectionExpression(Expression baseExpression, IFacet facet)
        {
            return BindSelectionExpression(baseExpression, (T)facet);
        }

        Expression IFacetBinder.BindWhereExpression(Expression baseExpression, IFacet facet)
        {
            return BindWhereExpression(baseExpression, (T)facet);
        }

        void IFacetBinder.ValidateEntity(IPersistEntity item, IFacet facet, RequirementCardinalityOptions requirement, IdsValidationResult result)
        {
            ValidateEntity(item, (T)facet, requirement, result);
        }

    }
}
