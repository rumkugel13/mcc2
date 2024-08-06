using System.Diagnostics;

namespace mcc2.AST;

public class FunctionDefinition
{
    public Lexer.Token Identifier;
    public Statement Body;

    public FunctionDefinition(Lexer.Token identifier, Statement body)
    {
        Debug.Assert(identifier.Type == Lexer.TokenType.Identifier);
        this.Identifier = identifier;
        this.Body = body;
    }
}