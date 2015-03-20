namespace System.Linq.Expressions
{
    
    public static class LambdaExtensions
    {
        /// <summary>
        /// Gets the member information from the expression.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="propertyExpression">The property expression.</param>
        /// <returns><see cref="MemberExpression"/></returns>
        /// <exception cref="ArgumentNullException">propertyExpression</exception>
        /// <exception cref="ArgumentException">propertyExpression</exception>
        public static MemberExpression GetMemberInfo<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            var lambda = propertyExpression as LambdaExpression;
            if (lambda == null)
                throw new ArgumentNullException("propertyExpression");

            MemberExpression memberExpr = null;

            if (lambda.Body.NodeType == ExpressionType.Convert)
            {
                memberExpr =
                    ((UnaryExpression)lambda.Body).Operand as MemberExpression;
            }
            else if (lambda.Body.NodeType == ExpressionType.MemberAccess)
            {
                memberExpr = lambda.Body as MemberExpression;
            }

            if (memberExpr == null)
                throw new ArgumentException("propertyExpression");

            return memberExpr;
        }
    }
}