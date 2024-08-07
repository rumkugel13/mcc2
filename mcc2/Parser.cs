namespace mcc2;

using System.Diagnostics;
using System.Text.RegularExpressions;
using mcc2.AST;
using Token = Lexer.Token;

public class Parser
{
    private string source;

    public Parser(string source)
    {
        this.source = source;
    }

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
        return new FunctionDefinition(GetIdentifier(id, this.source), statement);
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
        var nextToken = Peek(tokens, ref tokenPos);

        if (nextToken.Type == Lexer.TokenType.Constant)
        {
            var constant = TakeToken(tokens, ref tokenPos);
            return new ConstantExpression(GetConstant(constant, this.source));
        }
        else if (nextToken.Type == Lexer.TokenType.Hyphen || nextToken.Type == Lexer.TokenType.Tilde)
        {
            var op = ParseUnaryOperator(nextToken, tokens, ref tokenPos);
            var innerExpression = ParseExpression(tokens, ref tokenPos);
            return new UnaryExpression(op, innerExpression);
        }
        else if (nextToken.Type == Lexer.TokenType.OpenParenthesis)
        {
            TakeToken(tokens, ref tokenPos);
            var innerExpression = ParseExpression(tokens, ref tokenPos);
            Expect(Lexer.TokenType.CloseParenthesis, tokens, ref tokenPos);
            return innerExpression;
        }
        else
        {
            throw new Exception($"Parsing Error: Unsupported Token '{nextToken.Type}'");
        }
    }

    private UnaryExpression.UnaryOperator ParseUnaryOperator(Token current, List<Token> tokens, ref int tokenPos)
    {
        TakeToken(tokens, ref tokenPos);
        if (current.Type == Lexer.TokenType.Hyphen)
        {
            return UnaryExpression.UnaryOperator.Negate;
        }
        else if (current.Type == Lexer.TokenType.Tilde)
        {
            return UnaryExpression.UnaryOperator.Complement;
        }
        else
        {
            throw new Exception($"Parsing Error: Unknown Unary Operatpr: {current.Type}");
        }
    }

    private Token Peek(List<Token> tokens, ref int tokenPos)
    {
        if (tokenPos >= tokens.Count)
            throw new Exception("Parsing Error: No more Tokens");

        return tokens[tokenPos];
    }

    private Token Expect(Lexer.TokenType tokenType, List<Token> tokens, ref int tokenPos)
    {
        Token actual = TakeToken(tokens, ref tokenPos);
        if (actual.Type != tokenType)
            throw new Exception($"Parsing Error: Expected {tokenType}, got {actual.Type}");

        return actual;
    }

    private Token TakeToken(List<Token> tokens, ref int tokenPos)
    {
        if (tokenPos >= tokens.Count)
            throw new Exception("Parsing Error: No more Tokens");

        return tokens[tokenPos++];
    }

    private string GetIdentifier(Lexer.Token token, string source)
    {
        Regex regex = new($"\\G[a-zA-Z_]\\w*\\b");
        Match match = regex.Match(source, token.Position);
        Debug.Assert(match.Success, "There should be an Identifier");
        return match.Value;
    }

    private int GetConstant(Lexer.Token token, string source)
    {
        Regex regex = new($"\\G[0-9]+\\b");
        Match match = regex.Match(source, token.Position);
        Debug.Assert(match.Success, "There should be a Constant");
        return int.Parse(match.Value);
    }
}