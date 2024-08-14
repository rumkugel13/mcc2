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
            if (prevFunType is not Type.FunctionType funcA || funcA.Parameters.Count != funType.Parameters.Count)
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
        if (variableDeclaration.Initializer is Expression.ConstantExpression constant)
            initialValue = new InitialValue.Initial(ConvertConstant(variableDeclaration.VariableType, constant.Value));
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
            if (variableDeclaration.Initializer is Expression.ConstantExpression constant)
                initialValue = new InitialValue.Initial(ConvertConstant(variableDeclaration.VariableType, constant.Value));
            else if (variableDeclaration.Initializer == null)
                initialValue = new InitialValue.Initial(variableDeclaration.VariableType switch {
                    Type.Int => new StaticInit.IntInit(0),
                    Type.Long => new StaticInit.LongInit(0),
                    _ => throw new NotImplementedException()
                });
            else
                throw new Exception("Type Error: Non-constant initializer on local static variable");
            symbolTable[variableDeclaration.Identifier] = new SymbolEntry() { Type = variableDeclaration.VariableType, IdentifierAttributes = new IdentifierAttributes.Static(initialValue, false) };
        }
        else
        {
            symbolTable[variableDeclaration.Identifier] = new SymbolEntry() { Type = variableDeclaration.VariableType, IdentifierAttributes = new IdentifierAttributes.Local() };
            if (variableDeclaration.Initializer != null)
                return new Declaration.VariableDeclaration(variableDeclaration.Identifier, TypeCheckExpression(variableDeclaration.Initializer, symbolTable), variableDeclaration.VariableType, variableDeclaration.StorageClass);
        }
        return variableDeclaration;
    }

    private Expression TypeCheckExpression(Expression expression, Dictionary<string, SymbolEntry> symbolTable)
    {
        switch (expression)
        {
            case Expression.AssignmentExpression assignmentExpression:
                var typedLeft = TypeCheckExpression(assignmentExpression.ExpressionLeft, symbolTable);
                var typedRight = TypeCheckExpression(assignmentExpression.ExpressionRight, symbolTable);
                var leftType = GetType(typedLeft);
                var convertedRight = ConvertTo(typedRight, leftType);
                return new Expression.AssignmentExpression(typedLeft, convertedRight, leftType);
            case Expression.VariableExpression variableExpression:
                var varType = symbolTable[variableExpression.Identifier].Type;
                if (varType is Type.FunctionType)
                    throw new Exception("Type Error: Function name used as variable");
                return new Expression.VariableExpression(variableExpression.Identifier, varType);
            case Expression.UnaryExpression unaryExpression:
                var unaryInner = TypeCheckExpression(unaryExpression.Expression, symbolTable);
                return new Expression.UnaryExpression(unaryExpression.Operator, unaryInner, unaryExpression.Operator switch {
                    Expression.UnaryOperator.Not => new Type.Int(),
                    _ => GetType(unaryInner)
                });
            case Expression.BinaryExpression binaryExpression:
                var typedE1 = TypeCheckExpression(binaryExpression.ExpressionLeft, symbolTable);
                var typedE2 = TypeCheckExpression(binaryExpression.ExpressionRight, symbolTable);
                if (binaryExpression.Operator is Expression.BinaryOperator.And or Expression.BinaryOperator.Or)
                {
                    return new Expression.BinaryExpression(binaryExpression.Operator, typedE1, typedE2, new Type.Int());
                }
                var t1 = GetType(typedE1);
                var t2 = GetType(typedE2);
                var commonType = GetCommonType(t1, t2);
                var convertedE1 = ConvertTo(typedE1, commonType);
                var convertedE2 = ConvertTo(typedE2, commonType);
                return new Expression.BinaryExpression(binaryExpression.Operator, convertedE1, convertedE2, binaryExpression.Operator switch {
                    Expression.BinaryOperator.Add or 
                    Expression.BinaryOperator.Subtract or 
                    Expression.BinaryOperator.Multiply or 
                    Expression.BinaryOperator.Divide or 
                    Expression.BinaryOperator.Remainder =>
                        commonType,
                    _ => new Type.Int() // note: comparison results in int
                });
            case Expression.ConstantExpression constantExpression:
                return new Expression.ConstantExpression(constantExpression.Value, constantExpression.Value switch {
                    Const.ConstInt => new Type.Int(),
                    Const.ConstLong => new Type.Long(),
                    _ => throw new NotImplementedException()
                });
            case Expression.ConditionalExpression conditionalExpression:
                var typedCond = TypeCheckExpression(conditionalExpression.Condition, symbolTable);
                var typedThen = TypeCheckExpression(conditionalExpression.Then, symbolTable);
                var typedElse = TypeCheckExpression(conditionalExpression.Else, symbolTable);
                var typeThen = GetType(typedThen);
                var typeElse = GetType(typedElse);
                var common = GetCommonType(typeThen, typeElse);
                var convertedThen = ConvertTo(typedThen, common);
                var convertedElse = ConvertTo(typedElse, common);
                return new Expression.ConditionalExpression(typedCond, convertedThen, convertedElse, common);
            case Expression.FunctionCallExpression functionCallExpression:
                var funType = symbolTable[functionCallExpression.Identifier].Type;
                if (funType is not Type.FunctionType)
                    throw new Exception("Type Error: Variable used as function name");

                if (funType is Type.FunctionType functionType && functionType.Parameters.Count != functionCallExpression.Arguments.Count)
                    throw new Exception("Type Error: Function called with the wrong number of arguments");

                List<Expression> convertedArgs = [];
                for (int i = 0; i < functionCallExpression.Arguments.Count; i++)
                {
                    var typedArg = TypeCheckExpression(functionCallExpression.Arguments[i], symbolTable);
                    convertedArgs.Add(ConvertTo(typedArg, ((Type.FunctionType)funType).Parameters[i]));
                }
                return new Expression.FunctionCallExpression(functionCallExpression.Identifier, convertedArgs, ((Type.FunctionType)funType).Return);
            case Expression.CastExpression castExpression:
                var typedInner = TypeCheckExpression(castExpression.Expression, symbolTable);
                return new Expression.CastExpression(castExpression.TargetType, typedInner, castExpression.TargetType);
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

    private StaticInit ConvertConstant(Type target, Const constant)
    {
        if (target is Type.Int && constant is Const.ConstLong constLong)
            return new StaticInit.IntInit((int)constLong.Value);
        else if (target is Type.Long && constant is Const.ConstInt constInt)
            return new StaticInit.LongInit((long)constInt.Value);
        else return constant switch {
            Const.ConstInt val => new StaticInit.IntInit(val.Value),
            Const.ConstLong val => new StaticInit.LongInit(val.Value),
            _ => throw new NotImplementedException()
        };
    }

    private Expression ConvertTo(Expression expression, Type type)
    {
        if (GetType(expression) == type)
            return expression;
        
        return new Expression.CastExpression(type, expression, type);
    }

    private Type GetCommonType(Type one, Type two)
    {
        if (one == two)
            return one;
        else
            return new Type.Long();
    }

    public static Type? GetType(Expression expression)
    {
        return expression switch {
            Expression.ConstantExpression exp => exp.Type,
            Expression.VariableExpression exp => exp.Type,
            Expression.UnaryExpression exp => exp.Type,
            Expression.BinaryExpression exp => exp.Type,
            Expression.AssignmentExpression exp => exp.Type,
            Expression.ConditionalExpression exp => exp.Type,
            Expression.FunctionCallExpression exp => exp.Type,
            Expression.CastExpression exp => exp.Type,
            _ => throw new NotImplementedException()
        };
    }
}