namespace mcc2.AST;

public class VariableDeclaration : Declaration
{
    public string Identifier;
    public Expression? Initializer;

    public VariableDeclaration(string identifier, Expression? init)
    {
        this.Identifier = identifier;
        this.Initializer = init;
    }
}