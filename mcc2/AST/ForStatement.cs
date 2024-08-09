namespace mcc2.AST;

public class ForStatement : Statement
{
    public ForInit Init;
    public Expression? Condition;
    public Expression? Post;
    public Statement Body;
    public string? Label;

    public ForStatement(ForInit init, Expression? condition, Expression? post, Statement body)
    {
        this.Init = init;
        this.Condition = condition;
        this.Post = post;
        this.Body = body;
    }
}