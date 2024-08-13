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

        new TypeChecker().Check(program, symbolTable);

        for (int i = 0; i < program.Declarations.Count; i++)
        {
            Declaration? decl = program.Declarations[i];
            if (decl is Declaration.FunctionDeclaration fun)
                program.Declarations[i] = LabelFunction(fun, null);
        }
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