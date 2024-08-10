namespace mcc2.AST;

public class FunctionCallExpression : Expression
{
    public string Identifier;
    public List<Expression> Arguments;

    public FunctionCallExpression(string identifier, List<Expression> arguments)
    {
        this.Identifier = identifier;
        this.Arguments = arguments;
    }
}