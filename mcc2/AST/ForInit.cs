namespace mcc2.AST;

public abstract record ForInit
{
    public record InitDeclaration(Declaration.VariableDeclaration Declaration) : ForInit;
    public record InitExpression(Expression? Expression) : ForInit;

    private ForInit() { }
}