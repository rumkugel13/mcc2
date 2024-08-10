using mcc2.AST;

namespace mcc2;

public class SemanticAnalyzer
{
    private int varCounter;
    private int loopCounter;

    private struct MapEntry
    {
        public string NewName;
        public bool FromCurrentBlock;
    }

    public void Analyze(ASTProgram program)
    {
        Dictionary<string, MapEntry> variableMap = [];
        foreach (var fun in program.FunctionDeclarations)
            if (fun.Body != null)
            {
                ResolveBlock(fun.Body, variableMap);
                LabelBlock(fun.Body, null);
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

    private void ResolveBlock(Block block, Dictionary<string, MapEntry> variableMap)
    {
        foreach (var item in block.BlockItems)
        {
            if (item is VariableDeclaration declaration)
            {
                ResolveDeclaration(declaration, variableMap);
            }
            else if (item is Statement statement)
            {
                ResolveStatement(statement, variableMap);
            }
        }
    }

    private void ResolveStatement(Statement statement, Dictionary<string, MapEntry> variableMap)
    {
        switch (statement)
        {
            case ReturnStatement ret:
                ResolveExpression(ret.Expression, variableMap);
                break;
            case ExpressionStatement expressionStatement:
                ResolveExpression(expressionStatement.Expression, variableMap);
                break;
            case NullStatement:
                break;
            case IfStatement ifStatement:
                ResolveExpression(ifStatement.Condition, variableMap);
                ResolveStatement(ifStatement.Then, variableMap);
                if (ifStatement.Else != null)
                    ResolveStatement(ifStatement.Else, variableMap);
                break;
            case CompoundStatement compoundStatement:
                {
                    var newVarMap = CopyVarMap(variableMap);
                    ResolveBlock(compoundStatement.Block, newVarMap);
                    break;
                }
            case BreakStatement:
                break;
            case ContinueStatement:
                break;
            case WhileStatement whileStatement:
                ResolveExpression(whileStatement.Condition, variableMap);
                ResolveStatement(whileStatement.Body, variableMap);
                break;
            case DoWhileStatement doWhileStatement:
                ResolveStatement(doWhileStatement.Body, variableMap);
                ResolveExpression(doWhileStatement.Condition, variableMap);
                break;
            case ForStatement forStatement:
                {
                    var newVarMap = CopyVarMap(variableMap);
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

    private void ResolveOptionalExpression(Expression? expression, Dictionary<string, MapEntry> variableMap)
    {
        if (expression != null)
            ResolveExpression(expression, variableMap);
    }

    private void ResolveForInit(ForInit init, Dictionary<string, MapEntry> variableMap)
    {
        switch (init)
        {
            case InitExpression initExpression:
                ResolveOptionalExpression(initExpression.Expression, variableMap);
                break;
            case InitDeclaration initDeclaration:
                ResolveDeclaration(initDeclaration.Declaration, variableMap);
                break;
        }
    }

    private Dictionary<string, MapEntry> CopyVarMap(Dictionary<string, MapEntry> variableMap)
    {
        Dictionary<string, MapEntry> newMap = [];
        foreach (var item in variableMap)
        {
            newMap.Add(item.Key, new MapEntry() { NewName = item.Value.NewName, FromCurrentBlock = false });
        }
        return newMap;
    }

    private void ResolveExpression(Expression expression, Dictionary<string, MapEntry> variableMap)
    {
        switch (expression)
        {
            case AssignmentExpression assignmentExpression:
                if (assignmentExpression.ExpressionLeft is not VariableExpression)
                    throw new Exception("Semantic Error: Invalid lvalue");

                ResolveExpression(assignmentExpression.ExpressionLeft, variableMap);
                ResolveExpression(assignmentExpression.ExpressionRight, variableMap);
                break;
            case VariableExpression variableExpression:
                if (variableMap.TryGetValue(variableExpression.Identifier, out MapEntry newVariable))
                    variableExpression.Identifier = newVariable.NewName;
                else
                    throw new Exception("Semantic Error: Undeclared variable");
                break;
            case UnaryExpression unaryExpression:
                ResolveExpression(unaryExpression.Expression, variableMap);
                break;
            case BinaryExpression binaryExpression:
                ResolveExpression(binaryExpression.ExpressionLeft, variableMap);
                ResolveExpression(binaryExpression.ExpressionRight, variableMap);
                break;
            case ConstantExpression:
                break;
            case ConditionalExpression conditionalExpression:
                ResolveExpression(conditionalExpression.Condition, variableMap);
                ResolveExpression(conditionalExpression.Then, variableMap);
                ResolveExpression(conditionalExpression.Else, variableMap);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void ResolveDeclaration(VariableDeclaration declaration, Dictionary<string, MapEntry> variableMap)
    {
        if (variableMap.TryGetValue(declaration.Identifier, out MapEntry newVariable) && newVariable.FromCurrentBlock)
            throw new Exception("Semantic Error: Duplicate variable declaration");

        var uniqueName = MakeTemporary(declaration.Identifier);
        variableMap[declaration.Identifier] = new MapEntry() { NewName = uniqueName, FromCurrentBlock = true };
        if (declaration.Initializer != null)
        {
            ResolveExpression(declaration.Initializer, variableMap);
        }
        declaration.Identifier = uniqueName;
    }

    private string MakeTemporary(string varName)
    {
        return $"var.{varName}.{varCounter++}";
    }
}