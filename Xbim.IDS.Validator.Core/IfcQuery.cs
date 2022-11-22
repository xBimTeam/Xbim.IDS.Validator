using System.Linq.Expressions;
using Xbim.Common;

namespace Xbim.IDS.Validator.Core
{
    public class IfcQuery
    {
        ParameterExpression modelParameter;

        public IfcQuery()
        {
            modelParameter = Expression.Parameter(typeof(IModel), "model");

            // Build model.Instances Expression
            InstancesExpression = (Expression)Expression.Property(modelParameter, nameof(IModel.Instances)) ?? Expression.Empty();

        }

        public Expression InstancesExpression { get; private set; }


        /// <summary>
        /// Query the model based on the current expression
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public IEnumerable<IPersistEntity> Execute(Expression expression, IModel model)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var lambda = Expression.Lambda<Func<IModel, IEnumerable<IPersistEntity>>>(
            expression,
            new ParameterExpression[] { modelParameter }
            ).Compile();

            return lambda(model);
        }
    }
}
