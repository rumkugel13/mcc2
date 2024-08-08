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
        PrintStatement(functionDefinition.Body, source, indent + 1);
        PrintLine(")", indent);
        PrintLine(")", --indent);
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
                break;
        }
    }

    private void PrintLine(string line, int indent)
    {
        Console.WriteLine($"{String.Concat(Enumerable.Repeat("|   ", indent))}{line}");
    }
}