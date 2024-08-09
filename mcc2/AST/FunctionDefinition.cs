namespace mcc2.AST;

public class FunctionDefinition
{
    public string Name;
    public Block Body;

    public FunctionDefinition(string name, Block body)
    {
        this.Name = name;
        this.Body = body;
    }
}