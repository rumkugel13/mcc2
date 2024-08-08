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

    private readonly Dictionary<Lexer.TokenType, int> PrecedenceLevels = new(){
        {Lexer.TokenType.Asterisk, 50},
        {Lexer.TokenType.ForwardSlash, 50},
        {Lexer.TokenType.Percent, 50},
        {Lexer.TokenType.Plus, 45},
        {Lexer.TokenType.Hyphen, 45},
    };

    private Expression ParseExpression(List<Token> tokens, ref int tokenPos, int minPrecedence = 0)
    {
        var left = ParseFactor(tokens, ref tokenPos);
        var nextToken = Peek(tokens, ref tokenPos);
        while (PrecedenceLevels.TryGetValue(nextToken.Type, out int precedence) && precedence >= minPrecedence)
        {
            var op = ParseBinaryOperator(nextToken, tokens, ref tokenPos);
            var right = ParseExpression(tokens, ref tokenPos, precedence + 1);
            left = new BinaryExpression(op, left, right);
            nextToken = Peek(tokens, ref tokenPos);
        }
        return left;
    }

    private Expression ParseFactor(List<Token> tokens, ref int tokenPos)
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
            var innerExpression = ParseFactor(tokens, ref tokenPos);
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

    private BinaryExpression.BinaryOperator ParseBinaryOperator(Token current, List<Token> tokens, ref int tokenPos)
    {
        TakeToken(tokens, ref tokenPos);
        return current.Type switch
        {
            Lexer.TokenType.Hyphen => BinaryExpression.BinaryOperator.Subtract,
            Lexer.TokenType.Plus => BinaryExpression.BinaryOperator.Add,
            Lexer.TokenType.Asterisk => BinaryExpression.BinaryOperator.Multiply,
            Lexer.TokenType.ForwardSlash => BinaryExpression.BinaryOperator.Divide,
            Lexer.TokenType.Percent => BinaryExpression.BinaryOperator.Remainder,
            _ => throw new Exception($"Parsing Error: Unknown Binary Operator: {current.Type}")
        };
    }

    private UnaryExpression.UnaryOperator ParseUnaryOperator(Token current, List<Token> tokens, ref int tokenPos)
    {
        TakeToken(tokens, ref tokenPos);
        return current.Type switch
        {
            Lexer.TokenType.Hyphen => UnaryExpression.UnaryOperator.Negate,
            Lexer.TokenType.Tilde => UnaryExpression.UnaryOperator.Complement,
            _ => throw new Exception($"Parsing Error: Unknown Unary Operator: {current.Type}")
        };
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