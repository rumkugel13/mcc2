namespace mcc2.AST;

public class ReturnStatement : Statement
{
    public Expression Expression;

    public ReturnStatement(Expression expression)
    {
        this.Expression = expression;
    }
}