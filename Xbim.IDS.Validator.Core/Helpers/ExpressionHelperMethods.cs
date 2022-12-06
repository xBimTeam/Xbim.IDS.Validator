using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reflection;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Extensions;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Helpers
{

#nullable disable

    internal class ExpressionHelperMethods
    {

        private static MethodInfo _entityCollectionofTypeMethod = typeof(IReadOnlyEntityCollection).GetMethod(nameof(IReadOnlyEntityCollection.OfType), new Type[] { typeof(string), typeof(bool) });
        private static MethodInfo _enumerableIfcPropertySinglePropsValueMethod = typeof(IfcExtensions).GetMethod(nameof(IfcExtensions.GetIfcPropertySingleValues), new Type[] { typeof(IEnumerable<IIfcRelDefinesByProperties>), typeof(string), typeof(string), typeof(string) });
        private static MethodInfo _enumerableIfcMaterialSelectorMethod = typeof(IfcExtensions).GetMethod(nameof(IfcExtensions.GetIfcObjectsUsingMaterials), new Type[] { typeof(IEnumerable<IIfcRelAssociatesMaterial>), typeof(MaterialFacet) });
        private static MethodInfo _enumerableIfcAssociatesClassificationMethod = typeof(IfcExtensions).GetMethod(nameof(IfcExtensions.GetIfcObjectsUsingClassification), new Type[] { typeof(IEnumerable<IIfcRelAssociatesClassification>), typeof(IfcClassificationFacet) });

        private static MethodInfo _idsValidationIsSatisifiedMethod = typeof(IValueConstraintComponent).GetMethod(nameof(IValueConstraintComponent.IsSatisfiedBy), new Type[] { typeof(object), typeof(ValueConstraint), typeof(bool), typeof(ILogger) });


        private static MethodInfo _enumerableWhereMethod = GenericMethodOf(_ => Enumerable.Where<int>(default(IEnumerable<int>), default(Func<int, bool>)));
        private static MethodInfo _enumerableCastMethod = GenericMethodOf(_ => Enumerable.Cast<int>(default(IEnumerable<int>)));

        private static MethodInfo _enumerableConcatMethod = GenericMethodOf(_ => Enumerable.Concat<int>(default(IEnumerable<int>), default(IEnumerable<int>)));


        public static MethodInfo EnumerableWhereGeneric
        {
            get { return _enumerableWhereMethod; }
        }

        public static MethodInfo EntityCollectionOfType
        {
            get { return _entityCollectionofTypeMethod; }
        }

        public static MethodInfo IdsValidationIsSatisifiedMethod
        {
            get { return _idsValidationIsSatisifiedMethod; }
        }



        public static MethodInfo EnumerableIfcPropertySinglePropsValue
        {
            get { return _enumerableIfcPropertySinglePropsValueMethod; }
        }

        public static MethodInfo EnumerableIfcMaterialSelector
        {
            get { return _enumerableIfcMaterialSelectorMethod; }
        }

        public static MethodInfo EnumerableIfcClassificationSelector
        {
            get { return _enumerableIfcAssociatesClassificationMethod; }
        }

        

        public static MethodInfo EnumerableCastGeneric
        {
            get { return _enumerableCastMethod; }
        }

        public static MethodInfo EnumerableConcatGeneric
        {
            get { return _enumerableConcatMethod; }
        }

        private static MethodInfo GenericMethodOf<TReturn>(Expression<Func<object, TReturn>> expression)
        {
            return GenericMethodOf(expression as Expression);
        }

        private static MethodInfo GenericMethodOf(Expression expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            LambdaExpression lambdaExpression = expression as LambdaExpression;

            //Contract.Assert(expression.NodeType == ExpressionType.Lambda);
            //Contract.Assert(lambdaExpression != null);
            //Contract.Assert(lambdaExpression.Body.NodeType == ExpressionType.Call);

            return (lambdaExpression.Body as MethodCallExpression).Method.GetGenericMethodDefinition();
        }

        internal static bool IsIQueryable(Type type)
        {
            return typeof(IQueryable).IsAssignableFrom(type);
        }
    }

}
