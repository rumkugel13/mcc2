namespace mcc2.AST;

public class FunctionDefinition
{
    public string Name;
    public List<BlockItem> Body;

    public FunctionDefinition(string name, List<BlockItem> body)
    {
        this.Name = name;
        this.Body = body;
    }
}