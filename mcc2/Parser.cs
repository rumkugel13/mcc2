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
        while (tokenPos < tokens.Count && IsSpecifier(Peek(tokens)))
        {
            var fun = ParseDeclaration(tokens);
            declarations.Add(fun);
        }
        return new ASTProgram(declarations);
    }

    private void ParseParameterList(List<Token> tokens, List<Declarator> parameterDeclarations, List<Type> parameterTypes)
    {
        Expect(Lexer.TokenType.OpenParenthesis, tokens);

        var nextToken = Peek(tokens);
        if (nextToken.Type == Lexer.TokenType.VoidKeyword)
        {
            TakeToken(tokens);
        }
        else
        {
            var paramType = ParseType(ParseTypeSpecifiers(tokens));
            var paramDecl = ParseDeclarator(tokens);
            parameterTypes.Add(paramType);
            parameterDeclarations.Add(paramDecl);

            while (Peek(tokens).Type == Lexer.TokenType.Comma)
            {
                TakeToken(tokens);
                paramType = ParseType(ParseTypeSpecifiers(tokens));
                paramDecl = ParseDeclarator(tokens);
                parameterTypes.Add(paramType);
                parameterDeclarations.Add(paramDecl);
            }
        }
        Expect(Lexer.TokenType.CloseParenthesis, tokens);
    }

    private List<Lexer.TokenType> ParseSpecifiers(List<Token> tokens)
    {
        List<Lexer.TokenType> specifiers = [];
        var nextToken = Peek(tokens);
        while (IsSpecifier(nextToken))
        {
            specifiers.Add(TakeToken(tokens).Type);
            nextToken = Peek(tokens);
        }

        return specifiers;
    }

    private List<Lexer.TokenType> ParseTypeSpecifiers(List<Token> tokens)
    {
        List<Lexer.TokenType> specifiers = [];
        var nextToken = Peek(tokens);
        while (IsTypeSpecifier(nextToken.Type))
        {
            specifiers.Add(TakeToken(tokens).Type);
            nextToken = Peek(tokens);
        }

        return specifiers;
    }

    private Type ParseType(List<Lexer.TokenType> types)
    {
        if (types.Count == 0 ||
            (new HashSet<Lexer.TokenType>(types).Count != types.Count) ||
            (types.Contains(Lexer.TokenType.SignedKeyword) && types.Contains(Lexer.TokenType.UnsignedKeyword)))
            throw new Exception($"Parsing Error: Invalid type specifier");

        if (types.Count == 1 && types[0] == Lexer.TokenType.DoubleKeyword)
            return new Type.Double();
        if (types.Count > 1 && types.Contains(Lexer.TokenType.DoubleKeyword))
            throw new Exception($"Parsing Error: Can't combine 'double' with other type specifiers");

        if (types.Count == 1 && types[0] == Lexer.TokenType.CharKeyword)
            return new Type.Char();
        if (types.Count == 2 && types.Contains(Lexer.TokenType.CharKeyword) && types.Contains(Lexer.TokenType.SignedKeyword))
            return new Type.SChar();
        if (types.Count == 2 && types.Contains(Lexer.TokenType.CharKeyword) && types.Contains(Lexer.TokenType.UnsignedKeyword))
            return new Type.UChar();
        if (types.Contains(Lexer.TokenType.CharKeyword))
            throw new Exception("Parsing Error: Cannot combine other types with char");

        if (types.Contains(Lexer.TokenType.UnsignedKeyword) && types.Contains(Lexer.TokenType.LongKeyword))
            return new Type.ULong();
        if (types.Contains(Lexer.TokenType.LongKeyword))
            return new Type.Long();
        if (types.Contains(Lexer.TokenType.UnsignedKeyword))
            return new Type.UInt();

        return new Type.Int();
    }

    private void ParseTypeAndStorageClass(List<Lexer.TokenType> specifiers, out Type type, out Declaration.StorageClasses? storageClass)
    {
        List<Lexer.TokenType> types = [];
        List<Lexer.TokenType> storageClasses = [];
        foreach (var specifier in specifiers)
        {
            if (IsTypeSpecifier(specifier))
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

    private Declaration.StorageClasses ParseStorageClass(Lexer.TokenType storageClass)
    {
        return storageClass switch
        {
            Lexer.TokenType.ExternKeyword => Declaration.StorageClasses.Extern,
            Lexer.TokenType.StaticKeyword => Declaration.StorageClasses.Static,
            _ => throw new Exception($"Parsing Error: Invalid storage class")
        };
    }

    private Declaration ParseDeclaration(List<Token> tokens)
    {
        var specifiers = ParseSpecifiers(tokens);
        ParseTypeAndStorageClass(specifiers, out Type baseType, out Declaration.StorageClasses? storageClass);
        var declarator = ParseDeclarator(tokens);
        var (name, declType, paramNames) = ProcessDeclarator(declarator, baseType);

        if (declType is Type.FunctionType)
        {
            Block? body = null;
            if (Peek(tokens).Type == Lexer.TokenType.OpenBrace)
                body = ParseBlock(tokens);
            else
                Expect(Lexer.TokenType.Semicolon, tokens);
            return new Declaration.FunctionDeclaration(name, paramNames, body, declType, storageClass);
        }
        else
        {
            Initializer? initializer = null;
            if (Peek(tokens).Type == Lexer.TokenType.Equals)
            {
                TakeToken(tokens);
                initializer = ParseInitializer(tokens);
            }

            Expect(Lexer.TokenType.Semicolon, tokens);
            return new Declaration.VariableDeclaration(name, initializer, declType, storageClass);
        }
    }

    private Initializer ParseInitializer(List<Token> tokens)
    {
        if (Peek(tokens).Type == Lexer.TokenType.OpenBrace)
        {
            TakeToken(tokens);
            List<Initializer> initializers = [];
            initializers.Add(ParseInitializer(tokens));
            if (Peek(tokens).Type == Lexer.TokenType.Comma)
                    TakeToken(tokens);
            while (Peek(tokens).Type != Lexer.TokenType.CloseBrace)
            {
                initializers.Add(ParseInitializer(tokens));
                if (Peek(tokens).Type == Lexer.TokenType.Comma)
                    TakeToken(tokens);
            }
            Expect(Lexer.TokenType.CloseBrace, tokens);
            return new Initializer.CompoundInitializer(initializers, Type.None);
        }
        else
            return new Initializer.SingleInitializer(ParseExpression(tokens), Type.None);
    }

    private Declarator ParseDeclarator(List<Token> tokens)
    {
        if (Peek(tokens).Type == Lexer.TokenType.Asterisk)
        {
            TakeToken(tokens);
            return new Declarator.PointerDeclarator(ParseDeclarator(tokens));
        }
        else
        {
            return ParseDirectDeclarator(tokens);
        }
    }

    private Declarator ParseDirectDeclarator(List<Token> tokens)
    {
        var simple = ParseSimpleDeclarator(tokens);

        List<Declarator> parameterDeclarators = [];
        List<Type> parameterTypes = [];
        // optional declarator suffix
        if (Peek(tokens).Type == Lexer.TokenType.OpenParenthesis)
        {
            ParseParameterList(tokens, parameterDeclarators, parameterTypes);
            List<ParameterInfo> paramInfos = [];
            for (int i = 0; i < parameterDeclarators.Count; i++)
            {
                paramInfos.Add(new ParameterInfo(parameterTypes[i], parameterDeclarators[i]));
            }

            return new Declarator.FunctionDeclarator(paramInfos, simple);
        }
        else if (Peek(tokens).Type == Lexer.TokenType.OpenBracket)
        {
            Declarator result = simple;
            do
            {
                TakeToken(tokens);
                var constant = ParseConstant(tokens);
                if (constant is Const.ConstDouble)
                    throw new Exception("Parsing Error: Cannot declare array dimension with constant of type double");
                Expect(Lexer.TokenType.CloseBracket, tokens);
                result = new Declarator.ArrayDeclarator(result, GetValue(constant));
            }
            while (Peek(tokens).Type == Lexer.TokenType.OpenBracket);

            return result;
        }
        else
            return simple;
    }

    private Const ParseConstant(List<Token> tokens)
    {
        if (IsConstant(Peek(tokens)))
        {
            var constant = TakeToken(tokens);
            return GetConstant(constant, this.source);
        }
        else
            throw new Exception("Parsing Error: Expected a constant token");
    }

    private int GetValue(Const constVal)
    {
        return constVal switch
        {
            Const.ConstInt constant => (int)constant.Value,
            Const.ConstUInt constant => (int)constant.Value,
            Const.ConstLong constant => (int)constant.Value,
            Const.ConstULong constant => (int)constant.Value,
            _ => throw new NotImplementedException()
        };
    }

    private Declarator ParseSimpleDeclarator(List<Token> tokens)
    {
        if (Peek(tokens).Type == Lexer.TokenType.Identifier)
            return new Declarator.IdentifierDeclarator(GetIdentifier(TakeToken(tokens), source));
        else
        {
            Expect(Lexer.TokenType.OpenParenthesis, tokens);
            var declarator = ParseDeclarator(tokens);
            Expect(Lexer.TokenType.CloseParenthesis, tokens);
            return declarator;
        }
    }

    private (string Name, Type DerivedType, List<string> ParameterNames) ProcessDeclarator(Declarator declarator, Type baseType)
    {
        switch (declarator)
        {
            case Declarator.IdentifierDeclarator identDecl:
                return (identDecl.Identifier, baseType, []);
            case Declarator.PointerDeclarator pointerDecl:
                {
                    var derivedType = new Type.Pointer(baseType);
                    return ProcessDeclarator(pointerDecl.Declarator, derivedType);
                }
            case Declarator.FunctionDeclarator funcDecl:
                switch (funcDecl.Declarator)
                {
                    case Declarator.IdentifierDeclarator nameDecl:
                        List<string> paramNames = [];
                        List<Type> paramTypes = [];
                        foreach (var funcParam in funcDecl.Parameters)
                        {
                            var (paramName, paramType, _) = ProcessDeclarator(funcParam.Declarator, funcParam.Type);
                            if (paramType is Type.FunctionType)
                                throw new Exception("Parsing Error: Function pointers in parameters aren't supported");

                            paramNames.Add(paramName);
                            paramTypes.Add(paramType);
                        }

                        var newType = new Type.FunctionType(paramTypes, baseType);
                        return (nameDecl.Identifier, newType, paramNames);
                    default:
                        throw new Exception("Parsing Error: Can't apply additional type derivations to a function type");
                }
            case Declarator.ArrayDeclarator array:
                {
                    var derivedType = new Type.Array(baseType, array.Size);
                    return ProcessDeclarator(array.Declarator, derivedType);
                }
            default:
                throw new NotImplementedException();
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
        if (IsSpecifier(nextToken))
        {
            return ParseDeclaration(tokens);
        }
        else
        {
            return ParseStatement(tokens);
        }
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

        if (IsSpecifier(nextToken))
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
        var left = ParseUnaryExpression(tokens);
        var nextToken = Peek(tokens);
        while (PrecedenceLevels.TryGetValue(nextToken.Type, out int precedence) && precedence >= minPrecedence)
        {
            if (nextToken.Type == Lexer.TokenType.Equals)
            {
                TakeToken(tokens);
                var right = ParseExpression(tokens, precedence);
                left = new Expression.Assignment(left, right, Type.None);
            }
            else if (nextToken.Type == Lexer.TokenType.Question)
            {
                var middle = ParseConditionalMiddle(tokens);
                var right = ParseExpression(tokens, precedence);
                left = new Expression.Conditional(left, middle, right, Type.None);
            }
            else
            {
                var op = ParseBinaryOperator(nextToken, tokens);
                var right = ParseExpression(tokens, precedence + 1);
                left = new Expression.Binary(op, left, right, Type.None);
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

    private Expression ParseUnaryExpression(List<Token> tokens)
    {
        var nextToken = Peek(tokens);
        if (!IsUnaryOrPrimaryExpression(nextToken))
            throw new Exception($"Parsing Error: Unsupported Token '{nextToken.Type}'");

        if (IsUnaryOperator(nextToken))
        {
            var op = ParseUnaryOperator(nextToken, tokens);
            var innerExpression = ParseUnaryExpression(tokens);
            if (op is Expression.UnaryOperator.Dereference)
                return new Expression.Dereference(innerExpression, Type.None);
            else if (op is Expression.UnaryOperator.AddressOf)
                return new Expression.AddressOf(innerExpression, Type.None);
            return new Expression.Unary(op, innerExpression, Type.None);
        }
        else if (nextToken.Type == Lexer.TokenType.OpenParenthesis && IsTypeSpecifier(PeekAhead(tokens, 1).Type))
        {
            TakeToken(tokens);
            var type = ParseType(ParseTypeSpecifiers(tokens));
            // optional abstract declarator
            nextToken = Peek(tokens);
            if (nextToken.Type is Lexer.TokenType.Asterisk or Lexer.TokenType.OpenParenthesis or Lexer.TokenType.OpenBracket)
            {
                AbstractDeclarator abstractDeclarator = ParseAbstractDeclarator(tokens);
                var derivedType = ProcessAbstractDeclarator(abstractDeclarator, type);
                type = derivedType;
            }
            Expect(Lexer.TokenType.CloseParenthesis, tokens);
            var unaryExp = ParseUnaryExpression(tokens);
            return new Expression.Cast(type, unaryExp, Type.None);
        }
        else
            return ParsePostfixExpression(tokens);
    }

    private bool IsUnaryOrPrimaryExpression(Token token)
    {
        return IsUnaryOperator(token) || token.Type == Lexer.TokenType.OpenParenthesis ||
            IsConstant(token) || token.Type == Lexer.TokenType.Identifier || token.Type == Lexer.TokenType.StringLiteral;
    }

    private Expression ParsePostfixExpression(List<Token> tokens)
    {
        var primary = ParsePrimaryExpression(tokens);
        while (Peek(tokens).Type == Lexer.TokenType.OpenBracket)
        {
            TakeToken(tokens);
            var exp = ParseExpression(tokens);
            Expect(Lexer.TokenType.CloseBracket, tokens);
            primary = new Expression.Subscript(primary, exp, Type.None);
        }
        return primary;
    }

    private Expression ParsePrimaryExpression(List<Token> tokens)
    {
        var nextToken = Peek(tokens);

        if (IsConstant(nextToken))
        {
            var constant = TakeToken(tokens);
            return new Expression.Constant(GetConstant(constant, this.source), Type.None);
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
                return new Expression.FunctionCall(GetIdentifier(id, this.source), arguments, Type.None);
            }

            return new Expression.Variable(GetIdentifier(id, this.source), Type.None);
        }
        else if (nextToken.Type == Lexer.TokenType.StringLiteral)
        {
            return ParseStringLiteral(tokens);
        }
        else
        {
            throw new Exception($"Parsing Error: Unsupported Token '{nextToken.Type}'");
        }
    }

    private Expression.String ParseStringLiteral(List<Token> tokens)
    {
        Token token = TakeToken(tokens);
        Regex regex = new("""
            "([^"\\\n]|\\['"\\?abfnrtv])*"
            """);
        Match match = regex.Match(source, token.Position);
        string result = match.Value;
        while (Peek(tokens).Type == Lexer.TokenType.StringLiteral)
        {
            token = TakeToken(tokens);
            match = regex.Match(source, token.Position);
            result = string.Concat(result, match.Value);
        }
        return new Expression.String(result);
    }

    private AbstractDeclarator ParseAbstractDeclarator(List<Token> tokens)
    {
        var nextToken = Peek(tokens);
        if (nextToken.Type == Lexer.TokenType.Asterisk)
        {
            TakeToken(tokens);            
            if (Peek(tokens).Type is Lexer.TokenType.Asterisk or Lexer.TokenType.OpenParenthesis or Lexer.TokenType.OpenBracket)
            {
                return new AbstractDeclarator.AbstractPointer(ParseAbstractDeclarator(tokens));
            }
            return new AbstractDeclarator.AbstractPointer(new AbstractDeclarator.AbstractBase());
        }
        else if (nextToken.Type == Lexer.TokenType.OpenParenthesis || nextToken.Type == Lexer.TokenType.OpenBracket)
        {
            return ParseDirectAbstractDeclarator(tokens);
        }
        else
            return new AbstractDeclarator.AbstractBase();
    }

    private AbstractDeclarator ParseDirectAbstractDeclarator(List<Token> tokens)
    {
        var nextToken = Peek(tokens);
        if (nextToken.Type == Lexer.TokenType.OpenParenthesis)
        {
            TakeToken(tokens);
            var inner = ParseAbstractDeclarator(tokens);
            Expect(Lexer.TokenType.CloseParenthesis, tokens);
            while (Peek(tokens).Type == Lexer.TokenType.OpenBracket)
            {
                TakeToken(tokens);
                var constant = ParseConstant(tokens);
                if (constant is Const.ConstDouble)
                    throw new Exception("Parsing Error: Cannot declare array dimension with constant of type double");
                Expect(Lexer.TokenType.CloseBracket, tokens);
                inner = new AbstractDeclarator.AbstractArray(inner, GetValue(constant));
            }
            return inner;
        }
        else
        {
            TakeToken(tokens);
            var constant = ParseConstant(tokens);
            if (constant is Const.ConstDouble)
                throw new Exception("Parsing Error: Cannot declare array dimension with constant of type double");
            Expect(Lexer.TokenType.CloseBracket, tokens);
            var inner = new AbstractDeclarator.AbstractBase();
            var outer = new AbstractDeclarator.AbstractArray(inner, GetValue(constant));
            while (Peek(tokens).Type == Lexer.TokenType.OpenBracket)
            {
                TakeToken(tokens);
                constant = ParseConstant(tokens);
                if (constant is Const.ConstDouble)
                    throw new Exception("Parsing Error: Cannot declare array dimension with constant of type double");
                Expect(Lexer.TokenType.CloseBracket, tokens);
                outer = new AbstractDeclarator.AbstractArray(outer, GetValue(constant));
            }
            return outer;
        }
    }

    private Type ProcessAbstractDeclarator(AbstractDeclarator abstractDeclarator, Type baseType)
    {
        switch (abstractDeclarator)
        {
            case AbstractDeclarator.AbstractBase:
                return baseType;
            case AbstractDeclarator.AbstractPointer abstractPointer:
                {
                    var derivedType = new Type.Pointer(baseType);
                    return ProcessAbstractDeclarator(abstractPointer.AbstractDeclarator, derivedType);
                }
            case AbstractDeclarator.AbstractArray abstractArray:
                {
                    var derivedType = new Type.Array(baseType, abstractArray.Size);
                    return ProcessAbstractDeclarator(abstractArray.AbstractDeclarator, derivedType);
                }
            default:
                throw new NotImplementedException();
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
            Lexer.TokenType.Asterisk => Expression.UnaryOperator.Dereference,
            Lexer.TokenType.Ampersand => Expression.UnaryOperator.AddressOf,
            _ => throw new Exception($"Parsing Error: Unknown Unary Operator: {current.Type}")
        };
    }

    private bool IsUnaryOperator(Token token)
    {
        return token.Type == Lexer.TokenType.Hyphen ||
        token.Type == Lexer.TokenType.Tilde ||
        token.Type == Lexer.TokenType.Exclamation ||
        token.Type == Lexer.TokenType.Asterisk ||
        token.Type == Lexer.TokenType.Ampersand;
    }

    private bool IsSpecifier(Token token)
    {
        return IsTypeSpecifier(token.Type) ||
            token.Type == Lexer.TokenType.StaticKeyword ||
            token.Type == Lexer.TokenType.ExternKeyword;
    }

    private bool IsTypeSpecifier(Lexer.TokenType tokenType)
    {
        return tokenType == Lexer.TokenType.IntKeyword ||
            tokenType == Lexer.TokenType.LongKeyword ||
            tokenType == Lexer.TokenType.SignedKeyword ||
            tokenType == Lexer.TokenType.UnsignedKeyword ||
            tokenType == Lexer.TokenType.DoubleKeyword ||
            tokenType == Lexer.TokenType.CharKeyword;
    }

    private bool IsConstant(Token token)
    {
        return token.Type == Lexer.TokenType.IntConstant ||
            token.Type == Lexer.TokenType.LongConstant ||
            token.Type == Lexer.TokenType.UnsignedIntConstant ||
            token.Type == Lexer.TokenType.UnsignedLongConstant ||
            token.Type == Lexer.TokenType.DoubleConstant ||
            token.Type == Lexer.TokenType.CharacterConstant;
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
        if (token.Type == Lexer.TokenType.CharacterConstant)
        {
            Regex regex = new("""
            '([^'\\\n]|\\['"?\\abfnrtv])'
            """);
            Match match = regex.Match(source, token.Position);
            return new Const.ConstInt(Convert.ToInt32(match.Value[0]));
        }

        if (token.Type == Lexer.TokenType.DoubleConstant)
        {
            Regex regex = new(@"(([0-9]*\.[0-9]+|[0-9]+\.?)[Ee][+-]?[0-9]+|[0-9]*\.[0-9]+|[0-9]+\.)[^\w.]");
            Match match = regex.Match(source, token.Position);
            return new Const.ConstDouble(double.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
        }

        System.Numerics.BigInteger value;
        if (token.Type == Lexer.TokenType.IntConstant)
        {
            Regex regex = new(@"([0-9]+)[^\w.]");
            Match match = regex.Match(source, token.Position);
            value = System.Numerics.BigInteger.Parse(match.Groups[1].Value);
        }
        else if (token.Type == Lexer.TokenType.LongConstant)
        {
            Regex regex = new(@"([0-9]+[lL])[^\w.]");
            Match match = regex.Match(source, token.Position);
            // note: this does not parse the l suffix, so we remove it
            value = System.Numerics.BigInteger.Parse(match.Groups[1].Value[0..^1]);
        }
        else if (token.Type == Lexer.TokenType.UnsignedIntConstant)
        {
            Regex regex = new(@"([0-9]+[uU])[^\w.]");
            Match match = regex.Match(source, token.Position);
            // note: this does not parse the u suffix, so we remove it
            value = System.Numerics.BigInteger.Parse(match.Groups[1].Value[0..^1]);
        }
        else
        {
            Regex regex = new(@"([0-9]+([lL][uU]|[uU][lL]))[^\w.]");
            Match match = regex.Match(source, token.Position);
            // note: this does not parse the ul/lu suffix, so we remove it
            value = System.Numerics.BigInteger.Parse(match.Groups[1].Value[0..^2]);
        }

        if (token.Type == Lexer.TokenType.UnsignedIntConstant || token.Type == Lexer.TokenType.UnsignedLongConstant)
        {
            if (value > ulong.MaxValue)
                throw new Exception("Parsing Error: Constant is too large to represent as an uint or ulong");

            if (token.Type == Lexer.TokenType.UnsignedIntConstant && value <= uint.MaxValue)
                return new Const.ConstUInt((uint)value);

            return new Const.ConstULong((ulong)value);
        }
        else
        {
            if (value > long.MaxValue)
                throw new Exception("Parsing Error: Constant is too large to represent as an int or long");

            if (token.Type == Lexer.TokenType.IntConstant && value <= int.MaxValue)
                return new Const.ConstInt((int)value);

            return new Const.ConstLong((long)value);
        }
    }
}