namespace mcc2;

using mcc2.AST;
using Token = Lexer.Token;

public class Parser
{
    public ASTProgram Parse(List<Token> tokens)
    {
        int tokenPos = 0;
        var program = ParseProgram(tokens, ref tokenPos);
        if (tokenPos < tokens.Count)
            throw new Exception("Parsing Error: Too many Tokens");
        return program;
    }

    private ASTProgram ParseProgram(List<Token> tokens, ref int tokenPos)
    {
        var fun = ParseFunctionDefinition(tokens, ref tokenPos);
        return new ASTProgram(fun);
    }

    private FunctionDefinition ParseFunctionDefinition(List<Token> tokens, ref int tokenPos)
    {
        Expect(Lexer.TokenType.IntKeyword, tokens, ref tokenPos);
        var id = Expect(Lexer.TokenType.Identifier, tokens, ref tokenPos);
        Expect(Lexer.TokenType.OpenParenthesis, tokens, ref tokenPos);
        Expect(Lexer.TokenType.VoidKeyword, tokens, ref tokenPos);
        Expect(Lexer.TokenType.CloseParenthesis, tokens, ref tokenPos);
        Expect(Lexer.TokenType.OpenBrace, tokens, ref tokenPos);
        var statement = ParseStatement(tokens, ref tokenPos);
        Expect(Lexer.TokenType.CloseBrace, tokens, ref tokenPos);
        return new FunctionDefinition(id, statement);
    }

    private Statement ParseStatement(List<Token> tokens, ref int tokenPos)
    {
        Expect(Lexer.TokenType.ReturnKeyword, tokens, ref tokenPos);
        var exp = ParseExpression(tokens, ref tokenPos);
        Expect(Lexer.TokenType.Semicolon, tokens, ref tokenPos);
        return new ReturnStatement(exp);
    }

    private Expression ParseExpression(List<Token> tokens, ref int tokenPos)
    {
        var constant = Expect(Lexer.TokenType.Constant, tokens, ref tokenPos);
        return new ConstantExpression(constant);
    }

    private Token Expect(Lexer.TokenType tokenType, List<Token> tokens, ref int tokenPos)
    {
        if (tokenPos >= tokens.Count)
            throw new Exception("Parsing Error: No more Tokens");

        Token actual = tokens[tokenPos++];
        if (actual.Type != tokenType)
            throw new Exception($"Parsing Error: Expected {tokenType}, got {actual.Type}");

        return actual;
    }
}