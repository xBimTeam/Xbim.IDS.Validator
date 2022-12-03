using System.Linq.Expressions;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Binders
{
#nullable disable
    public class PsetFacetBinder : FacetBinderBase<IfcPropertyFacet>
    {

        public PsetFacetBinder(IModel model) : base(model)
        {

        }

        /// <summary>
        /// Binds an IFC property filter to an expression, where propertoes are IFC Pset and Quantity fields
        /// </summary>
        /// <remarks>e.g Where(p=> p.RelatingPropertyDefinition... )</remarks>
        /// <param name="baseExpression"></param>
        /// <param name="psetFacet"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public override Expression BindFilterExpression(Expression baseExpression, IfcPropertyFacet psetFacet)
        {
            if (baseExpression is null)
            {
                throw new ArgumentNullException(nameof(baseExpression));
            }

            if (psetFacet is null)
            {
                throw new ArgumentNullException(nameof(psetFacet));
            }

            if (!psetFacet.IsValid())
            {
                // IsValid checks against a know list of all IFC Attributes
                throw new InvalidOperationException($"IFC Property Facet '{psetFacet?.PropertySetName}'.{psetFacet?.PropertyName} is not valid");
            }


            var expression = baseExpression;
            // When an Ifc Type has not yet been specified, we start with the RelDefinesByProperties
            // TODO: Types
            if (expression.Type.IsInterface && expression.Type.IsAssignableTo(typeof(IEntityCollection)))
            {
                expression = BindIfcExpressType(expression, Model.Metadata.ExpressType(typeof(IfcRelDefinesByProperties)));
            }

            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);
            var expressType = Model.Metadata.ExpressType(collectionType);
            ValidateExpressType(expressType);

            expression = BindEqualPsetFilter(expression, expressType, psetFacet);
            return expression;
        }


        private Expression BindEqualPsetFilter(Expression expression, ExpressType expressType, IfcPropertyFacet psetFacet)
        {
            if (psetFacet is null)
            {
                throw new ArgumentNullException(nameof(psetFacet));
            }
            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);

            //var constraints = constraint.AcceptedValues;

            // call .Cast<EntityType>()
            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(collectionType), expression);

            if (psetFacet?.PropertySetName?.AcceptedValues?.Any() == false ||
                psetFacet?.PropertyName?.AcceptedValues?.Any() == false)
            {
                return expression;
            }

            var psetName = psetFacet.PropertySetName.SingleValue();
            var propName = psetFacet.PropertyName.SingleValue();
            var propValue = psetFacet.PropertyValue.SingleValue();

            var psetNameExpr = Expression.Constant(psetName, typeof(string));
            var propNameExpr = Expression.Constant(propName, typeof(string));
            var propValExpr = Expression.Constant(propValue, typeof(string));
            // Expression we're building
            // var psetRelDefines = model.Instances.OfType<IIfcRelDefinesByProperties>();
            // var entities = IfcExtensions.GetIfcPropertySingleValues(psetRelDefines, psetName, propName, propValue);


            var propsMethod = ExpressionHelperMethods.EnumerableIfcPropertySinglePropsValue;

            return Expression.Call(null, propsMethod, new[] { expression, psetNameExpr, propNameExpr, propValExpr });

            /*



            // IEnumerable.Where<TEntity>(...)
            var whereMethod = ExpressionHelperMethods.EnumerableWhereGeneric.MakeGenericMethod(collectionType);

            // build lambda param 'ent => ...'
            ParameterExpression ifcTypeParam = Expression.Parameter(collectionType, "ent");
            
            // build 'ent.AttributeName'
            Expression nameProperty = Expression.Property(ifcTypeParam, ifcAttributePropInfo);

            var propType = ifcAttributePropInfo.PropertyType;
            var isNullWrapped = TypeHelper.IsNullable(propType);
            var underlyingType = isNullWrapped ? Nullable.GetUnderlyingType(propType) : propType;

            Expression querybody = Expression.Empty();

            bool applyOr = false;
            foreach (var ifcAttributeValue in constraints)
            {
                Expression rightExpr;

                switch (ifcAttributeValue)
                {
                    case ExactConstraint e:

                        string exactValue = e.Value;
                        // Get the Constant
                        rightExpr = BuildAttributeValueConstant(isNullWrapped, underlyingType, exactValue);
                        nameProperty = SetAttributeProperty(nameProperty, underlyingType);
                        // Binding Equals(x,y)
                        rightExpr = Expression.Equal(nameProperty, rightExpr);
                        break;

                    case PatternConstraint p:
                        // Build a query that builds an expression that delegates to XIDS's IsSatisfied regex method.
                        // model.Instances.OfType<IIfcWall>().Where(ent => patternconstraint.IsSatisfiedBy(w.Name.ToString(), <AttributeValue>, true, null));
                        //                                                 instance,         methodIn new[]{rightExpr, constraintExpr,   case, logger }
                        var isSatisfiedMethod = ExpressionHelperMethods.IdsValidationIsSatisifiedMethod;
                        // Get Property: entity.<attribute>
                        rightExpr = BuildAttributeValueRegexPredicate(nameProperty, isNullWrapped, underlyingType, p);
                        var constraintExpr = Expression.Constant(constraint, typeof(ValueConstraint));
                        var caseInsensitive = Expression.Constant(true, typeof(bool));
                        var loggerExpr = Expression.Constant(null, typeof(ILogger));
                        var instanceExpr = Expression.Constant(p, typeof(PatternConstraint));
                        rightExpr = Expression.Call(instanceExpr, isSatisfiedMethod, new[] { rightExpr, constraintExpr, caseInsensitive, loggerExpr });

                        break;
                    case StructureConstraint s:
                    case RangeConstraint r:
                        throw new NotSupportedException(ifcAttributeValue.GetType().Name);

                    default:
                        throw new NotImplementedException(ifcAttributeValue.GetType().Name);
                }

                // Or the expressions on subsequent iterations.
                if (applyOr)
                {
                    querybody = Expression.Or(querybody, rightExpr);
                }
                else
                {
                    querybody = rightExpr;
                    applyOr = true;
                }
            }

            // Build Lambda expression for filter predicate (Func<T,bool>)
            var filterExpression = Expression.Lambda(querybody, ifcTypeParam);

            // Bind Lambda to Where method
            return Expression.Call(null, whereMethod, new[] { expression, filterExpression });
            */
            //return expression;
        }

        // For Selections on instances we don't need to use expressions. When filtering we will
        /// <summary>
        /// Gets a specific property for an entity, matching a psetName and property name
        /// </summary>
        /// <param name="entityLabel"></param>
        /// <param name="psetName"></param>
        /// <param name="propName"></param>
        /// <returns></returns>
        public IIfcValue GetProperty(int entityLabel, string psetName, string propName)
        {
            var entity = Model.Instances[entityLabel];

            IIfcPropertySingleValue psetValue;
            if (entity is IIfcTypeObject type)
            {
                psetValue = type.HasPropertySets.OfType<IIfcPropertySet>()
                    .Where(p => p.Name == psetName)
                    .SelectMany(p => p.HasProperties.Where(ps => ps.Name == propName)
                        .OfType<IIfcPropertySingleValue>())
                    .FirstOrDefault();
            }
            else if (entity is IIfcObject)
            {
                psetValue = Model.Instances.OfType<IIfcRelDefinesByProperties>()
                    .Where(r => r.RelatedObjects.Any(o => o.EntityLabel == entityLabel))
                    .Where(r => r.RelatingPropertyDefinition is IIfcPropertySet ps && ps.Name == psetName)
                    .SelectMany(p => ((IIfcPropertySet)p.RelatingPropertyDefinition)
                        .HasProperties.Where(ps => ps.Name == propName)
                        .OfType<IIfcPropertySingleValue>())
                    .FirstOrDefault();
            }
            else
            {
                return null;
            }

            return psetValue?.NominalValue;

        }

    }
}
