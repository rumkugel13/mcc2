namespace mcc2.AST;

public class ExpressionStatement : Statement
{
    public Expression Expression;

    public ExpressionStatement(Expression expression)
    {
        this.Expression = expression;
    }
}