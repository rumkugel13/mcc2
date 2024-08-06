using System.Diagnostics;
using System.Text.RegularExpressions;
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
        }
    }

    private void PrintLine(string line, int indent)
    {
        Console.WriteLine($"{String.Concat(Enumerable.Repeat("|   ", indent))}{line}");
    }
}