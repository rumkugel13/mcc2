namespace mcc2.AST;

public class DoWhileStatement : Statement
{
    public Statement Body;
    public Expression Condition;
    public string? Label;

    public DoWhileStatement(Statement body, Expression condition)
    {
        this.Body = body;
        this.Condition = condition;
    }
}