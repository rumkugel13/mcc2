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

    public void Analyze(ASTProgram program)
    {
        Dictionary<string, MapEntry> identifierMap = [];
        foreach (var fun in program.FunctionDeclarations)
        {
            ResolveFunctionDeclaration(fun, identifierMap);
        }
        foreach (var fun in program.FunctionDeclarations)
        {
            LabelFunction(fun, null);
        }
    }

    private void LabelFunction(FunctionDeclaration functionDeclaration, string? currentLabel)
    {
        if (functionDeclaration.Body != null)
        {
            LabelBlock(functionDeclaration.Body, null);
        }
    }

    private void LabelBlock(Block block, string? currentLabel)
    {
        foreach (var item in block.BlockItems)
        {
            if (item is Statement statement)
                LabelStatement(statement, currentLabel);
        }
    }

    private void LabelStatement(Statement statement, string? currentLabel)
    {
        switch (statement)
        {
            case ReturnStatement:
                break;
            case ExpressionStatement:
                break;
            case NullStatement:
                break;
            case IfStatement ifStatement:
                LabelStatement(ifStatement.Then, currentLabel);
                if (ifStatement.Else != null)
                    LabelStatement(ifStatement.Else, currentLabel);
                break;
            case CompoundStatement compoundStatement:
                LabelBlock(compoundStatement.Block, currentLabel);
                break;
            case BreakStatement breakStatement:
                if (currentLabel == null)
                    throw new Exception("Semantic Error: Break statement outside of loop");
                breakStatement.Label = currentLabel;
                break;
            case ContinueStatement continueStatement:
                if (currentLabel == null)
                    throw new Exception("Semantic Error: Continue statement outside of loop");
                continueStatement.Label = currentLabel;
                break;
            case WhileStatement whileStatement:
                {
                    var newLabel = MakeLabel();
                    LabelStatement(whileStatement.Body, newLabel);
                    whileStatement.Label = newLabel;
                    break;
                }
            case DoWhileStatement doWhileStatement:
                {
                    var newLabel = MakeLabel();
                    LabelStatement(doWhileStatement.Body, newLabel);
                    doWhileStatement.Label = newLabel;
                    break;
                }
            case ForStatement forStatement:
                {
                    var newLabel = MakeLabel();
                    LabelStatement(forStatement.Body, newLabel);
                    forStatement.Label = newLabel;
                    break;
                }
            default:
                throw new NotImplementedException();
        }
    }

    private string MakeLabel()
    {
        return $"loop.{loopCounter++}";
    }

    private void ResolveFunctionDeclaration(FunctionDeclaration functionDeclaration, Dictionary<string, MapEntry> identifierMap)
    {
        if (identifierMap.TryGetValue(functionDeclaration.Identifier, out MapEntry prevEntry))
        {
            if (prevEntry.FromCurrentScope && !prevEntry.HasLinkage)
                throw new Exception("Semantic Error: Duplicate declaration");
        }

        identifierMap[functionDeclaration.Identifier] = new MapEntry() { NewName = functionDeclaration.Identifier, FromCurrentScope = true, HasLinkage = true };

        var innerMap = CopyIdentifierMap(identifierMap);
        for (int i = 0; i < functionDeclaration.Parameters.Count; i++)
        {
            string? parameter = functionDeclaration.Parameters[i];
            ResolveParameter(ref parameter, innerMap);
             functionDeclaration.Parameters[i] = parameter;
        }

        if (functionDeclaration.Body != null)
        {
            ResolveBlock(functionDeclaration.Body, innerMap);
        }
    }

    private void ResolveParameter(ref string parameter, Dictionary<string, MapEntry> identifierMap)
    {
        if (identifierMap.TryGetValue(parameter, out MapEntry newVariable) && newVariable.FromCurrentScope)
            throw new Exception("Semantic Error: Duplicate parameter declaration");

        var uniqueName = MakeTemporary(parameter);
        identifierMap[parameter] = new MapEntry() { NewName = uniqueName, FromCurrentScope = true };
        parameter = uniqueName;
    }

    private void ResolveBlock(Block block, Dictionary<string, MapEntry> identifierMap)
    {
        foreach (var item in block.BlockItems)
        {
            if (item is VariableDeclaration declaration)
            {
                ResolveVariableDeclaration(declaration, identifierMap);
            }
            else if (item is FunctionDeclaration functionDeclaration)
            {
                if (functionDeclaration.Body != null)
                    throw new Exception("Semantic Error: Local function definition");

                ResolveFunctionDeclaration(functionDeclaration, identifierMap);
            }
            else if (item is Statement statement)
            {
                ResolveStatement(statement, identifierMap);
            }
        }
    }

    private void ResolveStatement(Statement statement, Dictionary<string, MapEntry> identifierMap)
    {
        switch (statement)
        {
            case ReturnStatement ret:
                ResolveExpression(ret.Expression, identifierMap);
                break;
            case ExpressionStatement expressionStatement:
                ResolveExpression(expressionStatement.Expression, identifierMap);
                break;
            case NullStatement:
                break;
            case IfStatement ifStatement:
                ResolveExpression(ifStatement.Condition, identifierMap);
                ResolveStatement(ifStatement.Then, identifierMap);
                if (ifStatement.Else != null)
                    ResolveStatement(ifStatement.Else, identifierMap);
                break;
            case CompoundStatement compoundStatement:
                {
                    var newIdentifierMap = CopyIdentifierMap(identifierMap);
                    ResolveBlock(compoundStatement.Block, newIdentifierMap);
                    break;
                }
            case BreakStatement:
                break;
            case ContinueStatement:
                break;
            case WhileStatement whileStatement:
                ResolveExpression(whileStatement.Condition, identifierMap);
                ResolveStatement(whileStatement.Body, identifierMap);
                break;
            case DoWhileStatement doWhileStatement:
                ResolveStatement(doWhileStatement.Body, identifierMap);
                ResolveExpression(doWhileStatement.Condition, identifierMap);
                break;
            case ForStatement forStatement:
                {
                    var newVarMap = CopyIdentifierMap(identifierMap);
                    ResolveForInit(forStatement.Init, newVarMap);
                    ResolveOptionalExpression(forStatement.Condition, newVarMap);
                    ResolveOptionalExpression(forStatement.Post, newVarMap);
                    ResolveStatement(forStatement.Body, newVarMap);
                    break;
                }
            default:
                throw new NotImplementedException();
        }
    }

    private void ResolveOptionalExpression(Expression? expression, Dictionary<string, MapEntry> identifierMap)
    {
        if (expression != null)
            ResolveExpression(expression, identifierMap);
    }

    private void ResolveForInit(ForInit init, Dictionary<string, MapEntry> identifierMap)
    {
        switch (init)
        {
            case InitExpression initExpression:
                ResolveOptionalExpression(initExpression.Expression, identifierMap);
                break;
            case InitDeclaration initDeclaration:
                ResolveVariableDeclaration(initDeclaration.Declaration, identifierMap);
                break;
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

    private void ResolveExpression(Expression expression, Dictionary<string, MapEntry> identifierMap)
    {
        switch (expression)
        {
            case AssignmentExpression assignmentExpression:
                if (assignmentExpression.ExpressionLeft is not VariableExpression)
                    throw new Exception("Semantic Error: Invalid lvalue");

                ResolveExpression(assignmentExpression.ExpressionLeft, identifierMap);
                ResolveExpression(assignmentExpression.ExpressionRight, identifierMap);
                break;
            case VariableExpression variableExpression:
                if (identifierMap.TryGetValue(variableExpression.Identifier, out MapEntry newVariable))
                    variableExpression.Identifier = newVariable.NewName;
                else
                    throw new Exception("Semantic Error: Undeclared variable");
                break;
            case UnaryExpression unaryExpression:
                ResolveExpression(unaryExpression.Expression, identifierMap);
                break;
            case BinaryExpression binaryExpression:
                ResolveExpression(binaryExpression.ExpressionLeft, identifierMap);
                ResolveExpression(binaryExpression.ExpressionRight, identifierMap);
                break;
            case ConstantExpression:
                break;
            case ConditionalExpression conditionalExpression:
                ResolveExpression(conditionalExpression.Condition, identifierMap);
                ResolveExpression(conditionalExpression.Then, identifierMap);
                ResolveExpression(conditionalExpression.Else, identifierMap);
                break;
            case FunctionCallExpression functionCallExpression:
                if (identifierMap.TryGetValue(functionCallExpression.Identifier, out MapEntry entry))
                {
                    functionCallExpression.Identifier = entry.NewName;
                    foreach (var arg in functionCallExpression.Arguments)
                        ResolveExpression(arg, identifierMap);
                }
                else
                    throw new Exception("Semantic Error: Undeclared function");
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void ResolveVariableDeclaration(VariableDeclaration declaration, Dictionary<string, MapEntry> identifierMap)
    {
        if (identifierMap.TryGetValue(declaration.Identifier, out MapEntry newVariable) && newVariable.FromCurrentScope)
            throw new Exception("Semantic Error: Duplicate variable declaration");

        var uniqueName = MakeTemporary(declaration.Identifier);
        identifierMap[declaration.Identifier] = new MapEntry() { NewName = uniqueName, FromCurrentScope = true, HasLinkage = false };
        if (declaration.Initializer != null)
        {
            ResolveExpression(declaration.Initializer, identifierMap);
        }
        declaration.Identifier = uniqueName;
    }

    private string MakeTemporary(string varName)
    {
        return $"var.{varName}.{varCounter++}";
    }
}