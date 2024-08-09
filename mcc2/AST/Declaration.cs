namespace mcc2.AST;

public class Declaration : BlockItem
{
    public string Identifier;
    public Expression? Initializer;

    public Declaration(string identifier, Expression? init)
    {
        this.Identifier = identifier;
        this.Initializer = init;
    }
}