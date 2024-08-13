using mcc2.AST;

namespace mcc2;

public class SemanticAnalyzer
{
    private int varCounter;
    private int loopCounter;

    private struct MapEntry
    {
        public string NewName;
        public bool FromCurrentScope, HasLinkage;
    }

    public struct SymbolEntry
    {
        public Type Type;
        public IdentifierAttributes IdentifierAttributes;
    }

    public void Analyze(ASTProgram program, Dictionary<string, SymbolEntry> symbolTable)
    {
        Dictionary<string, MapEntry> identifierMap = [];
        for (int i = 0; i < program.Declarations.Count; i++)
        {
            Declaration? decl = program.Declarations[i];
            if (decl is Declaration.FunctionDeclaration fun)
                program.Declarations[i] = ResolveFunctionDeclaration(fun, identifierMap);
            else if (decl is Declaration.VariableDeclaration var)
                ResolveFileScopeVariableDeclaration(var, identifierMap);
        }

        foreach (var decl in program.Declarations)
        {
            if (decl is Declaration.FunctionDeclaration fun)
                TypeCheckFunctionDeclaration(fun, symbolTable);
            else if (decl is Declaration.VariableDeclaration var)
                TypeCheckFileScopeVariableDeclaration(var, symbolTable);
        }

        for (int i = 0; i < program.Declarations.Count; i++)
        {
            Declaration? decl = program.Declarations[i];
            if (decl is Declaration.FunctionDeclaration fun)
                program.Declarations[i] = LabelFunction(fun, null);
        }
    }

    private void TypeCheckFunctionDeclaration(Declaration.FunctionDeclaration functionDeclaration, Dictionary<string, SymbolEntry> symbolTable)
    {
        Type.FunctionType funType = new Type.FunctionType(functionDeclaration.Parameters.Count);
        bool hasBody = functionDeclaration.Body != null;
        bool alreadyDefined = false;
        bool global = functionDeclaration.StorageClass != Declaration.StorageClasses.Static;

        if (symbolTable.TryGetValue(functionDeclaration.Identifier, out SymbolEntry prevEntry))
        {
            var attributes = (IdentifierAttributes.Function)prevEntry.IdentifierAttributes;
            // note: check correct type and number of parameters
            if (prevEntry.Type is not Type.FunctionType funcA || funcA.ParameterCount != funType.ParameterCount)
                throw new Exception("Type Error: Incompatible function declarations");

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
            foreach (var param in functionDeclaration.Parameters)
                symbolTable.Add(param, new SymbolEntry() { Type = new Type.Int() });
            TypeCheckBlock(functionDeclaration.Body, symbolTable);
        }
    }

    private void TypeCheckBlock(Block block, Dictionary<string, SymbolEntry> symbolTable)
    {
        foreach (var item in block.BlockItems)
        {
            if (item is Declaration.VariableDeclaration declaration)
            {
                TypeCheckLocalVariableDeclaration(declaration, symbolTable);
            }
            else if (item is Declaration.FunctionDeclaration functionDeclaration)
            {
                if (functionDeclaration.StorageClass != Declaration.StorageClasses.Static)
                    TypeCheckFunctionDeclaration(functionDeclaration, symbolTable);
                else
                    throw new Exception("Type Error: StorageClass static used in block function declaration");
            }
            else if (item is Statement statement)
            {
                TypeCheckStatement(statement, symbolTable);
            }
        }
    }

    private void TypeCheckFileScopeVariableDeclaration(Declaration.VariableDeclaration variableDeclaration, Dictionary<string, SymbolEntry> symbolTable)
    {
        InitialValue initialValue;
        if (variableDeclaration.Initializer is Expression.ConstantExpression constant)
            initialValue = new InitialValue.Initial(constant.Value);
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
            if (prevEntry.Type is not Type.Int)
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

        symbolTable[variableDeclaration.Identifier] = new SymbolEntry() { Type = new Type.Int(), IdentifierAttributes = new IdentifierAttributes.Static(initialValue, global) };
    }

    private void TypeCheckLocalVariableDeclaration(Declaration.VariableDeclaration variableDeclaration, Dictionary<string, SymbolEntry> symbolTable)
    {
        if (variableDeclaration.StorageClass == Declaration.StorageClasses.Extern)
        {
            if (variableDeclaration.Initializer != null)
                throw new Exception("Type Error: Initializer on local extern variable declaration");
            if (symbolTable.TryGetValue(variableDeclaration.Identifier, out SymbolEntry prevEntry))
            {
                if (prevEntry.Type is not Type.Int)
                    throw new Exception("Type Error: Function redeclared as variable");
            }
            else
                symbolTable.Add(variableDeclaration.Identifier, new SymbolEntry() { Type = new Type.Int(), IdentifierAttributes = new IdentifierAttributes.Static(new InitialValue.NoInitializer(), true) });
        }
        else if (variableDeclaration.StorageClass == Declaration.StorageClasses.Static)
        {
            InitialValue initialValue;
            if (variableDeclaration.Initializer is Expression.ConstantExpression constant)
                initialValue = new InitialValue.Initial(constant.Value);
            else if (variableDeclaration.Initializer == null)
                initialValue = new InitialValue.Initial(0);
            else
                throw new Exception("Type Error: Non-constant initializer on local static variable");
            symbolTable[variableDeclaration.Identifier] = new SymbolEntry() { Type = new Type.Int(), IdentifierAttributes = new IdentifierAttributes.Static(initialValue, false) };
        }
        else
        {
            symbolTable[variableDeclaration.Identifier] = new SymbolEntry() { Type = new Type.Int(), IdentifierAttributes = new IdentifierAttributes.Local() };
            if (variableDeclaration.Initializer != null)
                TypeCheckExpression(variableDeclaration.Initializer, symbolTable);
        }
    }

    private void TypeCheckExpression(Expression expression, Dictionary<string, SymbolEntry> symbolTable)
    {
        switch (expression)
        {
            case Expression.AssignmentExpression assignmentExpression:
                TypeCheckExpression(assignmentExpression.ExpressionLeft, symbolTable);
                TypeCheckExpression(assignmentExpression.ExpressionRight, symbolTable);
                break;
            case Expression.VariableExpression variableExpression:
                if (symbolTable[variableExpression.Identifier].Type is not Type.Int)
                    throw new Exception("Type Error: Function name used as variable");
                break;
            case Expression.UnaryExpression unaryExpression:
                TypeCheckExpression(unaryExpression.Expression, symbolTable);
                break;
            case Expression.BinaryExpression binaryExpression:
                TypeCheckExpression(binaryExpression.ExpressionLeft, symbolTable);
                TypeCheckExpression(binaryExpression.ExpressionRight, symbolTable);
                break;
            case Expression.ConstantExpression:
                break;
            case Expression.ConditionalExpression conditionalExpression:
                TypeCheckExpression(conditionalExpression.Condition, symbolTable);
                TypeCheckExpression(conditionalExpression.Then, symbolTable);
                TypeCheckExpression(conditionalExpression.Else, symbolTable);
                break;
            case Expression.FunctionCallExpression functionCallExpression:
                var funType = symbolTable[functionCallExpression.Identifier].Type;
                if (funType is Type.Int)
                    throw new Exception("Type Error: Variable used as function name");

                if (funType is Type.FunctionType functionType && functionType.ParameterCount != functionCallExpression.Arguments.Count)
                    throw new Exception("Type Error: Function called with the wrong number of arguments");

                foreach (var arg in functionCallExpression.Arguments)
                    TypeCheckExpression(arg, symbolTable);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void TypeCheckStatement(Statement statement, Dictionary<string, SymbolEntry> symbolTable)
    {
        switch (statement)
        {
            case Statement.ReturnStatement ret:
                TypeCheckExpression(ret.Expression, symbolTable);
                break;
            case Statement.ExpressionStatement expressionStatement:
                TypeCheckExpression(expressionStatement.Expression, symbolTable);
                break;
            case Statement.NullStatement:
                break;
            case Statement.IfStatement ifStatement:
                TypeCheckExpression(ifStatement.Condition, symbolTable);
                TypeCheckStatement(ifStatement.Then, symbolTable);
                if (ifStatement.Else != null)
                    TypeCheckStatement(ifStatement.Else, symbolTable);
                break;
            case Statement.CompoundStatement compoundStatement:
                TypeCheckBlock(compoundStatement.Block, symbolTable);
                break;
            case Statement.BreakStatement:
                break;
            case Statement.ContinueStatement:
                break;
            case Statement.WhileStatement whileStatement:
                TypeCheckExpression(whileStatement.Condition, symbolTable);
                TypeCheckStatement(whileStatement.Body, symbolTable);
                break;
            case Statement.DoWhileStatement doWhileStatement:
                TypeCheckStatement(doWhileStatement.Body, symbolTable);
                TypeCheckExpression(doWhileStatement.Condition, symbolTable);
                break;
            case Statement.ForStatement forStatement:
                {
                    TypeCheckForInit(forStatement.Init, symbolTable);
                    TypeCheckOptionalExpression(forStatement.Condition, symbolTable);
                    TypeCheckOptionalExpression(forStatement.Post, symbolTable);
                    TypeCheckStatement(forStatement.Body, symbolTable);
                    break;
                }
            default:
                throw new NotImplementedException();
        }
    }

    private void TypeCheckForInit(ForInit init, Dictionary<string, SymbolEntry> symbolTable)
    {
        switch (init)
        {
            case ForInit.InitExpression initExpression:
                TypeCheckOptionalExpression(initExpression.Expression, symbolTable);
                break;
            case ForInit.InitDeclaration initDeclaration:
                if (initDeclaration.Declaration.StorageClass == null)
                    TypeCheckLocalVariableDeclaration(initDeclaration.Declaration, symbolTable);
                else
                    throw new Exception("Type Error: StorageClass used in for loop init");
                break;
        }
    }

    private void TypeCheckOptionalExpression(Expression? expression, Dictionary<string, SymbolEntry> symbolTable)
    {
        if (expression != null)
            TypeCheckExpression(expression, symbolTable);
    }

    private Declaration.FunctionDeclaration LabelFunction(Declaration.FunctionDeclaration functionDeclaration, string? currentLabel)
    {
        if (functionDeclaration.Body != null)
        {
            return new Declaration.FunctionDeclaration(functionDeclaration.Identifier,
                functionDeclaration.Parameters,
                LabelBlock(functionDeclaration.Body, null),
                functionDeclaration.StorageClass);
        }

        return functionDeclaration;
    }

    private Block LabelBlock(Block block, string? currentLabel)
    {
        List<BlockItem> newItems = [];
        foreach (var item in block.BlockItems)
        {
            var newItem = item;
            if (newItem is Statement statement)
                newItem = LabelStatement(statement, currentLabel);
            newItems.Add(newItem);
        }
        return new Block(newItems);
    }

    private Statement LabelStatement(Statement statement, string? currentLabel)
    {
        switch (statement)
        {
            case Statement.ReturnStatement returnStatement:
                return returnStatement;
            case Statement.ExpressionStatement expressionStatement:
                return expressionStatement;
            case Statement.NullStatement nullStatement:
                return nullStatement;
            case Statement.IfStatement ifStatement:
                {
                    var then = LabelStatement(ifStatement.Then, currentLabel);
                    var el = ifStatement.Else;
                    if (el != null)
                        el = LabelStatement(el, currentLabel);
                    return new Statement.IfStatement(ifStatement.Condition, then, el);
                }
            case Statement.CompoundStatement compoundStatement:
                return new Statement.CompoundStatement(LabelBlock(compoundStatement.Block, currentLabel));
            case Statement.BreakStatement breakStatement:
                if (currentLabel == null)
                    throw new Exception("Semantic Error: Break statement outside of loop");
                return new Statement.BreakStatement(currentLabel);
            case Statement.ContinueStatement continueStatement:
                if (currentLabel == null)
                    throw new Exception("Semantic Error: Continue statement outside of loop");
                return new Statement.ContinueStatement(currentLabel);
            case Statement.WhileStatement whileStatement:
                {
                    var newLabel = MakeLabel();
                    var body = LabelStatement(whileStatement.Body, newLabel);
                    return new Statement.WhileStatement(whileStatement.Condition, body, newLabel);
                }
            case Statement.DoWhileStatement doWhileStatement:
                {
                    var newLabel = MakeLabel();
                    var body = LabelStatement(doWhileStatement.Body, newLabel);
                    return new Statement.DoWhileStatement(body, doWhileStatement.Condition, newLabel);
                }
            case Statement.ForStatement forStatement:
                {
                    var newLabel = MakeLabel();
                    var body = LabelStatement(forStatement.Body, newLabel);
                    return new Statement.ForStatement(forStatement.Init, forStatement.Condition, forStatement.Post, body, newLabel);
                }
            default:
                throw new NotImplementedException();
        }
    }

    private string MakeLabel()
    {
        return $"loop.{loopCounter++}";
    }

    private Declaration.FunctionDeclaration ResolveFunctionDeclaration(Declaration.FunctionDeclaration functionDeclaration, Dictionary<string, MapEntry> identifierMap)
    {
        if (identifierMap.TryGetValue(functionDeclaration.Identifier, out MapEntry prevEntry))
        {
            if (prevEntry.FromCurrentScope && !prevEntry.HasLinkage)
                throw new Exception("Semantic Error: Duplicate declaration");
        }

        identifierMap[functionDeclaration.Identifier] = new MapEntry() { NewName = functionDeclaration.Identifier, FromCurrentScope = true, HasLinkage = true };

        var innerMap = CopyIdentifierMap(identifierMap);
        List<string> newParams = [];
        foreach (var param in functionDeclaration.Parameters)
        {
            newParams.Add(ResolveParameter(param, innerMap));
        }

        Block? newBody = null;
        if (functionDeclaration.Body != null)
        {
            newBody = ResolveBlock(functionDeclaration.Body, innerMap);
        }
        return new Declaration.FunctionDeclaration(functionDeclaration.Identifier, newParams, newBody, functionDeclaration.StorageClass);
    }

    private string ResolveParameter(string parameter, Dictionary<string, MapEntry> identifierMap)
    {
        if (identifierMap.TryGetValue(parameter, out MapEntry newVariable) && newVariable.FromCurrentScope)
            throw new Exception("Semantic Error: Duplicate parameter declaration");

        var uniqueName = MakeTemporary(parameter);
        identifierMap[parameter] = new MapEntry() { NewName = uniqueName, FromCurrentScope = true };
        return uniqueName;
    }

    private void ResolveFileScopeVariableDeclaration(Declaration.VariableDeclaration var, Dictionary<string, MapEntry> identifierMap)
    {
        identifierMap[var.Identifier] = new MapEntry() { NewName = var.Identifier, FromCurrentScope = true, HasLinkage = true };
    }

    private Block ResolveBlock(Block block, Dictionary<string, MapEntry> identifierMap)
    {
        List<BlockItem> newItems = [];
        foreach (var item in block.BlockItems)
        {
            if (item is Declaration.VariableDeclaration declaration)
            {
                newItems.Add(ResolveVariableDeclaration(declaration, identifierMap));
            }
            else if (item is Declaration.FunctionDeclaration functionDeclaration)
            {
                if (functionDeclaration.Body != null)
                    throw new Exception("Semantic Error: Local function definition");

                newItems.Add(ResolveFunctionDeclaration(functionDeclaration, identifierMap));
            }
            else if (item is Statement statement)
            {
                newItems.Add(ResolveStatement(statement, identifierMap));
            }
        }
        return new Block(newItems);
    }

    private Statement ResolveStatement(Statement statement, Dictionary<string, MapEntry> identifierMap)
    {
        switch (statement)
        {
            case Statement.ReturnStatement ret:
                return new Statement.ReturnStatement(ResolveExpression(ret.Expression, identifierMap));
            case Statement.ExpressionStatement expressionStatement:
                return new Statement.ExpressionStatement(ResolveExpression(expressionStatement.Expression, identifierMap));
            case Statement.NullStatement nullStatement:
                return nullStatement;
            case Statement.IfStatement ifStatement:
                {
                    var cond = ResolveExpression(ifStatement.Condition, identifierMap);
                    var then = ResolveStatement(ifStatement.Then, identifierMap);
                    Statement? el = ifStatement.Else;
                    if (el != null)
                        el = ResolveStatement(el, identifierMap);
                    return new Statement.IfStatement(cond, then, el);
                }
            case Statement.CompoundStatement compoundStatement:
                {
                    var newIdentifierMap = CopyIdentifierMap(identifierMap);
                    return new Statement.CompoundStatement(ResolveBlock(compoundStatement.Block, newIdentifierMap));
                }
            case Statement.BreakStatement breakStatement:
                return breakStatement;
            case Statement.ContinueStatement continueStatement:
                return continueStatement;
            case Statement.WhileStatement whileStatement:
                {
                    var cond = ResolveExpression(whileStatement.Condition, identifierMap);
                    var body = ResolveStatement(whileStatement.Body, identifierMap);
                    return new Statement.WhileStatement(cond, body, whileStatement.Label);
                }
            case Statement.DoWhileStatement doWhileStatement:
                {
                    var body = ResolveStatement(doWhileStatement.Body, identifierMap);
                    var cond = ResolveExpression(doWhileStatement.Condition, identifierMap);
                    return new Statement.DoWhileStatement(body, cond, doWhileStatement.Label);
                }
            case Statement.ForStatement forStatement:
                {
                    var newVarMap = CopyIdentifierMap(identifierMap);
                    var init = ResolveForInit(forStatement.Init, newVarMap);
                    var cond = ResolveOptionalExpression(forStatement.Condition, newVarMap);
                    var post = ResolveOptionalExpression(forStatement.Post, newVarMap);
                    var body = ResolveStatement(forStatement.Body, newVarMap);
                    return new Statement.ForStatement(init, cond, post, body, forStatement.Label);
                }
            default:
                throw new NotImplementedException();
        }
    }

    private Expression? ResolveOptionalExpression(Expression? expression, Dictionary<string, MapEntry> identifierMap)
    {
        if (expression != null)
            return ResolveExpression(expression, identifierMap);
        else
            return null;
    }

    private ForInit ResolveForInit(ForInit init, Dictionary<string, MapEntry> identifierMap)
    {
        switch (init)
        {
            case ForInit.InitExpression initExpression:
                return new ForInit.InitExpression(ResolveOptionalExpression(initExpression.Expression, identifierMap));
            case ForInit.InitDeclaration initDeclaration:
                return new ForInit.InitDeclaration(ResolveVariableDeclaration(initDeclaration.Declaration, identifierMap));
            default:
                throw new NotImplementedException();
        }
    }

    private Dictionary<string, MapEntry> CopyIdentifierMap(Dictionary<string, MapEntry> identifierMap)
    {
        Dictionary<string, MapEntry> newMap = [];
        foreach (var item in identifierMap)
        {
            newMap.Add(item.Key, new MapEntry() { NewName = item.Value.NewName, FromCurrentScope = false });
        }
        return newMap;
    }

    private Expression ResolveExpression(Expression expression, Dictionary<string, MapEntry> identifierMap)
    {
        switch (expression)
        {
            case Expression.AssignmentExpression assignmentExpression:
                if (assignmentExpression.ExpressionLeft is not Expression.VariableExpression)
                    throw new Exception("Semantic Error: Invalid lvalue");

                {
                    var left = ResolveExpression(assignmentExpression.ExpressionLeft, identifierMap);
                    var right = ResolveExpression(assignmentExpression.ExpressionRight, identifierMap);
                    return new Expression.AssignmentExpression(left, right);
                }
            case Expression.VariableExpression variableExpression:
                if (identifierMap.TryGetValue(variableExpression.Identifier, out MapEntry newVariable))
                    return new Expression.VariableExpression(newVariable.NewName);
                else
                    throw new Exception("Semantic Error: Undeclared variable");
            case Expression.UnaryExpression unaryExpression:
                {
                    var exp = ResolveExpression(unaryExpression.Expression, identifierMap);
                    return new Expression.UnaryExpression(unaryExpression.Operator, exp);
                }
            case Expression.BinaryExpression binaryExpression:
                {
                    var left = ResolveExpression(binaryExpression.ExpressionLeft, identifierMap);
                    var right = ResolveExpression(binaryExpression.ExpressionRight, identifierMap);
                    return new Expression.BinaryExpression(binaryExpression.Operator, left, right);
                }
            case Expression.ConstantExpression constantExpression:
                return constantExpression;
            case Expression.ConditionalExpression conditionalExpression:
                {
                    var cond = ResolveExpression(conditionalExpression.Condition, identifierMap);
                    var then = ResolveExpression(conditionalExpression.Then, identifierMap);
                    var el = ResolveExpression(conditionalExpression.Else, identifierMap);
                    return new Expression.ConditionalExpression(cond, then, el);
                }
            case Expression.FunctionCallExpression functionCallExpression:
                if (identifierMap.TryGetValue(functionCallExpression.Identifier, out MapEntry entry))
                {
                    var newFunName = entry.NewName;
                    List<Expression> newArgs = [];
                    foreach (var arg in functionCallExpression.Arguments)
                        newArgs.Add(ResolveExpression(arg, identifierMap));
                    return new Expression.FunctionCallExpression(newFunName, newArgs);
                }
                else
                    throw new Exception("Semantic Error: Undeclared function");
            default:
                throw new NotImplementedException();
        }
    }

    private Declaration.VariableDeclaration ResolveVariableDeclaration(Declaration.VariableDeclaration declaration, Dictionary<string, MapEntry> identifierMap)
    {
        if (identifierMap.TryGetValue(declaration.Identifier, out MapEntry prevEntry))
        {
            if (prevEntry.FromCurrentScope)
                if (!(prevEntry.HasLinkage && declaration.StorageClass == Declaration.StorageClasses.Extern))
                    throw new Exception("Semantic Error: Conflicting local declarations");
        }

        if (declaration.StorageClass == Declaration.StorageClasses.Extern)
        {
            identifierMap[declaration.Identifier] = new MapEntry() { NewName = declaration.Identifier, FromCurrentScope = true, HasLinkage = true };
            return declaration;
        }

        var uniqueName = MakeTemporary(declaration.Identifier);
        identifierMap[declaration.Identifier] = new MapEntry() { NewName = uniqueName, FromCurrentScope = true, HasLinkage = false };
        var init = declaration.Initializer;
        if (init != null)
            init = ResolveExpression(init, identifierMap);
        return new Declaration.VariableDeclaration(uniqueName, init, declaration.StorageClass);
    }

    private string MakeTemporary(string varName)
    {
        return $"var.{varName}.{varCounter++}";
    }
}