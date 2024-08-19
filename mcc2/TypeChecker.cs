using mcc2.AST;
using static mcc2.SemanticAnalyzer;

namespace mcc2;

public class TypeChecker
{
    private Type functionReturnType = new Type.Int();

    public void Check(ASTProgram program, Dictionary<string, SymbolEntry> symbolTable)
    {
        for (int i = 0; i < program.Declarations.Count; i++)
        {
            Declaration? decl = program.Declarations[i];
            if (decl is Declaration.FunctionDeclaration fun)
                program.Declarations[i] = TypeCheckFunctionDeclaration(fun, symbolTable);
            else if (decl is Declaration.VariableDeclaration var)
                program.Declarations[i] = TypeCheckFileScopeVariableDeclaration(var, symbolTable);
        }
    }

    private Declaration.FunctionDeclaration TypeCheckFunctionDeclaration(Declaration.FunctionDeclaration functionDeclaration, Dictionary<string, SymbolEntry> symbolTable)
    {
        Type.FunctionType funType = (Type.FunctionType)functionDeclaration.FunctionType;
        if (funType.Return is Type.Array)
            throw new Exception("Type Error: A function cannot return an array");

        List<Type> adjustedParams = [];
        foreach (var t in funType.Parameters)
        {
            switch (t)
            {
                case Type.Array array:
                    adjustedParams.Add(new Type.Pointer(array.Element));
                    break;
                default:
                    adjustedParams.Add(t);
                    break;
            }
        }
        funType.Parameters.Clear();
        funType.Parameters.AddRange(adjustedParams);

        bool hasBody = functionDeclaration.Body != null;
        bool alreadyDefined = false;
        bool global = functionDeclaration.StorageClass != Declaration.StorageClasses.Static;

        if (symbolTable.TryGetValue(functionDeclaration.Identifier, out SymbolEntry prevEntry))
        {
            var attributes = (IdentifierAttributes.Function)prevEntry.IdentifierAttributes;
            // note: check correct type and number of parameters
            var prevFunType = (Type.FunctionType)prevEntry.Type;
            if (prevFunType is not Type.FunctionType funcA || funcA.Parameters.Count != funType.Parameters.Count || funcA.Return != funType.Return)
                throw new Exception("Type Error: Incompatible function declarations");

            for (int i = 0; i < prevFunType.Parameters.Count; i++)
            {
                if (prevFunType.Parameters[i] != funType.Parameters[i])
                    throw new Exception("Type Error: Incompatible function declarations");
            }

            alreadyDefined = attributes.Defined;
            if (alreadyDefined && hasBody)
                throw new Exception("Type Error: Function is defined more than once");

            if (attributes.Global && functionDeclaration.StorageClass == Declaration.StorageClasses.Static)
                throw new Exception("Static function declaration follows non-static");

            global = attributes.Global;
        }

        symbolTable[functionDeclaration.Identifier] = new SymbolEntry() { Type = funType, IdentifierAttributes = new IdentifierAttributes.Function(alreadyDefined || hasBody, global) };

        if (functionDeclaration.Body != null)
        {
            functionReturnType = funType.Return;
            for (int i = 0; i < functionDeclaration.Parameters.Count; i++)
            {
                string? param = functionDeclaration.Parameters[i];
                symbolTable.Add(param, new SymbolEntry() { Type = funType.Parameters[i] });
            }

            return new Declaration.FunctionDeclaration(functionDeclaration.Identifier, functionDeclaration.Parameters, TypeCheckBlock(functionDeclaration.Body, symbolTable), funType, functionDeclaration.StorageClass);
        }

        return functionDeclaration;
    }

    private Block TypeCheckBlock(Block block, Dictionary<string, SymbolEntry> symbolTable)
    {
        List<BlockItem> newItems = [];
        for (int i = 0; i < block.BlockItems.Count; i++)
        {
            BlockItem? item = block.BlockItems[i];
            if (item is Declaration.VariableDeclaration declaration)
            {
                newItems.Add(TypeCheckLocalVariableDeclaration(declaration, symbolTable));
            }
            else if (item is Declaration.FunctionDeclaration functionDeclaration)
            {
                if (functionDeclaration.StorageClass != Declaration.StorageClasses.Static)
                    newItems.Add(TypeCheckFunctionDeclaration(functionDeclaration, symbolTable));
                else
                    throw new Exception("Type Error: StorageClass static used in block function declaration");
            }
            else if (item is Statement statement)
            {
                newItems.Add(TypeCheckStatement(statement, symbolTable));
            }
        }
        return new Block(newItems);
    }

    private Declaration.VariableDeclaration TypeCheckFileScopeVariableDeclaration(Declaration.VariableDeclaration variableDeclaration, Dictionary<string, SymbolEntry> symbolTable)
    {
        InitialValue initialValue;
        if (variableDeclaration.Initializer != null)
            initialValue = new InitialValue.Initial(ConvertToStaticInit(variableDeclaration.VariableType, variableDeclaration.Initializer));
        else if (variableDeclaration.Initializer == null)
        {
            if (variableDeclaration.StorageClass == Declaration.StorageClasses.Extern)
                initialValue = new InitialValue.NoInitializer();
            else
                initialValue = new InitialValue.Tentative();
        }
        else
            throw new Exception("Type Error: Non-Constant initializer");

        var global = variableDeclaration.StorageClass != Declaration.StorageClasses.Static;

        if (symbolTable.TryGetValue(variableDeclaration.Identifier, out SymbolEntry prevEntry))
        {
            var attributes = (IdentifierAttributes.Static)prevEntry.IdentifierAttributes;
            if (prevEntry.Type != variableDeclaration.VariableType)
                throw new Exception("Type Error: Function redeclared as variable");
            if (variableDeclaration.StorageClass == Declaration.StorageClasses.Extern)
                global = attributes.Global;
            else if (attributes.Global != global)
                throw new Exception("Type Error: Conflicting variable linkage");

            if (attributes.InitialValue is InitialValue.Initial)
                if (initialValue is InitialValue.Initial)
                    throw new Exception("Type Error: Conflicting file scope variable definitions");
                else
                    initialValue = attributes.InitialValue;
            else if (initialValue is not InitialValue.Initial && attributes.InitialValue is InitialValue.Tentative)
                initialValue = new InitialValue.Tentative();
        }

        symbolTable[variableDeclaration.Identifier] = new SymbolEntry() { Type = variableDeclaration.VariableType, IdentifierAttributes = new IdentifierAttributes.Static(initialValue, global) };
        return variableDeclaration;
    }

    private Declaration.VariableDeclaration TypeCheckLocalVariableDeclaration(Declaration.VariableDeclaration variableDeclaration, Dictionary<string, SymbolEntry> symbolTable)
    {
        if (variableDeclaration.StorageClass == Declaration.StorageClasses.Extern)
        {
            if (variableDeclaration.Initializer != null)
                throw new Exception("Type Error: Initializer on local extern variable declaration");
            if (symbolTable.TryGetValue(variableDeclaration.Identifier, out SymbolEntry prevEntry))
            {
                if (prevEntry.Type != variableDeclaration.VariableType)
                    throw new Exception("Type Error: Function redeclared as variable");
            }
            else
                symbolTable.Add(variableDeclaration.Identifier, new SymbolEntry() { Type = variableDeclaration.VariableType, IdentifierAttributes = new IdentifierAttributes.Static(new InitialValue.NoInitializer(), true) });
        }
        else if (variableDeclaration.StorageClass == Declaration.StorageClasses.Static)
        {
            InitialValue initialValue;
            if (variableDeclaration.Initializer != null)
                initialValue = new InitialValue.Initial(ConvertToStaticInit(variableDeclaration.VariableType, variableDeclaration.Initializer));
            else if (variableDeclaration.Initializer == null)
                initialValue = new InitialValue.Initial([new StaticInit.ZeroInit(GetTypeSize(variableDeclaration.VariableType))]);
            else
                throw new Exception("Type Error: Non-constant initializer on local static variable");
            symbolTable[variableDeclaration.Identifier] = new SymbolEntry() { Type = variableDeclaration.VariableType, IdentifierAttributes = new IdentifierAttributes.Static(initialValue, false) };
        }
        else
        {
            symbolTable[variableDeclaration.Identifier] = new SymbolEntry() { Type = variableDeclaration.VariableType, IdentifierAttributes = new IdentifierAttributes.Local() };
            if (variableDeclaration.Initializer != null)
                return new Declaration.VariableDeclaration(variableDeclaration.Identifier, TypeCheckInitializer(variableDeclaration.VariableType, variableDeclaration.Initializer, symbolTable), variableDeclaration.VariableType, variableDeclaration.StorageClass);
        }
        return variableDeclaration;
    }

    private List<StaticInit> ConvertToStaticInit(Type targetType, Initializer initializer)
    {
        List<StaticInit> staticInits = [];

        switch (targetType, initializer)
        {
            case (Type.Array array, Initializer.CompoundInitializer compound):
                if (compound.Initializers.Count > array.Size)
                    throw new Exception("Type Error: Wrong number of values in initializer");

                List<StaticInit> typeCheckedInits = [];
                foreach (var init in compound.Initializers)
                {
                    var typecheckedElem = ConvertToStaticInit(array.Element, init);
                    typeCheckedInits.AddRange(typecheckedElem);
                }
                if ((array.Size - compound.Initializers.Count) > 0)
                    typeCheckedInits.Add(new StaticInit.ZeroInit(GetTypeSize(array.Element) * (array.Size - compound.Initializers.Count)));
                staticInits.AddRange(typeCheckedInits);
                break;
            case (Type.Array, Initializer.SingleInitializer):
                throw new Exception("Type Error: Can't initialize array from scalar value");
            case (_, Initializer.SingleInitializer single):
                if (single.Expression is Expression.Constant constant)
                {
                    // if (IsZeroInteger(constant.Value))
                    //     staticInits.Add(ConvertConstantToInit(constant.Type, new Const.ConstULong(0)));
                    // else
                    staticInits.Add(ConvertConstantToInit(targetType, constant.Value));
                }
                else
                    throw new Exception("Type Error: Non-Static initializer");
                break;
            default:
                throw new Exception("Type Error: Can't initialize a scalar object with a compound initializer");
        }

        return staticInits;
    }

    private Initializer TypeCheckInitializer(Type targetType, Initializer initializer, Dictionary<string, SymbolEntry> symbolTable)
    {
        switch (targetType, initializer)
        {
            case (_, Initializer.SingleInitializer single):
                {
                    var typecheckedExp = TypeCheckAndConvertExpression(single.Expression, symbolTable);
                    var castExp = ConvertByAssignment(typecheckedExp, targetType);
                    return new Initializer.SingleInitializer(castExp, targetType);
                }
            case (Type.Array array, Initializer.CompoundInitializer compound):
                if (compound.Initializers.Count > array.Size)
                    throw new Exception("Type Error: Wrong number of values in initializer");

                List<Initializer> typecheckedInits = [];
                foreach (var init in compound.Initializers)
                {
                    var typecheckedElem = TypeCheckInitializer(array.Element, init, symbolTable);
                    typecheckedInits.Add(typecheckedElem);
                }
                while (typecheckedInits.Count < array.Size)
                    typecheckedInits.Add(ZeroInitializer(array.Element));
                return new Initializer.CompoundInitializer(typecheckedInits, targetType);
            default:
                throw new Exception("Type Error: Can't initialize a scalar object with a compound initializer");
        }
    }

    private Initializer ZeroInitializer(Type element)
    {
        switch (element)
        {
            case Type.Int:
                return new Initializer.SingleInitializer(new Expression.Constant(new Const.ConstInt(0), element), element);
            case Type.UInt:
                return new Initializer.SingleInitializer(new Expression.Constant(new Const.ConstUInt(0), element), element);
            case Type.Long:
                return new Initializer.SingleInitializer(new Expression.Constant(new Const.ConstLong(0), element), element);
            case Type.ULong:
                return new Initializer.SingleInitializer(new Expression.Constant(new Const.ConstULong(0), element), element);
            case Type.Double or Type.Pointer:
                return new Initializer.SingleInitializer(new Expression.Constant(new Const.ConstDouble(0), element), element);
            case Type.Array array:
                List<Initializer> inits = [];
                for (int i = 0; i < array.Size; i++)
                {
                    inits.Add(ZeroInitializer(array.Element));
                }
                return new Initializer.CompoundInitializer(inits, element);
            default:
                throw new Exception($"Type Error: Can't zero initialize with type {element}");
        }
    }

    private Expression TypeCheckExpression(Expression expression, Dictionary<string, SymbolEntry> symbolTable)
    {
        switch (expression)
        {
            case Expression.Assignment assignment:
                {
                    var typedLeft = TypeCheckAndConvertExpression(assignment.Left, symbolTable);
                    if (!IsLvalue(typedLeft))
                        throw new Exception("Type Error: Tried to assign to non-lvalue");
                    var typedRight = TypeCheckAndConvertExpression(assignment.Right, symbolTable);
                    var leftType = GetType(typedLeft);
                    var convertedRight = ConvertByAssignment(typedRight, leftType);
                    return new Expression.Assignment(typedLeft, convertedRight, leftType);
                }
            case Expression.Variable variable:
                {
                    var varType = symbolTable[variable.Identifier].Type;
                    if (varType is Type.FunctionType)
                        throw new Exception("Type Error: Function name used as variable");
                    return new Expression.Variable(variable.Identifier, varType);
                }
            case Expression.Unary unary:
                {
                    var unaryInner = TypeCheckAndConvertExpression(unary.Expression, symbolTable);
                    if (unary.Operator is Expression.UnaryOperator.Negate && !IsArithmetic(GetType(unaryInner)))
                        throw new Exception("Type Error: Can only negate arithmetic types");
                    if (unary.Operator is Expression.UnaryOperator.Complement && !IsArithmetic(GetType(unaryInner)))
                        throw new Exception("Type Error: Bitwise complement only valid for integer types");
                    if (unary.Operator == Expression.UnaryOperator.Complement && GetType(unaryInner) is Type.Double)
                        throw new Exception("Type Error: Can't take the bitwise complement of a double");
                    return new Expression.Unary(unary.Operator, unaryInner, unary.Operator switch
                    {
                        Expression.UnaryOperator.Not => new Type.Int(),
                        _ => GetType(unaryInner)
                    });
                }
            case Expression.Binary binary:
                {
                    var typedE1 = TypeCheckAndConvertExpression(binary.Left, symbolTable);
                    var typedE2 = TypeCheckAndConvertExpression(binary.Right, symbolTable);
                    if (binary.Operator is Expression.BinaryOperator.And or Expression.BinaryOperator.Or)
                    {
                        return new Expression.Binary(binary.Operator, typedE1, typedE2, new Type.Int());
                    }
                    var t1 = GetType(typedE1);
                    var t2 = GetType(typedE2);
                    if (binary.Operator == Expression.BinaryOperator.Add)
                    {
                        if (IsArithmetic(t1) && IsArithmetic(t2))
                        {
                            var commonAdd = GetCommonType(t1, t2);
                            var convertedLeft = ConvertTo(typedE1, commonAdd);
                            var convertedRight = ConvertTo(typedE2, commonAdd);
                            return new Expression.Binary(binary.Operator, convertedLeft, convertedRight, commonAdd);
                        }
                        else if (t1 is Type.Pointer && IsInteger(t2))
                        {
                            var convertedRight = ConvertTo(typedE2, new Type.Long());
                            return new Expression.Binary(binary.Operator, typedE1, convertedRight, t1);
                        }
                        else if (t2 is Type.Pointer && IsInteger(t1))
                        {
                            var convertedLeft = ConvertTo(typedE1, new Type.Long());
                            return new Expression.Binary(binary.Operator, convertedLeft, typedE2, t2);
                        }
                        else
                            throw new Exception("Type Error: Invalid operands for addition");
                    }
                    if (binary.Operator == Expression.BinaryOperator.Subtract)
                    {
                        if (IsArithmetic(t1) && IsArithmetic(t2))
                        {
                            var commonSub = GetCommonType(t1, t2);
                            var convertedLeft = ConvertTo(typedE1, commonSub);
                            var convertedRight = ConvertTo(typedE2, commonSub);
                            return new Expression.Binary(binary.Operator, convertedLeft, convertedRight, commonSub);
                        }
                        else if (t1 is Type.Pointer && IsInteger(t2))
                        {
                            var convertedRight = ConvertTo(typedE2, new Type.Long());
                            return new Expression.Binary(binary.Operator, typedE1, convertedRight, t1);
                        }
                        else if (t2 is Type.Pointer && t1 == t2)
                        {
                            return new Expression.Binary(binary.Operator, typedE1, typedE2, new Type.Long());
                        }
                        else
                            throw new Exception("Type Error: Invalid operands for subtraction");
                    }
                    if (binary.Operator is Expression.BinaryOperator.LessThan or Expression.BinaryOperator.LessOrEqual or
                        Expression.BinaryOperator.GreaterThan or Expression.BinaryOperator.GreaterOrEqual)
                    {
                        var commonComp = (IsArithmetic(t1) && IsArithmetic(t2))
                            ? GetCommonType(t1, t2)
                            : (t1 is Type.Pointer && t1 == t2) ?
                            t1 : throw new Exception("Type Error: Invalid types for comparison");
                        var convertedLeft = ConvertTo(typedE1, commonComp);
                        var convertedRight = ConvertTo(typedE2, commonComp);
                        return new Expression.Binary(binary.Operator, convertedLeft, convertedRight, new Type.Int());
                    }
                    if (binary.Operator is Expression.BinaryOperator.Equal or Expression.BinaryOperator.NotEqual)
                    {
                        var commonEquality = (t1 is Type.Pointer || t2 is Type.Pointer)
                            ? GetCommonPointerType(typedE1, typedE2)
                            : (IsArithmetic(t1) && IsArithmetic(t2)) ?
                            GetCommonType(t1, t2)
                            : throw new Exception("Type Error: Invalid operands for equality");
                        var convertedLeft = ConvertTo(typedE1, commonEquality);
                        var convertedRight = ConvertTo(typedE2, commonEquality);
                        return new Expression.Binary(binary.Operator, convertedLeft, convertedRight, new Type.Int());
                    }

                    // todo: refactor this mess
                    var commonType = (binary.Operator is Expression.BinaryOperator.Equal or Expression.BinaryOperator.NotEqual &&
                        (t1 is Type.Pointer || t2 is Type.Pointer))
                        ? GetCommonPointerType(typedE1, typedE2)
                        : GetCommonType(t1, t2);
                    if (binary.Operator == Expression.BinaryOperator.Remainder && commonType is Type.Double)
                        throw new Exception("Type Error: Can't apply remainder to double");
                    if (binary.Operator is Expression.BinaryOperator.Multiply or Expression.BinaryOperator.Divide or Expression.BinaryOperator.Remainder &&
                        !IsArithmetic(commonType))
                        throw new Exception("Type Error: Can only multiply arithmetic types");
                    var convertedE1 = ConvertTo(typedE1, commonType);
                    var convertedE2 = ConvertTo(typedE2, commonType);
                    return new Expression.Binary(binary.Operator, convertedE1, convertedE2, binary.Operator switch
                    {
                        Expression.BinaryOperator.Add or
                        Expression.BinaryOperator.Subtract or
                        Expression.BinaryOperator.Multiply or
                        Expression.BinaryOperator.Divide or
                        Expression.BinaryOperator.Remainder =>
                            commonType,
                        _ => new Type.Int() // note: comparison results in int
                    });
                }
            case Expression.Constant constant:
                return new Expression.Constant(constant.Value, constant.Value switch
                {
                    Const.ConstInt => new Type.Int(),
                    Const.ConstLong => new Type.Long(),
                    Const.ConstUInt => new Type.UInt(),
                    Const.ConstULong => new Type.ULong(),
                    Const.ConstDouble => new Type.Double(),
                    _ => throw new NotImplementedException()
                });
            case Expression.Conditional conditional:
                var typedCond = TypeCheckAndConvertExpression(conditional.Condition, symbolTable);
                var typedThen = TypeCheckAndConvertExpression(conditional.Then, symbolTable);
                var typedElse = TypeCheckAndConvertExpression(conditional.Else, symbolTable);
                var typeThen = GetType(typedThen);
                var typeElse = GetType(typedElse);
                var common = (typeThen is Type.Pointer || typeElse is Type.Pointer)
                    ? GetCommonPointerType(typedThen, typedElse)
                    : GetCommonType(typeThen, typeElse);
                var convertedThen = ConvertTo(typedThen, common);
                var convertedElse = ConvertTo(typedElse, common);
                return new Expression.Conditional(typedCond, convertedThen, convertedElse, common);
            case Expression.FunctionCall functionCall:
                var funType = symbolTable[functionCall.Identifier].Type;
                if (funType is not Type.FunctionType)
                    throw new Exception("Type Error: Variable used as function name");

                if (funType is Type.FunctionType functionType && functionType.Parameters.Count != functionCall.Arguments.Count)
                    throw new Exception("Type Error: Function called with the wrong number of arguments");

                List<Expression> convertedArgs = [];
                for (int i = 0; i < functionCall.Arguments.Count; i++)
                {
                    var typedArg = TypeCheckAndConvertExpression(functionCall.Arguments[i], symbolTable);
                    convertedArgs.Add(ConvertByAssignment(typedArg, ((Type.FunctionType)funType).Parameters[i]));
                }
                return new Expression.FunctionCall(functionCall.Identifier, convertedArgs, ((Type.FunctionType)funType).Return);
            case Expression.Cast cast:
                {
                    var typedInner = TypeCheckAndConvertExpression(cast.Expression, symbolTable);
                    if (cast.TargetType is Type.Double && GetType(typedInner) is Type.Pointer ||
                        cast.TargetType is Type.Pointer && GetType(typedInner) is Type.Double)
                        throw new Exception("Type Error: Cannot cast between pointer and double");
                    if (GetType(typedInner) is Type.Array)
                        throw new Exception("Type Error: Cannot cast expression to array");
                    return new Expression.Cast(cast.TargetType, typedInner, cast.TargetType);
                }
            case Expression.Dereference dereference:
                {
                    var typedInner = TypeCheckAndConvertExpression(dereference.Expression, symbolTable);
                    return GetType(typedInner) switch
                    {
                        Type.Pointer pointer => new Expression.Dereference(typedInner, pointer.Referenced),
                        _ => throw new Exception("Type Error: Cannot dereference non-pointer"),
                    };
                }
            case Expression.AddressOf addressOf:
                {
                    if (IsLvalue(addressOf.Expression))
                    {
                        var typedInner = TypeCheckExpression(addressOf.Expression, symbolTable);
                        var referencedType = GetType(typedInner);
                        return new Expression.AddressOf(typedInner, new Type.Pointer(referencedType));
                    }
                    else
                        throw new Exception("Type Error: Can't take the address of a non-lvalue");
                }
            case Expression.Subscript subscript:
                {
                    var typedE1 = TypeCheckAndConvertExpression(subscript.Left, symbolTable);
                    var typedE2 = TypeCheckAndConvertExpression(subscript.Right, symbolTable);
                    var t1 = GetType(typedE1);
                    var t2 = GetType(typedE2);
                    Type? pointerType;
                    if (t1 is Type.Pointer && IsInteger(t2))
                    {
                        pointerType = t1;
                        typedE2 = ConvertTo(typedE2, new Type.Long());
                    }
                    else if (IsInteger(t1) && t2 is Type.Pointer)
                    {
                        pointerType = t2;
                        typedE1 = ConvertTo(typedE1, new Type.Long());
                    }
                    else
                        throw new Exception("Type Error: Subscript must have integer and pointer operands");
                    return new Expression.Subscript(typedE1, typedE2, ((Type.Pointer)pointerType).Referenced);
                }
            default:
                throw new NotImplementedException();
        }
    }

    private Expression TypeCheckAndConvertExpression(Expression expression, Dictionary<string, SymbolEntry> symbolTable)
    {
        var typedExp = TypeCheckExpression(expression, symbolTable);
        return GetType(typedExp) switch
        {
            Type.Array array => new Expression.AddressOf(typedExp, new Type.Pointer(array.Element)),
            _ => typedExp,
        };
    }

    private Expression ConvertByAssignment(Expression exp, Type targetType)
    {
        if (GetType(exp) == targetType)
            return exp;
        else if (IsArithmetic(GetType(exp)) && IsArithmetic(targetType))
            return ConvertTo(exp, targetType);
        else if (IsNullPointerConstant(exp) && targetType is Type.Pointer)
            return ConvertTo(exp, targetType);
        else
            throw new Exception("Type Error: Cannot convert type for assignment");
    }

    private bool IsInteger(Type type)
    {
        return type switch
        {
            Type.Int or Type.Long or Type.UInt or Type.ULong => true,
            _ => false
        };
    }

    private bool IsArithmetic(Type type)
    {
        return type switch
        {
            Type.Int or Type.Long or Type.UInt or Type.ULong or Type.Double => true,
            _ => false
        };
    }

    private bool IsLvalue(Expression exp)
    {
        return exp is Expression.Variable or Expression.Dereference or Expression.Subscript;
    }

    private bool IsZeroInteger(Const constant)
    {
        return constant switch
        {
            Const.ConstInt c => c.Value == 0,
            Const.ConstUInt c => c.Value == 0,
            Const.ConstLong c => c.Value == 0,
            Const.ConstULong c => c.Value == 0,
            _ => false
        };
    }

    private bool IsNullPointerConstant(Expression exp)
    {
        return exp switch
        {
            Expression.Constant constant => IsZeroInteger(constant.Value),
            _ => false
        };
    }

    private Type GetCommonPointerType(Expression exp1, Expression exp2)
    {
        var type1 = GetType(exp1);
        var type2 = GetType(exp2);
        if (type1 == type2)
            return type1;
        else if (IsNullPointerConstant(exp1))
            return type2;
        else if (IsNullPointerConstant(exp2))
            return type1;
        else
            throw new Exception("Type Error: Expressions have incompatible types");
    }

    private Statement TypeCheckStatement(Statement statement, Dictionary<string, SymbolEntry> symbolTable)
    {
        switch (statement)
        {
            case Statement.ReturnStatement ret:
                var typedReturn = TypeCheckAndConvertExpression(ret.Expression, symbolTable);
                var converted = ConvertByAssignment(typedReturn, functionReturnType);
                return new Statement.ReturnStatement(converted);
            case Statement.ExpressionStatement expressionStatement:
                return new Statement.ExpressionStatement(TypeCheckAndConvertExpression(expressionStatement.Expression, symbolTable));
            case Statement.NullStatement nullStatement:
                return nullStatement;
            case Statement.IfStatement ifStatement:
                var ifCond = TypeCheckAndConvertExpression(ifStatement.Condition, symbolTable);
                var ifThen = TypeCheckStatement(ifStatement.Then, symbolTable);
                var ifElse = ifStatement.Else;
                if (ifElse != null)
                    ifElse = TypeCheckStatement(ifElse, symbolTable);
                return new Statement.IfStatement(ifCond, ifThen, ifElse);
            case Statement.CompoundStatement compoundStatement:
                return new Statement.CompoundStatement(TypeCheckBlock(compoundStatement.Block, symbolTable));
            case Statement.BreakStatement breakStatement:
                return breakStatement;
            case Statement.ContinueStatement continueStatement:
                return continueStatement;
            case Statement.WhileStatement whileStatement:
                var whileCond = TypeCheckAndConvertExpression(whileStatement.Condition, symbolTable);
                var whileBody = TypeCheckStatement(whileStatement.Body, symbolTable);
                return new Statement.WhileStatement(whileCond, whileBody, whileStatement.Label);
            case Statement.DoWhileStatement doWhileStatement:
                var doBody = TypeCheckStatement(doWhileStatement.Body, symbolTable);
                var doCond = TypeCheckAndConvertExpression(doWhileStatement.Condition, symbolTable);
                return new Statement.DoWhileStatement(doBody, doCond, doWhileStatement.Label);
            case Statement.ForStatement forStatement:
                {
                    var forInit = TypeCheckForInit(forStatement.Init, symbolTable);
                    var cond = TypeCheckOptionalExpression(forStatement.Condition, symbolTable);
                    var post = TypeCheckOptionalExpression(forStatement.Post, symbolTable);
                    var body = TypeCheckStatement(forStatement.Body, symbolTable);
                    return new Statement.ForStatement(forInit, cond, post, body, forStatement.Label);
                }
            default:
                throw new NotImplementedException();
        }
    }

    private ForInit TypeCheckForInit(ForInit init, Dictionary<string, SymbolEntry> symbolTable)
    {
        switch (init)
        {
            case ForInit.InitExpression initExpression:
                return new ForInit.InitExpression(TypeCheckOptionalExpression(initExpression.Expression, symbolTable));
            case ForInit.InitDeclaration initDeclaration:
                if (initDeclaration.Declaration.StorageClass == null)
                    return new ForInit.InitDeclaration(TypeCheckLocalVariableDeclaration(initDeclaration.Declaration, symbolTable));
                else
                    throw new Exception("Type Error: StorageClass used in for loop init");
            default:
                throw new NotImplementedException();
        }
    }

    private Expression? TypeCheckOptionalExpression(Expression? expression, Dictionary<string, SymbolEntry> symbolTable)
    {
        if (expression != null)
            return TypeCheckAndConvertExpression(expression, symbolTable);
        else
            return null;
    }

    public static StaticInit ConvertConstantToInit(Type target, Const constant)
    {
        if (constant is Const.ConstDouble cd && target is Type.Double)
            return new StaticInit.DoubleInit(cd.Value);

        ulong value = constant switch
        {
            Const.ConstInt constInt => (ulong)constInt.Value,
            Const.ConstLong constLong => (ulong)constLong.Value,
            Const.ConstUInt constUInt => (ulong)constUInt.Value,
            Const.ConstULong constULong => (ulong)constULong.Value,
            Const.ConstDouble constDouble => (ulong)constDouble.Value,
            _ => throw new NotImplementedException()
        };

        if (target is Type.Pointer && value != 0)
            throw new Exception("Type Error: Invalid static initializer for pointer");

        return target switch
        {
            Type.Int => new StaticInit.IntInit((int)value),
            Type.Long => new StaticInit.LongInit((long)value),
            Type.UInt => new StaticInit.UIntInit((uint)value),
            Type.ULong => new StaticInit.ULongInit((ulong)value),
            Type.Double => new StaticInit.DoubleInit((double)value),
            Type.Pointer => new StaticInit.ULongInit(value),
            Type.Array array => new StaticInit.ZeroInit(array.Size * GetTypeSize(array.Element)),
            _ => throw new NotImplementedException()
        };
    }

    private Expression ConvertTo(Expression expression, Type type)
    {
        if (GetType(expression) == type)
            return expression;

        return new Expression.Cast(type, expression, type);
    }

    private Type GetCommonType(Type type1, Type type2)
    {
        if (type1 == type2)
            return type1;
        if (type1 is Type.Double || type2 is Type.Double)
            return new Type.Double();
        if (GetTypeSize(type1) == GetTypeSize(type2))
        {
            if (IsSignedType(type1))
                return type2;
            else
                return type1;
        }
        if (GetTypeSize(type1) > GetTypeSize(type2))
            return type1;
        else
            return type2;
    }

    public static int GetTypeSize(Type type)
    {
        return type switch
        {
            Type.Int or Type.UInt => 4,
            Type.Long or Type.ULong or Type.Pointer or Type.Double => 8,
            Type.Array array => GetTypeSize(array.Element) * array.Size,
            _ => throw new NotImplementedException()
        };
    }

    public static bool IsSignedType(Type type)
    {
        return type switch
        {
            Type.Int or Type.Long => true,
            Type.UInt or Type.ULong => false,
            _ => throw new NotImplementedException()
        };
    }

    public static Type GetType(Expression expression)
    {
        return expression switch
        {
            Expression.Constant exp => exp.Type,
            Expression.Variable exp => exp.Type,
            Expression.Unary exp => exp.Type,
            Expression.Binary exp => exp.Type,
            Expression.Assignment exp => exp.Type,
            Expression.Conditional exp => exp.Type,
            Expression.FunctionCall exp => exp.Type,
            Expression.Cast exp => exp.Type,
            Expression.Dereference exp => exp.Type,
            Expression.AddressOf exp => exp.Type,
            Expression.Subscript exp => exp.Type,
            _ => throw new NotImplementedException()
        };
    }
}