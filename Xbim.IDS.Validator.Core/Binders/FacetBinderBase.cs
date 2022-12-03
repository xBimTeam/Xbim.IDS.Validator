using System.Linq.Expressions;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.Interfaces;
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
    }
}
