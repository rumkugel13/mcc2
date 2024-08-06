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
        var name = GetIdentifier(functionDefinition.Identifier, source);
        PrintLine($"name=\"{name}\",", indent);
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
                var val = GetConstant(c.Constant, source);
                PrintLine($"Constant({val})", indent);
                break;
        }
    }

    private string GetIdentifier(Lexer.Token token, string source)
    {
        Regex regex = new($"\\G[a-zA-Z_]\\w*\\b");
        Match match = regex.Match(source, token.Position);
        Debug.Assert(match.Success, "There should be an Identifier");
        return match.Value;
    }

    private int GetConstant(Lexer.Token token, string source)
    {
        Regex regex = new($"\\G[0-9]+\\b");
        Match match = regex.Match(source, token.Position);
        Debug.Assert(match.Success, "There should be a Constant");
        return int.Parse(match.Value);
    }

    private void PrintLine(string line, int indent)
    {
        Console.WriteLine($"{String.Concat(Enumerable.Repeat("|   ", indent))}{line}");
    }
}