namespace mcc2.AST;

public class VariableExpression : Expression
{
    public string Identifier;

    public VariableExpression(string identifier)
    {
        this.Identifier = identifier;
    }
}