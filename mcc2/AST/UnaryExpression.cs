namespace mcc2.AST;

public class UnaryExpression : Expression
{
    public enum UnaryOperator
    {
        Complement,
        Negate,
    }

    public UnaryOperator Operator;
    public Expression Expression;

    public UnaryExpression(UnaryOperator unaryOperator, Expression expression)
    {
        this.Operator = unaryOperator;
        this.Expression = expression;
    }
}