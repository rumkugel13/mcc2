using mcc2.AST;

namespace mcc2;

public class SemanticAnalyzer
{
    private int varCounter;

    private struct MapEntry
    {
        public string NewName;
        public bool FromCurrentBlock;
    }

    public void Analyze(ASTProgram program)
    {
        Dictionary<string, MapEntry> variableMap = [];
        ResolveBlock(program.Function.Body, variableMap);
    }

    private void ResolveBlock(Block block, Dictionary<string, MapEntry> variableMap)
    {
        foreach (var item in block.BlockItems)
        {
            if (item is Declaration declaration)
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
                var newVarMap = CopyVarMap(variableMap);
                ResolveBlock(compoundStatement.Block, newVarMap);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private Dictionary<string, MapEntry> CopyVarMap(Dictionary<string, MapEntry> variableMap)
    {
        Dictionary<string, MapEntry> newMap = [];
        foreach (var item in variableMap)
        {
            newMap.Add(item.Key, new MapEntry(){NewName = item.Value.NewName, FromCurrentBlock = false});
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

    private void ResolveDeclaration(Declaration declaration, Dictionary<string, MapEntry> variableMap)
    {
        if (variableMap.TryGetValue(declaration.Identifier, out MapEntry newVariable) && newVariable.FromCurrentBlock)
            throw new Exception("Semantic Error: Duplicate variable declaration");

        var uniqueName = MakeTemporary(declaration.Identifier);
        variableMap[declaration.Identifier] = new MapEntry(){NewName = uniqueName, FromCurrentBlock = true};
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