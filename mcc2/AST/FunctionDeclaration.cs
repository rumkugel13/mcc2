namespace mcc2.AST;

public class FunctionDeclaration : Declaration
{
    public string Identifier;
    public List<string> Parameters;
    public Block? Body;

    public FunctionDeclaration(string identifier, List<string> parameters, Block? body)
    {
        this.Identifier = identifier;
        this.Parameters = parameters;
        this.Body = body;
    }
}