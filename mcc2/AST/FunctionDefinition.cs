namespace mcc2.AST;

public class FunctionDefinition
{
    public string Name;
    public Statement Body;

    public FunctionDefinition(string name, Statement body)
    {
        this.Name = name;
        this.Body = body;
    }
}