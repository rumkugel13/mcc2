namespace mcc2.AST;

public class InitExpression : ForInit
{
    public Expression? Expression;

    public InitExpression(Expression? expression)
    {
        this.Expression = expression;
    }
}