using mcc2.AST;

namespace mcc2;

public class PrettyPrinter
{
    public void Print(ASTProgram program, string source)
    {
        PrintProgram(program, source, 0);
    }

    private void PrintProgram(ASTProgram program, string source, int indent)
    {
        PrintLine("Program(", indent);
        PrintFunctionDefinition(program.Function, source, indent + 1);
        PrintLine(")", indent);
    }

    private void PrintFunctionDefinition(FunctionDefinition functionDefinition, string source, int indent)
    {
        PrintLine("Function(", indent++);
        PrintLine($"name=\"{functionDefinition.Name}\",", indent);
        PrintLine($"body=(", indent);
        foreach (var item in functionDefinition.Body.BlockItems)
            PrintBlockItem(item, source, indent + 1);
        PrintLine(")", indent);
        PrintLine(")", --indent);
    }

    private void PrintBlockItem(BlockItem blockItem, string source, int indent)
    {
        switch (blockItem)
        {
            case Statement statement:
                PrintStatement(statement, source, indent);
                break;
            case Declaration declaration:
                PrintLine($"Declare(", indent);
                PrintLine($"name=\"{declaration.Identifier}\",", indent + 1);
                if (declaration.Initializer != null)
                    PrintExpression(declaration.Initializer, source, indent + 1);
                PrintLine(")", indent);
                break;
        }
    }

    private void PrintStatement(Statement statement, string source, int indent)
    {
        switch (statement)
        {
            case ReturnStatement ret:
                PrintLine($"Return(", indent);
                PrintExpression(ret.Expression, source, indent + 1);
                PrintLine(")", indent);
                break;
            case IfStatement ifStatement:
                PrintLine($"If(", indent);
                PrintExpression(ifStatement.Condition, source, indent + 1);
                PrintLine($"Then(", indent + 1);
                PrintStatement(ifStatement.Then, source, indent + 2);
                PrintLine(")", indent + 1);
                if (ifStatement.Else != null)
                {
                    PrintLine($"Else(", indent + 1);
                    PrintStatement(ifStatement.Else, source, indent + 2);
                    PrintLine(")", indent + 1);
                }
                PrintLine(")", indent);
                break;
            case CompoundStatement compoundStatement:
                foreach (var item in compoundStatement.Block.BlockItems)
                    PrintBlockItem(item, source, indent + 1);
                break;
        }
    }

    private void PrintExpression(Expression expression, string source, int indent)
    {
        switch (expression)
        {
            case ConstantExpression c:
                PrintLine($"Constant({c.Value})", indent);
                break;
            case UnaryExpression unaryExpression:
                PrintLine($"Unary(", indent);
                PrintLine($"{unaryExpression.Operator}(", indent + 1);
                PrintExpression(unaryExpression.Expression, source, indent + 2);
                PrintLine($")", indent + 1);
                break;
            case BinaryExpression binaryExpression:
                PrintLine($"Unary(", indent);
                PrintExpression(binaryExpression.ExpressionLeft, source, indent + 2);
                PrintLine($"{binaryExpression.Operator}(", indent + 1);
                PrintExpression(binaryExpression.ExpressionRight, source, indent + 2);
                PrintLine($")", indent + 1);
                PrintLine(")", indent);
                break;
            case VariableExpression variableExpression:
                PrintLine($"Var(", indent);
                PrintLine($"name=\"{variableExpression.Identifier}\",", indent + 1);
                PrintLine(")", indent);
                break;
            case AssignmentExpression assignmentExpression:
                PrintLine($"Assign(", indent);
                PrintExpression(assignmentExpression.ExpressionLeft, source, indent + 1);
                PrintLine($"Equals(", indent + 1);
                PrintExpression(assignmentExpression.ExpressionRight, source, indent + 2);
                PrintLine(")", indent + 1);
                PrintLine(")", indent);
                break;
            case ConditionalExpression conditionalExpression:
                PrintLine($"Conditional(", indent);
                PrintExpression(conditionalExpression.Condition, source, indent + 1);
                PrintLine($"Then(", indent + 1);
                PrintExpression(conditionalExpression.Then, source, indent + 2);
                PrintLine(")", indent + 1);
                if (conditionalExpression.Else != null)
                {
                    PrintLine($"Else(", indent + 1);
                    PrintExpression(conditionalExpression.Else, source, indent + 2);
                    PrintLine(")", indent + 1);
                }
                PrintLine(")", indent);
                break;
        }
    }

    private void PrintLine(string line, int indent)
    {
        Console.WriteLine($"{String.Concat(Enumerable.Repeat("|   ", indent))}{line}");
    }
}