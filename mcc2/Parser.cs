namespace mcc2;

using System.Diagnostics;
using System.Text.RegularExpressions;
using mcc2.AST;
using Token = Lexer.Token;

public class Parser
{
    private string source;
    private int tokenPos;

    public Parser(string source)
    {
        this.source = source;
    }

    public ASTProgram Parse(List<Token> tokens)
    {
        tokenPos = 0;
        var program = ParseProgram(tokens);
        if (tokenPos < tokens.Count)
            throw new Exception("Parsing Error: Too many Tokens");
        return program;
    }

    private ASTProgram ParseProgram(List<Token> tokens)
    {
        var fun = ParseFunctionDefinition(tokens);
        return new ASTProgram(fun);
    }

    private FunctionDefinition ParseFunctionDefinition(List<Token> tokens)
    {
        Expect(Lexer.TokenType.IntKeyword, tokens);
        var id = Expect(Lexer.TokenType.Identifier, tokens);
        Expect(Lexer.TokenType.OpenParenthesis, tokens);
        Expect(Lexer.TokenType.VoidKeyword, tokens);
        Expect(Lexer.TokenType.CloseParenthesis, tokens);
        Expect(Lexer.TokenType.OpenBrace, tokens);

        List<BlockItem> body = [];
        while (Peek(tokens).Type != Lexer.TokenType.CloseBrace)
        {
            body.Add(ParseBlockItem(tokens));
        }

        TakeToken(tokens);
        return new FunctionDefinition(GetIdentifier(id, this.source), body);
    }

    private BlockItem ParseBlockItem(List<Token> tokens)
    {
        var nextToken = Peek(tokens);
        if (nextToken.Type == Lexer.TokenType.IntKeyword)
        {
            return ParseDeclaration(tokens);
        }
        else
        {
            return ParseStatement(tokens);
        }
    }

    private Declaration ParseDeclaration(List<Token> tokens)
    {
        Expect(Lexer.TokenType.IntKeyword, tokens);
        var id = Expect(Lexer.TokenType.Identifier, tokens);
        Expression? expression = null;
        var nextToken = Peek(tokens);

        if (nextToken.Type == Lexer.TokenType.Equals)
        {
            TakeToken(tokens);
            expression = ParseExpression(tokens);
        }

        Expect(Lexer.TokenType.Semicolon, tokens);
        return new Declaration(GetIdentifier(id, this.source), expression);
    }

    private Statement ParseStatement(List<Token> tokens)
    {
        var nextToken = Peek(tokens);

        if (nextToken.Type == Lexer.TokenType.ReturnKeyword)
        {
            TakeToken(tokens);
            var exp = ParseExpression(tokens);
            Expect(Lexer.TokenType.Semicolon, tokens);
            return new ReturnStatement(exp);
        }
        else if (nextToken.Type == Lexer.TokenType.Semicolon)
        {
            TakeToken(tokens);
            return new NullStatement();
        }
        else
        {
            var exp = ParseExpression(tokens);
            Expect(Lexer.TokenType.Semicolon, tokens);
            return new ExpressionStatement(exp);
        }
    }

    private readonly Dictionary<Lexer.TokenType, int> PrecedenceLevels = new(){
        {Lexer.TokenType.Asterisk, 50},
        {Lexer.TokenType.ForwardSlash, 50},
        {Lexer.TokenType.Percent, 50},
        {Lexer.TokenType.Plus, 45},
        {Lexer.TokenType.Hyphen, 45},
        {Lexer.TokenType.LessThan, 35},
        {Lexer.TokenType.LessThanEquals, 35},
        {Lexer.TokenType.GreaterThan, 35},
        {Lexer.TokenType.GreaterThanEquals, 35},
        {Lexer.TokenType.DoubleEquals, 30},
        {Lexer.TokenType.ExclamationEquals, 30},
        {Lexer.TokenType.DoubleAmpersand, 10},
        {Lexer.TokenType.DoubleVertical, 5},
        {Lexer.TokenType.Equals, 1},
    };

    private Expression ParseExpression(List<Token> tokens, int minPrecedence = 0)
    {
        var left = ParseFactor(tokens);
        var nextToken = Peek(tokens);
        while (PrecedenceLevels.TryGetValue(nextToken.Type, out int precedence) && precedence >= minPrecedence)
        {
            if (nextToken.Type == Lexer.TokenType.Equals)
            {
                TakeToken(tokens);
                var right = ParseExpression(tokens, precedence);
                left = new AssignmentExpression(left, right);
            }
            else
            {
                var op = ParseBinaryOperator(nextToken, tokens);
                var right = ParseExpression(tokens, precedence + 1);
                left = new BinaryExpression(op, left, right);
            }
            nextToken = Peek(tokens);
        }
        return left;
    }

    private Expression ParseFactor(List<Token> tokens)
    {
        var nextToken = Peek(tokens);

        if (nextToken.Type == Lexer.TokenType.Constant)
        {
            var constant = TakeToken(tokens);
            return new ConstantExpression(GetConstant(constant, this.source));
        }
        else if (nextToken.Type == Lexer.TokenType.Hyphen || nextToken.Type == Lexer.TokenType.Tilde || nextToken.Type == Lexer.TokenType.Exclamation)
        {
            var op = ParseUnaryOperator(nextToken, tokens);
            var innerExpression = ParseFactor(tokens);
            return new UnaryExpression(op, innerExpression);
        }
        else if (nextToken.Type == Lexer.TokenType.OpenParenthesis)
        {
            TakeToken(tokens);
            var innerExpression = ParseExpression(tokens);
            Expect(Lexer.TokenType.CloseParenthesis, tokens);
            return innerExpression;
        }
        else if (nextToken.Type == Lexer.TokenType.Identifier)
        {
            var id = TakeToken(tokens);
            return new VariableExpression(GetIdentifier(id, this.source));
        }
        else
        {
            throw new Exception($"Parsing Error: Unsupported Token '{nextToken.Type}'");
        }
    }

    private BinaryExpression.BinaryOperator ParseBinaryOperator(Token current, List<Token> tokens)
    {
        TakeToken(tokens);
        return current.Type switch
        {
            Lexer.TokenType.Hyphen => BinaryExpression.BinaryOperator.Subtract,
            Lexer.TokenType.Plus => BinaryExpression.BinaryOperator.Add,
            Lexer.TokenType.Asterisk => BinaryExpression.BinaryOperator.Multiply,
            Lexer.TokenType.ForwardSlash => BinaryExpression.BinaryOperator.Divide,
            Lexer.TokenType.Percent => BinaryExpression.BinaryOperator.Remainder,
            Lexer.TokenType.DoubleAmpersand => BinaryExpression.BinaryOperator.And,
            Lexer.TokenType.DoubleVertical => BinaryExpression.BinaryOperator.Or,
            Lexer.TokenType.DoubleEquals => BinaryExpression.BinaryOperator.Equal,
            Lexer.TokenType.ExclamationEquals => BinaryExpression.BinaryOperator.NotEqual,
            Lexer.TokenType.LessThan => BinaryExpression.BinaryOperator.LessThan,
            Lexer.TokenType.LessThanEquals => BinaryExpression.BinaryOperator.LessOrEqual,
            Lexer.TokenType.GreaterThan => BinaryExpression.BinaryOperator.GreaterThan,
            Lexer.TokenType.GreaterThanEquals => BinaryExpression.BinaryOperator.GreaterOrEqual,
            _ => throw new Exception($"Parsing Error: Unknown Binary Operator: {current.Type}")
        };
    }

    private UnaryExpression.UnaryOperator ParseUnaryOperator(Token current, List<Token> tokens)
    {
        TakeToken(tokens);
        return current.Type switch
        {
            Lexer.TokenType.Hyphen => UnaryExpression.UnaryOperator.Negate,
            Lexer.TokenType.Tilde => UnaryExpression.UnaryOperator.Complement,
            Lexer.TokenType.Exclamation => UnaryExpression.UnaryOperator.Not,
            _ => throw new Exception($"Parsing Error: Unknown Unary Operator: {current.Type}")
        };
    }

    private Token Peek(List<Token> tokens)
    {
        if (tokenPos >= tokens.Count)
            throw new Exception("Parsing Error: No more Tokens");

        return tokens[tokenPos];
    }

    private Token Expect(Lexer.TokenType tokenType, List<Token> tokens)
    {
        Token actual = TakeToken(tokens);
        if (actual.Type != tokenType)
            throw new Exception($"Parsing Error: Expected {tokenType}, got {actual.Type}");

        return actual;
    }

    private Token TakeToken(List<Token> tokens)
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