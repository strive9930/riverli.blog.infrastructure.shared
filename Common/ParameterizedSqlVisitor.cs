using System.Linq.Expressions;
using System.Text;
using Dapper;

namespace RiverLi.Blog.Infrastructure.Shared.Repositories;

/// <summary>
/// 一个简单的 Expression Visitor，用于将 Expression<Func<T, bool>> 转换为参数化的 SQL 片段和参数
/// 注意：这是一个高度简化的示例，不支持所有可能的表达式类型
/// </summary>
internal class ParameterizedSqlVisitor : ExpressionVisitor
{
    private readonly DynamicParameters _parameters;
    private readonly StringBuilder _sql = new();
    private int _parameterCounter = 0;

    public ParameterizedSqlVisitor(DynamicParameters parameters)
    {
        _parameters = parameters;
    }

    public string Visit(Expression expression)
    {
        _sql.Clear();
        Visit(expression);
        return _sql.ToString();
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        _sql.Append("(");
        Visit(node.Left);

        switch (node.NodeType)
        {
            case ExpressionType.AndAlso: // Logical AND
            case ExpressionType.And: // Bitwise AND (often used for logical)
                _sql.Append(" AND ");
                break;
            case ExpressionType.OrElse: // Logical OR
            case ExpressionType.Or: // Bitwise OR (often used for logical)
                _sql.Append(" OR ");
                break;
            case ExpressionType.Equal:
                if (IsNullConstant(node.Right))
                {
                    _sql.Append(" IS NULL");
                }
                else
                {
                    _sql.Append(" = ");
                    VisitConstant(node.Right);
                }
                break;
            case ExpressionType.NotEqual:
                 if (IsNullConstant(node.Right))
                {
                    _sql.Append(" IS NOT NULL");
                }
                else
                {
                    _sql.Append(" <> ");
                    VisitConstant(node.Right);
                }
                break;
            case ExpressionType.GreaterThan:
                _sql.Append(" > ");
                VisitConstant(node.Right);
                break;
            case ExpressionType.GreaterThanOrEqual:
                _sql.Append(" >= ");
                VisitConstant(node.Right);
                break;
            case ExpressionType.LessThan:
                _sql.Append(" < ");
                VisitConstant(node.Right);
                break;
            case ExpressionType.LessThanOrEqual:
                _sql.Append(" <= ");
                VisitConstant(node.Right);
                break;
            default:
                throw new NotSupportedException($"不支持的操作符: {node.NodeType}");
        }

        Visit(node.Right);
        _sql.Append(")");
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        // 处理实体属性访问，例如 x.Name
        if (node.Expression?.NodeType == ExpressionType.Parameter)
        {
            _sql.Append(node.Member.Name);
            return node;
        }
        // 如果不是直接的属性访问（例如 x.Prop.NestedProp），需要更复杂的处理
        // 这里简单抛出异常
        throw new NotSupportedException($"不支持的成员表达式: {node}");
    }

    private void VisitConstant(Expression expression)
    {
        if (expression is ConstantExpression constantExpression)
        {
            var paramName = $"p{_parameterCounter++}";
            _parameters.Add(paramName, constantExpression.Value);
            _sql.Append($"@{paramName}");
        }
        else
        {
            // Handle other constant types if needed, e.g., member access on constants
            throw new NotSupportedException($"不支持的常量表达式类型: {expression.GetType()}");
        }
    }

    private static bool IsNullConstant(Expression expression)
    {
        if (expression is ConstantExpression constantExpr)
        {
            return constantExpr.Value == null;
        }
        return false;
    }
}