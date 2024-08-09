namespace mcc2.AST;

public class WhileStatement : Statement
{
    public Expression Condition;
    public Statement Body;
    public string? Label;

    public WhileStatement(Expression condition, Statement body)
    {
        this.Condition = condition;
        this.Body = body;
    }
}