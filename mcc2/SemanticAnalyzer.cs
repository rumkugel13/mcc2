using mcc2.AST;

namespace mcc2;

using VarMap = Dictionary<string, string>;

public class SemanticAnalyzer
{
    private int varCounter;

    public void Analyze(ASTProgram program)
    {
        VarMap variableMap = [];

        foreach (var item in program.Function.Body.BlockItems)
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

    private void ResolveStatement(Statement statement, VarMap variableMap)
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
            default:
                throw new NotImplementedException();
        }
    }

    private void ResolveExpression(Expression expression, VarMap variableMap)
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
                if (variableMap.TryGetValue(variableExpression.Identifier, out string? newVariable))
                    variableExpression.Identifier = newVariable;
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

    private void ResolveDeclaration(Declaration declaration, VarMap variableMap)
    {
        if (variableMap.ContainsKey(declaration.Identifier))
            throw new Exception("Semantic Error: Duplicate variable declaration");

        var uniqueName = MakeTemporary(declaration.Identifier);
        variableMap.Add(declaration.Identifier, uniqueName);
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