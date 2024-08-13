namespace mcc2.AST;

public abstract record Statement : BlockItem
{
    public record ReturnStatement(Expression Expression) : Statement;
    public record ExpressionStatement(Expression Expression) : Statement;
    public record IfStatement(Expression Condition, Statement Then, Statement? Else) : Statement;
    public record CompoundStatement(Block Block) : Statement;
    public record BreakStatement(string? Label) : Statement;
    public record ContinueStatement(string? Label) : Statement;
    public record WhileStatement(Expression Condition, Statement Body, string? Label) : Statement;
    public record DoWhileStatement(Statement Body, Expression Condition, string? Label) : Statement;
    public record ForStatement(ForInit Init, Expression? Condition, Expression? Post, Statement Body, string? Label) : Statement;
    public record NullStatement() : Statement;

    private Statement() { }
}