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
        List<Declaration> declarations = [];
        while (tokenPos < tokens.Count &&
            (Peek(tokens).Type == Lexer.TokenType.IntKeyword ||
            Peek(tokens).Type == Lexer.TokenType.LongKeyword ||
            Peek(tokens).Type == Lexer.TokenType.StaticKeyword ||
            Peek(tokens).Type == Lexer.TokenType.ExternKeyword))
        {
            var fun = ParseDeclaration(tokens);
            declarations.Add(fun);
        }
        return new ASTProgram(declarations);
    }

    private Declaration.FunctionDeclaration ParseFunctionDeclaration(List<Token> tokens, Type type, Declaration.StorageClasses? storageClass)
    {
        var id = Expect(Lexer.TokenType.Identifier, tokens);
        Expect(Lexer.TokenType.OpenParenthesis, tokens);

        var returnType = type;
        List<string> parameters = [];
        List<Type> parameterTypes = [];

        var nextToken = Peek(tokens);
        if (nextToken.Type == Lexer.TokenType.VoidKeyword)
        {
            TakeToken(tokens);
        }
        else
        {
            ParseParameterList(tokens, parameters, parameterTypes);
        }

        Expect(Lexer.TokenType.CloseParenthesis, tokens);

        Block? body = null;
        if (Peek(tokens).Type == Lexer.TokenType.OpenBrace)
            body = ParseBlock(tokens);
        else
            Expect(Lexer.TokenType.Semicolon, tokens);
        return new Declaration.FunctionDeclaration(GetIdentifier(id, this.source), parameters, body, new Type.FunctionType(parameterTypes , type), storageClass);
    }

    private void ParseParameterList(List<Token> tokens, List<string> parameterNames, List<Type> parameterTypes)
    {
        var type = ParseType(ParseTypeSpecifiers(tokens));
        var paramId = Expect(Lexer.TokenType.Identifier, tokens);
        parameterNames.Add(GetIdentifier(paramId, source));
        parameterTypes.Add(type);

        while (Peek(tokens).Type == Lexer.TokenType.Comma)
        {
            TakeToken(tokens);
            type = ParseType(ParseTypeSpecifiers(tokens));
            paramId = Expect(Lexer.TokenType.Identifier, tokens);
            parameterNames.Add(GetIdentifier(paramId, source));
            parameterTypes.Add(type);
        }
    }

    private List<Token> ParseSpecifiers(List<Token> tokens)
    {
        List<Token> specifiers = [];
        var nextToken = Peek(tokens);
        while (nextToken.Type == Lexer.TokenType.IntKeyword ||
            nextToken.Type == Lexer.TokenType.LongKeyword ||
            nextToken.Type == Lexer.TokenType.StaticKeyword ||
            nextToken.Type == Lexer.TokenType.ExternKeyword)
        {
            specifiers.Add(TakeToken(tokens));
            nextToken = Peek(tokens);
        }

        return specifiers;
    }

    private List<Token> ParseTypeSpecifiers(List<Token> tokens)
    {
        List<Token> specifiers = [];
        var nextToken = Peek(tokens);
        while (nextToken.Type == Lexer.TokenType.IntKeyword ||
            nextToken.Type == Lexer.TokenType.LongKeyword)
        {
            specifiers.Add(TakeToken(tokens));
            nextToken = Peek(tokens);
        }

        return specifiers;
    }

    private Type ParseType(List<Token> types)
    {
        if (types.Count == 1 && types[0].Type == Lexer.TokenType.IntKeyword)
            return new Type.Int();
        if (types.Count == 1 && types[0].Type == Lexer.TokenType.LongKeyword ||
            types.Count == 2 && types[0].Type == Lexer.TokenType.LongKeyword && types[1].Type == Lexer.TokenType.IntKeyword ||
            types.Count == 2 && types[0].Type == Lexer.TokenType.IntKeyword && types[1].Type == Lexer.TokenType.LongKeyword)
            return new Type.Long();

        throw new Exception($"Parsing Error: Invalid type specifier");
    }

    private void ParseTypeAndStorageClass(List<Token> specifiers, out Type type, out Declaration.StorageClasses? storageClass)
    {
        List<Token> types = [];
        List<Token> storageClasses = [];
        foreach (var specifier in specifiers)
        {
            if (specifier.Type == Lexer.TokenType.IntKeyword ||
                specifier.Type == Lexer.TokenType.LongKeyword)
                types.Add(specifier);
            else
                storageClasses.Add(specifier);
        }

        type = ParseType(types);

        if (storageClasses.Count > 1)
            throw new Exception($"Parsing Error: Invalid storage class count");
        if (storageClasses.Count == 1)
            storageClass = ParseStorageClass(storageClasses[0]);
        else
            storageClass = null;

        return;
    }

    private Declaration.StorageClasses ParseStorageClass(Token storageClass)
    {
        return storageClass.Type switch
        {
            Lexer.TokenType.ExternKeyword => Declaration.StorageClasses.Extern,
            Lexer.TokenType.StaticKeyword => Declaration.StorageClasses.Static,
            _ => throw new Exception($"Parsing Error: Invalid storage class")
        };
    }

    private Declaration ParseDeclaration(List<Token> tokens)
    {
        var specifiers = ParseSpecifiers(tokens);
        ParseTypeAndStorageClass(specifiers, out Type type, out Declaration.StorageClasses? storageClass);

        // 0 ahead = last specifier, 1 ahead is openParen or not
        if (PeekAhead(tokens, 1).Type == Lexer.TokenType.OpenParenthesis)
        {
            return ParseFunctionDeclaration(tokens, type, storageClass);
        }
        else
        {
            return ParseVariableDeclaration(tokens, type, storageClass);
        }
    }

    private Block ParseBlock(List<Token> tokens)
    {
        Expect(Lexer.TokenType.OpenBrace, tokens);
        List<BlockItem> body = [];
        while (Peek(tokens).Type != Lexer.TokenType.CloseBrace)
        {
            body.Add(ParseBlockItem(tokens));
        }
        TakeToken(tokens);
        return new Block(body);
    }

    private BlockItem ParseBlockItem(List<Token> tokens)
    {
        var nextToken = Peek(tokens);
        if (nextToken.Type == Lexer.TokenType.IntKeyword ||
            nextToken.Type == Lexer.TokenType.LongKeyword ||
            nextToken.Type == Lexer.TokenType.StaticKeyword ||
            nextToken.Type == Lexer.TokenType.ExternKeyword)
        {
            return ParseDeclaration(tokens);
        }
        else
        {
            return ParseStatement(tokens);
        }
    }

    private Declaration.VariableDeclaration ParseVariableDeclaration(List<Token> tokens, Type type, Declaration.StorageClasses? storageClass)
    {
        var id = Expect(Lexer.TokenType.Identifier, tokens);
        Expression? expression = null;
        var nextToken = Peek(tokens);

        if (nextToken.Type == Lexer.TokenType.Equals)
        {
            TakeToken(tokens);
            expression = ParseExpression(tokens);
        }

        Expect(Lexer.TokenType.Semicolon, tokens);
        return new Declaration.VariableDeclaration(GetIdentifier(id, this.source), expression, type, storageClass);
    }

    private Statement ParseStatement(List<Token> tokens)
    {
        var nextToken = Peek(tokens);

        switch (nextToken.Type)
        {
            case Lexer.TokenType.ReturnKeyword:
                {
                    TakeToken(tokens);
                    var exp = ParseExpression(tokens);
                    Expect(Lexer.TokenType.Semicolon, tokens);
                    return new Statement.ReturnStatement(exp);
                }

            case Lexer.TokenType.Semicolon:
                TakeToken(tokens);
                return new Statement.NullStatement();
            case Lexer.TokenType.IfKeyword:
                {
                    TakeToken(tokens);
                    Expect(Lexer.TokenType.OpenParenthesis, tokens);
                    var cond = ParseExpression(tokens);
                    Expect(Lexer.TokenType.CloseParenthesis, tokens);
                    var thenStatement = ParseStatement(tokens);
                    Statement? elseStatement = null;

                    var maybeElse = Peek(tokens);
                    if (maybeElse.Type == Lexer.TokenType.ElseKeyword)
                    {
                        TakeToken(tokens);
                        elseStatement = ParseStatement(tokens);
                    }
                    return new Statement.IfStatement(cond, thenStatement, elseStatement);
                }

            case Lexer.TokenType.OpenBrace:
                {
                    var block = ParseBlock(tokens);
                    return new Statement.CompoundStatement(block);
                }

            case Lexer.TokenType.BreakKeyword:
                TakeToken(tokens);
                Expect(Lexer.TokenType.Semicolon, tokens);
                return new Statement.BreakStatement(null);
            case Lexer.TokenType.ContinueKeyword:
                TakeToken(tokens);
                Expect(Lexer.TokenType.Semicolon, tokens);
                return new Statement.ContinueStatement(null);
            case Lexer.TokenType.WhileKeyword:
                {
                    TakeToken(tokens);
                    Expect(Lexer.TokenType.OpenParenthesis, tokens);
                    var cond = ParseExpression(tokens);
                    Expect(Lexer.TokenType.CloseParenthesis, tokens);
                    var body = ParseStatement(tokens);
                    return new Statement.WhileStatement(cond, body, null);
                }
            case Lexer.TokenType.DoKeyword:
                {
                    TakeToken(tokens);
                    var body = ParseStatement(tokens);
                    Expect(Lexer.TokenType.WhileKeyword, tokens);
                    Expect(Lexer.TokenType.OpenParenthesis, tokens);
                    var cond = ParseExpression(tokens);
                    Expect(Lexer.TokenType.CloseParenthesis, tokens);
                    Expect(Lexer.TokenType.Semicolon, tokens);
                    return new Statement.DoWhileStatement(body, cond, null);
                }
            case Lexer.TokenType.ForKeyword:
                {
                    TakeToken(tokens);
                    Expect(Lexer.TokenType.OpenParenthesis, tokens);
                    var init = ParseForInit(tokens);
                    var cond = ParseOptionalExpression(tokens, Lexer.TokenType.Semicolon);
                    var post = ParseOptionalExpression(tokens, Lexer.TokenType.CloseParenthesis);
                    var body = ParseStatement(tokens);
                    return new Statement.ForStatement(init, cond, post, body, null);
                }

            default:
                {
                    var exp = ParseExpression(tokens);
                    Expect(Lexer.TokenType.Semicolon, tokens);
                    return new Statement.ExpressionStatement(exp);
                }
        }
    }

    private ForInit ParseForInit(List<Token> tokens)
    {
        var nextToken = Peek(tokens);

        if (nextToken.Type == Lexer.TokenType.IntKeyword ||
            nextToken.Type == Lexer.TokenType.LongKeyword ||
            nextToken.Type == Lexer.TokenType.StaticKeyword ||
            nextToken.Type == Lexer.TokenType.ExternKeyword)
        {
            var decl = (Declaration.VariableDeclaration)ParseDeclaration(tokens);
            return new ForInit.InitDeclaration(decl);
        }
        else
        {
            var exp = ParseOptionalExpression(tokens, Lexer.TokenType.Semicolon);
            return new ForInit.InitExpression(exp);
        }
    }

    private Expression? ParseOptionalExpression(List<Token> tokens, Lexer.TokenType endToken)
    {
        var nextToken = Peek(tokens);

        if (nextToken.Type != endToken)
        {
            var exp = ParseExpression(tokens);
            Expect(endToken, tokens);
            return exp;
        }

        Expect(endToken, tokens);
        return null;
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
        {Lexer.TokenType.Question, 3},
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
                left = new Expression.AssignmentExpression(left, right);
            }
            else if (nextToken.Type == Lexer.TokenType.Question)
            {
                var middle = ParseConditionalMiddle(tokens);
                var right = ParseExpression(tokens, precedence);
                left = new Expression.ConditionalExpression(left, middle, right);
            }
            else
            {
                var op = ParseBinaryOperator(nextToken, tokens);
                var right = ParseExpression(tokens, precedence + 1);
                left = new Expression.BinaryExpression(op, left, right);
            }
            nextToken = Peek(tokens);
        }
        return left;
    }

    private Expression ParseConditionalMiddle(List<Token> tokens)
    {
        TakeToken(tokens);
        var exp = ParseExpression(tokens, 0);
        Expect(Lexer.TokenType.Colon, tokens);
        return exp;
    }

    private Expression ParseFactor(List<Token> tokens)
    {
        var nextToken = Peek(tokens);

        if (nextToken.Type == Lexer.TokenType.Constant || nextToken.Type == Lexer.TokenType.LongConstant)
        {
            var constant = TakeToken(tokens);
            return new Expression.ConstantExpression(GetConstant(constant, this.source));
        }
        else if (nextToken.Type == Lexer.TokenType.Hyphen || nextToken.Type == Lexer.TokenType.Tilde || nextToken.Type == Lexer.TokenType.Exclamation)
        {
            var op = ParseUnaryOperator(nextToken, tokens);
            var innerExpression = ParseFactor(tokens);
            return new Expression.UnaryExpression(op, innerExpression);
        }
        else if (nextToken.Type == Lexer.TokenType.OpenParenthesis)
        {
            TakeToken(tokens);

            nextToken = Peek(tokens);
            if (nextToken.Type == Lexer.TokenType.IntKeyword ||
                nextToken.Type == Lexer.TokenType.LongKeyword)
            {
                var type = ParseType(ParseTypeSpecifiers(tokens));
                Expect(Lexer.TokenType.CloseParenthesis, tokens);
                var factor = ParseFactor(tokens);
                return new Expression.CastExpression(type, factor);
            }
            else
            {
                var innerExpression = ParseExpression(tokens);
                Expect(Lexer.TokenType.CloseParenthesis, tokens);
                return innerExpression;
            }
        }
        else if (nextToken.Type == Lexer.TokenType.Identifier)
        {
            var id = TakeToken(tokens);

            if (Peek(tokens).Type == Lexer.TokenType.OpenParenthesis)
            {
                TakeToken(tokens);
                List<Expression> arguments = [];

                if (Peek(tokens).Type != Lexer.TokenType.CloseParenthesis)
                {
                    arguments.Add(ParseExpression(tokens));
                    while (Peek(tokens).Type == Lexer.TokenType.Comma)
                    {
                        TakeToken(tokens);
                        arguments.Add(ParseExpression(tokens));
                    }
                }

                Expect(Lexer.TokenType.CloseParenthesis, tokens);
                return new Expression.FunctionCallExpression(GetIdentifier(id, this.source), arguments);
            }

            return new Expression.VariableExpression(GetIdentifier(id, this.source));
        }
        else
        {
            throw new Exception($"Parsing Error: Unsupported Token '{nextToken.Type}'");
        }
    }

    private Expression.BinaryOperator ParseBinaryOperator(Token current, List<Token> tokens)
    {
        TakeToken(tokens);
        return current.Type switch
        {
            Lexer.TokenType.Hyphen => Expression.BinaryOperator.Subtract,
            Lexer.TokenType.Plus => Expression.BinaryOperator.Add,
            Lexer.TokenType.Asterisk => Expression.BinaryOperator.Multiply,
            Lexer.TokenType.ForwardSlash => Expression.BinaryOperator.Divide,
            Lexer.TokenType.Percent => Expression.BinaryOperator.Remainder,
            Lexer.TokenType.DoubleAmpersand => Expression.BinaryOperator.And,
            Lexer.TokenType.DoubleVertical => Expression.BinaryOperator.Or,
            Lexer.TokenType.DoubleEquals => Expression.BinaryOperator.Equal,
            Lexer.TokenType.ExclamationEquals => Expression.BinaryOperator.NotEqual,
            Lexer.TokenType.LessThan => Expression.BinaryOperator.LessThan,
            Lexer.TokenType.LessThanEquals => Expression.BinaryOperator.LessOrEqual,
            Lexer.TokenType.GreaterThan => Expression.BinaryOperator.GreaterThan,
            Lexer.TokenType.GreaterThanEquals => Expression.BinaryOperator.GreaterOrEqual,
            _ => throw new Exception($"Parsing Error: Unknown Binary Operator: {current.Type}")
        };
    }

    private Expression.UnaryOperator ParseUnaryOperator(Token current, List<Token> tokens)
    {
        TakeToken(tokens);
        return current.Type switch
        {
            Lexer.TokenType.Hyphen => Expression.UnaryOperator.Negate,
            Lexer.TokenType.Tilde => Expression.UnaryOperator.Complement,
            Lexer.TokenType.Exclamation => Expression.UnaryOperator.Not,
            _ => throw new Exception($"Parsing Error: Unknown Unary Operator: {current.Type}")
        };
    }

    private Token Peek(List<Token> tokens)
    {
        if (tokenPos >= tokens.Count)
            throw new Exception("Parsing Error: No more Tokens");

        return tokens[tokenPos];
    }

    private Token PeekAhead(List<Token> tokens, int ahead)
    {
        if (tokenPos + ahead >= tokens.Count)
            throw new Exception("Parsing Error: No more Tokens");

        return tokens[tokenPos + ahead];
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

    private string GetIdentifier(Token token, string source)
    {
        Regex regex = new($"\\G[a-zA-Z_]\\w*\\b");
        Match match = regex.Match(source, token.Position);
        Debug.Assert(match.Success, "There should be an Identifier");
        return match.Value;
    }

    private Const GetConstant(Token token, string source)
    {
        System.Numerics.BigInteger value;
        if (token.Type == Lexer.TokenType.Constant)
        {
            Regex regex = new($"\\G[0-9]+\\b");
            Match match = regex.Match(source, token.Position);
            Debug.Assert(match.Success, "There should be an Int Constant");
            value = System.Numerics.BigInteger.Parse(match.Value);
        }
        else
        {
            Regex regex = new($"\\G[0-9]+[lL]\\b");
            Match match = regex.Match(source, token.Position);
            Debug.Assert(match.Success, "There should be a Long Constant");
            // note: this does not parse the l suffix, so we remove it
            value = System.Numerics.BigInteger.Parse(match.Value[0..^1]);
        }

        if (value > long.MaxValue)
            throw new Exception("Parsing Error: Constant is too large to represent as an int or long");

        if (token.Type == Lexer.TokenType.Constant && value <= int.MaxValue)
            return new Const.ConstInt((int)value);

        return new Const.ConstLong((long)value);
    }
}