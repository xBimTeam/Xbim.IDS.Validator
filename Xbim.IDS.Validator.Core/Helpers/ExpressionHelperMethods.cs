using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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


        private static MethodInfo _getTypeMethod = typeof(object).GetMethod(nameof(Object.GetType));

        private static MethodInfo _enumerableOfXbimTypeMethod = typeof(IReadOnlyEntityCollection).GetMethods().First(m =>
            m.Name == nameof(IReadOnlyEntityCollection.OfType) &&
            m.IsGenericMethod &&
            m.GetParameters().Length == 1 &&
            m.ReturnType.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        
        private static MethodInfo _enumerableIfcObjectsWithPropertiesMethod = typeof(IfcExtensions).GetMethod(nameof(IfcExtensions.GetIfcObjectsWithProperties), new Type[] { typeof(IEnumerable<IIfcRelDefinesByProperties>), typeof(IfcPropertyFacet)});
        private static MethodInfo _enumerableIfcMaterialSelectorMethod = typeof(IfcExtensions).GetMethod(nameof(IfcExtensions.GetIfcObjectsUsingMaterials), new Type[] { typeof(IEnumerable<IIfcRelAssociatesMaterial>), typeof(MaterialFacet) });
        private static MethodInfo _enumerableIfcAssociatesClassificationMethod = typeof(IfcExtensions).GetMethod(nameof(IfcExtensions.GetIfcObjectsUsingClassification), new Type[] { typeof(IEnumerable<IIfcRelAssociatesClassification>), typeof(IfcClassificationFacet) });
        private static MethodInfo _enumerableIfcPartofRelatedMethod = typeof(IfcExtensions).GetMethod(nameof(IfcExtensions.GetRelatedIfcObjects), new Type[] { typeof(IEnumerable<IIfcRelationship>), typeof(PartOfFacet) });
        
        private static MethodInfo _enumerableWhereAssociatedWithClassificationMethod = typeof(IfcExtensions).GetMethod(nameof(IfcExtensions.WhereAssociatedWithClassification), new Type[] { typeof(IEnumerable<IIfcObjectDefinition>), typeof(IfcClassificationFacet) });
        private static MethodInfo _enumerableWhereAssociatedWithMaterialMethod = typeof(IfcExtensions).GetMethod(nameof(IfcExtensions.WhereAssociatedWithMaterial), new Type[] { typeof(IEnumerable<IIfcObjectDefinition>), typeof(MaterialFacet) });
        private static MethodInfo _enumerableWhereObjAssociatedWithPropertyMethod = typeof(IfcExtensions).GetMethod(nameof(IfcExtensions.WhereAssociatedWithProperty), new Type[] { typeof(IEnumerable<IIfcObject>), typeof(IfcPropertyFacet) });
        private static MethodInfo _enumerableWhereTypeAssociatedWithPropertyMethod = typeof(IfcExtensions).GetMethod(nameof(IfcExtensions.WhereAssociatedWithProperty), new Type[] { typeof(IEnumerable<IIfcTypeObject>), typeof(IfcPropertyFacet) });
        private static MethodInfo _enumerableWhereObjectPartOfMethod = typeof(IfcExtensions).GetMethod(nameof(IfcExtensions.WhereHasPartOfRelationship), new Type[] { typeof(IEnumerable<IIfcObjectDefinition>), typeof(PartOfFacet) });

        //

        private static MethodInfo _idsValidationIsSatisifiedMethod = typeof(IValueConstraintComponent).GetMethod(nameof(IValueConstraintComponent.IsSatisfiedBy), new Type[] { typeof(object), typeof(ValueConstraint), typeof(bool), typeof(ILogger) });
        private static MethodInfo _idsSatisfiesConstraintMethod = typeof(ValueConstraintExtensions).GetMethod(nameof(ValueConstraintExtensions.SatisifesConstraint), new Type[] { typeof(ValueConstraint), typeof(object) });


        private static MethodInfo _enumerableWhereMethod = GenericMethodOf(_ => Enumerable.Where<int>(default(IEnumerable<int>), default(Func<int, bool>)));
        private static MethodInfo _enumerableCastMethod = GenericMethodOf(_ => Enumerable.Cast<int>(default(IEnumerable<int>)));
        private static MethodInfo _enumerableOfTypeMethod = GenericMethodOf(_ => Enumerable.OfType<int>(default(IEnumerable<int>)));
        private static MethodInfo _enumerableSelectMethod = GenericMethodOf(_ => Enumerable.Select<int, int>(default(IEnumerable<int>), i => i));
        private static MethodInfo _enumerableSelectManyMethod = GenericMethodOf(_ => Enumerable.SelectMany<int, int>(default(IEnumerable<int>), default(Func<int, IEnumerable<int>>)));
        private static MethodInfo _enumerableConcatMethod = GenericMethodOf(_ => Enumerable.Concat<int>(default(IEnumerable<int>), default(IEnumerable<int>)));

        private static MethodInfo _enumerableFirstOrDefaultMethod = GenericMethodOf(_ => Enumerable.FirstOrDefault<int>(default(IEnumerable<int>)));

        public static MethodInfo GetTypeMethod
        {
            get { return _getTypeMethod; }
        }

        public static MethodInfo EnumerableWhereGeneric
        {
            get { return _enumerableWhereMethod; }
        }

        public static MethodInfo EntityCollectionOfGenericType
        {
            get { return _enumerableOfXbimTypeMethod; }
        }


        public static MethodInfo IdsValidationIsSatisifiedMethod
        {
            get { return _idsValidationIsSatisifiedMethod; }
        }

        public static MethodInfo IdsSatisifiesConstraintMethod
        {
            get { return _idsSatisfiesConstraintMethod; }
        }
        //
        public static MethodInfo EnumerableIfcObjectsWithProperties
        {
            get { return _enumerableIfcObjectsWithPropertiesMethod; }
        }

        public static MethodInfo EnumerableIfcMaterialSelector
        {
            get { return _enumerableIfcMaterialSelectorMethod; }
        }

        public static MethodInfo EnumerableIfcClassificationSelector
        {
            get { return _enumerableIfcAssociatesClassificationMethod; }
        }

        public static MethodInfo EnumerableIfcPartofRelatedMethod
        {
            get { return _enumerableIfcPartofRelatedMethod; }
        }

        

        public static MethodInfo EnumerableWhereAssociatedWithClassification
        {
            get { return _enumerableWhereAssociatedWithClassificationMethod; }
        }

        public static MethodInfo EnumerableWhereAssociatedWithMaterial
        {
            get { return _enumerableWhereAssociatedWithMaterialMethod; }
        }

        public static MethodInfo EnumerableObjectWhereAssociatedWithProperty
        {
            get { return _enumerableWhereObjAssociatedWithPropertyMethod; }
        }

        public static MethodInfo EnumerableTypeWhereAssociatedWithProperty
        {
            get { return _enumerableWhereTypeAssociatedWithPropertyMethod; }
        }

        public static MethodInfo EnumerableWhereObjectPartOfMethod
        {
            get { return _enumerableWhereObjectPartOfMethod; }
        }

        //_enumerableWhereObjectPartOfMethod

        public static MethodInfo EnumerableCastGeneric
        {
            get { return _enumerableCastMethod; }
        }

        public static MethodInfo EnumerableOfTypeGeneric
        {
            get { return _enumerableOfTypeMethod; }
        }

        public static MethodInfo EnumerableSelectGeneric
        {
            get { return _enumerableSelectMethod; }
        }

        public static MethodInfo EnumerableSelectManyGeneric
        {
            get { return _enumerableSelectManyMethod; }
        }

        public static MethodInfo EnumerableConcatGeneric
        {
            get { return _enumerableConcatMethod; }
        }

        public static MethodInfo EnumerableFirstOrDefault
        {
            get { return _enumerableFirstOrDefaultMethod; }
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
