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
        bool hasBody = functionDeclaration.Body != null;
        bool alreadyDefined = false;
        bool global = functionDeclaration.StorageClass != Declaration.StorageClasses.Static;

        if (symbolTable.TryGetValue(functionDeclaration.Identifier, out SymbolEntry prevEntry))
        {
            var attributes = (IdentifierAttributes.Function)prevEntry.IdentifierAttributes;
            // note: check correct type and number of parameters
            var prevFunType = (Type.FunctionType) prevEntry.Type;
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

            return new Declaration.FunctionDeclaration(functionDeclaration.Identifier, functionDeclaration.Parameters, TypeCheckBlock(functionDeclaration.Body, symbolTable), funType, functionDeclaration.StorageClass) ;
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
        if (variableDeclaration.Initializer is Expression.Constant constant)
            initialValue = new InitialValue.Initial(ConvertConstantToInit(variableDeclaration.VariableType, constant.Value));
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
            if (variableDeclaration.Initializer is Expression.Constant constant)
                initialValue = new InitialValue.Initial(ConvertConstantToInit(variableDeclaration.VariableType, constant.Value));
            else if (variableDeclaration.Initializer == null)
                initialValue = new InitialValue.Initial(ConvertConstantToInit(variableDeclaration.VariableType, new Const.ConstInt(0)));
            else
                throw new Exception("Type Error: Non-constant initializer on local static variable");
            symbolTable[variableDeclaration.Identifier] = new SymbolEntry() { Type = variableDeclaration.VariableType, IdentifierAttributes = new IdentifierAttributes.Static(initialValue, false) };
        }
        else
        {
            symbolTable[variableDeclaration.Identifier] = new SymbolEntry() { Type = variableDeclaration.VariableType, IdentifierAttributes = new IdentifierAttributes.Local() };
            if (variableDeclaration.Initializer != null)
                return new Declaration.VariableDeclaration(variableDeclaration.Identifier, ConvertTo(TypeCheckExpression(variableDeclaration.Initializer, symbolTable),variableDeclaration.VariableType), variableDeclaration.VariableType, variableDeclaration.StorageClass);
        }
        return variableDeclaration;
    }

    private Expression TypeCheckExpression(Expression expression, Dictionary<string, SymbolEntry> symbolTable)
    {
        switch (expression)
        {
            case Expression.Assignment assignment:
                var typedLeft = TypeCheckExpression(assignment.ExpressionLeft, symbolTable);
                var typedRight = TypeCheckExpression(assignment.ExpressionRight, symbolTable);
                var leftType = GetType(typedLeft);
                var convertedRight = ConvertTo(typedRight, leftType);
                return new Expression.Assignment(typedLeft, convertedRight, leftType);
            case Expression.Variable variable:
                var varType = symbolTable[variable.Identifier].Type;
                if (varType is Type.FunctionType)
                    throw new Exception("Type Error: Function name used as variable");
                return new Expression.Variable(variable.Identifier, varType);
            case Expression.Unary unary:
                var unaryInner = TypeCheckExpression(unary.Expression, symbolTable);
                if (unary.Operator == Expression.UnaryOperator.Complement && GetType(unaryInner) is Type.Double)
                    throw new Exception("Type Error: Can't take the bitwise complement of a double");
                return new Expression.Unary(unary.Operator, unaryInner, unary.Operator switch {
                    Expression.UnaryOperator.Not => new Type.Int(),
                    _ => GetType(unaryInner)
                });
            case Expression.Binary binary:
                var typedE1 = TypeCheckExpression(binary.ExpressionLeft, symbolTable);
                var typedE2 = TypeCheckExpression(binary.ExpressionRight, symbolTable);
                if (binary.Operator is Expression.BinaryOperator.And or Expression.BinaryOperator.Or)
                {
                    return new Expression.Binary(binary.Operator, typedE1, typedE2, new Type.Int());
                }
                var t1 = GetType(typedE1);
                var t2 = GetType(typedE2);
                var commonType = GetCommonType(t1, t2);
                if (binary.Operator == Expression.BinaryOperator.Remainder && commonType is Type.Double)
                    throw new Exception("Type Error: Can't apply remainder to double");
                var convertedE1 = ConvertTo(typedE1, commonType);
                var convertedE2 = ConvertTo(typedE2, commonType);
                return new Expression.Binary(binary.Operator, convertedE1, convertedE2, binary.Operator switch {
                    Expression.BinaryOperator.Add or 
                    Expression.BinaryOperator.Subtract or 
                    Expression.BinaryOperator.Multiply or 
                    Expression.BinaryOperator.Divide or 
                    Expression.BinaryOperator.Remainder =>
                        commonType,
                    _ => new Type.Int() // note: comparison results in int
                });
            case Expression.Constant constant:
                return new Expression.Constant(constant.Value, constant.Value switch {
                    Const.ConstInt => new Type.Int(),
                    Const.ConstLong => new Type.Long(),
                    Const.ConstUInt => new Type.UInt(),
                    Const.ConstULong => new Type.ULong(),
                    Const.ConstDouble => new Type.Double(),
                    _ => throw new NotImplementedException()
                });
            case Expression.Conditional conditional:
                var typedCond = TypeCheckExpression(conditional.Condition, symbolTable);
                var typedThen = TypeCheckExpression(conditional.Then, symbolTable);
                var typedElse = TypeCheckExpression(conditional.Else, symbolTable);
                var typeThen = GetType(typedThen);
                var typeElse = GetType(typedElse);
                var common = GetCommonType(typeThen, typeElse);
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
                    var typedArg = TypeCheckExpression(functionCall.Arguments[i], symbolTable);
                    convertedArgs.Add(ConvertTo(typedArg, ((Type.FunctionType)funType).Parameters[i]));
                }
                return new Expression.FunctionCall(functionCall.Identifier, convertedArgs, ((Type.FunctionType)funType).Return);
            case Expression.Cast cast:
                var typedInner = TypeCheckExpression(cast.Expression, symbolTable);
                return new Expression.Cast(cast.TargetType, typedInner, cast.TargetType);
            default:
                throw new NotImplementedException();
        }
    }

    private Statement TypeCheckStatement(Statement statement, Dictionary<string, SymbolEntry> symbolTable)
    {
        switch (statement)
        {
            case Statement.ReturnStatement ret:
                var typedReturn = TypeCheckExpression(ret.Expression, symbolTable);
                var converted = ConvertTo(typedReturn, functionReturnType);
                return new Statement.ReturnStatement(converted);
            case Statement.ExpressionStatement expressionStatement:
                return new Statement.ExpressionStatement(TypeCheckExpression(expressionStatement.Expression, symbolTable));
            case Statement.NullStatement nullStatement:
                return nullStatement;
            case Statement.IfStatement ifStatement:
                var ifCond = TypeCheckExpression(ifStatement.Condition, symbolTable);
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
                var whileCond = TypeCheckExpression(whileStatement.Condition, symbolTable);
                var whileBody = TypeCheckStatement(whileStatement.Body, symbolTable);
                return new Statement.WhileStatement(whileCond, whileBody, whileStatement.Label);
            case Statement.DoWhileStatement doWhileStatement:
                var doBody = TypeCheckStatement(doWhileStatement.Body, symbolTable);
                var doCond = TypeCheckExpression(doWhileStatement.Condition, symbolTable);
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
            return TypeCheckExpression(expression, symbolTable);
        else
            return null;
    }

    public static StaticInit ConvertConstantToInit(Type target, Const constant)
    {
        if (constant is Const.ConstDouble cd && target is Type.Double)
        {
            return new StaticInit.DoubleInit(cd.Value);
        }

        ulong value = constant switch {
            Const.ConstInt constInt => (ulong)constInt.Value,
            Const.ConstLong constLong => (ulong)constLong.Value,
            Const.ConstUInt constUInt => (ulong)constUInt.Value,
            Const.ConstULong constULong => (ulong)constULong.Value,
            Const.ConstDouble constDouble => (ulong)constDouble.Value,
            _ => throw new NotImplementedException()
        };
        return target switch {
            Type.Int => new StaticInit.IntInit((int)value),
            Type.Long => new StaticInit.LongInit((long)value),
            Type.UInt => new StaticInit.UIntInit((uint)value),
            Type.ULong => new StaticInit.ULongInit((ulong)value),
            Type.Double => new StaticInit.DoubleInit((double)value),
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
        return type switch {
            Type.Int or Type.UInt => 4,
            Type.Long or Type.ULong => 8,
             _ => throw new NotImplementedException()
        };
    }

    public static bool IsSignedType(Type type)
    {
        return type switch {
            Type.Int or Type.Long => true,
            Type.UInt or Type.ULong => false,
             _ => throw new NotImplementedException()
        };
    }

    public static Type GetType(Expression expression)
    {
        return expression switch {
            Expression.Constant exp => exp.Type,
            Expression.Variable exp => exp.Type,
            Expression.Unary exp => exp.Type,
            Expression.Binary exp => exp.Type,
            Expression.Assignment exp => exp.Type,
            Expression.Conditional exp => exp.Type,
            Expression.FunctionCall exp => exp.Type,
            Expression.Cast exp => exp.Type,
            _ => throw new NotImplementedException()
        };
    }
}