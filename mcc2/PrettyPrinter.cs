using mcc2.AST;

namespace mcc2;

public class PrettyPrinter
{
    public void Print(ASTProgram program, string source)
    {
        PrintProgram(program, source, 0);
    }

    // todo: actually update for once, to support new ast nodes
    private void PrintProgram(ASTProgram program, string source, int indent)
    {
        PrintLine("Program(", indent);
        foreach (var fun in program.Declarations)
            PrintDeclaration(fun, source, indent + 1);
        PrintLine(")", indent);
    }

    private void PrintDeclaration(Declaration declaration, string source, int indent)
    {
        switch (declaration)
        {
            case Declaration.FunctionDeclaration functionDeclaration:
                PrintFunctionDefinition(functionDeclaration, source, indent);
                break;
            case Declaration.VariableDeclaration variableDeclaration:
                PrintBlockItem(variableDeclaration, source, indent);
                break;
        }
    }

    private void PrintFunctionDefinition(Declaration.FunctionDeclaration functionDefinition, string source, int indent)
    {
        PrintLine("Function(", indent++);
        PrintLine($"name=\"{functionDefinition.Identifier}\",", indent);
        PrintLine($"parameters=(", indent);
        foreach (var param in functionDefinition.Parameters)
        {
            PrintLine($"\"{param}\",", indent + 1);
        }
        PrintLine(")", indent);
        PrintLine($"body=(", indent);
        if (functionDefinition.Body != null)
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
            case Declaration.VariableDeclaration declaration:
                PrintLine($"Declare(", indent);
                PrintLine($"name=\"{declaration.Identifier}\",", indent + 1);
                if (declaration.Initializer != null)
                    PrintExpression(declaration.Initializer, source, indent + 1);
                PrintLine(")", indent);
                break;
            case Declaration.FunctionDeclaration fun:
                PrintFunctionDefinition(fun, source, indent);
                break;
        }
    }

    private void PrintStatement(Statement statement, string source, int indent)
    {
        switch (statement)
        {
            case Statement.ReturnStatement ret:
                PrintLine($"Return(", indent);
                PrintExpression(ret.Expression, source, indent + 1);
                PrintLine(")", indent);
                break;
            case Statement.IfStatement ifStatement:
                PrintLine($"If(", indent);
                PrintLine("condition=", indent + 1);
                PrintExpression(ifStatement.Condition, source, indent + 2);
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
            case Statement.CompoundStatement compoundStatement:
                PrintLine($"Compund(", indent);
                foreach (var item in compoundStatement.Block.BlockItems)
                    PrintBlockItem(item, source, indent + 1);
                PrintLine(")", indent);
                break;
            case Statement.ExpressionStatement expressionStatement:
                PrintExpression(expressionStatement.Expression, source, indent);
                break;
            case Statement.WhileStatement whileStatement:
                PrintLine($"While(", indent);
                PrintLine("condition=", indent + 1);
                PrintExpression(whileStatement.Condition, source, indent + 2);
                PrintLine(")", indent + 1);
                PrintLine("body=", indent + 1);
                PrintStatement(whileStatement.Body, source, indent + 2);
                PrintLine(")", indent + 1);
                PrintLine(")", indent);
                break;
            case Statement.DoWhileStatement doWhileStatement:
                PrintLine($"DoWhile(", indent);
                PrintLine("body=", indent + 1);
                PrintStatement(doWhileStatement.Body, source, indent + 2);
                PrintLine(")", indent + 1);
                PrintLine("condition=", indent + 1);
                PrintExpression(doWhileStatement.Condition, source, indent + 2);
                PrintLine(")", indent + 1);
                PrintLine(")", indent);
                break;
            case Statement.ForStatement forStatement:
                PrintLine($"For(", indent);
                PrintLine("init=", indent + 1);
                if (forStatement.Init is ForInit.InitDeclaration initDeclaration)
                    PrintDeclaration(initDeclaration.Declaration, source, indent + 2);
                else if (forStatement.Init is ForInit.InitExpression initExpression && initExpression.Expression != null)
                    PrintExpression(initExpression.Expression, source, indent + 2);
                PrintLine(")", indent + 1);
                PrintLine("condition=", indent + 1);
                if (forStatement.Condition != null)
                    PrintExpression(forStatement.Condition, source, indent + 2);
                else
                    PrintLine("true", indent + 2);
                PrintLine(")", indent + 1);
                PrintLine("post=", indent + 1);
                if (forStatement.Post != null)
                    PrintExpression(forStatement.Post, source, indent + 2);
                PrintLine(")", indent + 1);
                PrintLine("body=", indent + 1);
                PrintStatement(forStatement.Body, source, indent + 2);
                PrintLine(")", indent + 1);
                PrintLine(")", indent);
                break;
            case Statement.ContinueStatement continueStatement:
                PrintLine($"Continue()", indent);
                break;
            case Statement.BreakStatement breakStatement:
                PrintLine($"Break()", indent);
                break;
        }
    }

    private void PrintExpression(Expression expression, string source, int indent)
    {
        switch (expression)
        {
            case Expression.ConstantExpression c:
                PrintLine($"Constant(", indent);
                PrintLine($"value=\"{c.Value}\"", indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.UnaryExpression unaryExpression:
                PrintLine($"Unary(", indent);
                PrintLine($"{unaryExpression.Operator}(", indent + 1);
                PrintExpression(unaryExpression.Expression, source, indent + 2);
                PrintLine($")", indent + 1);
                break;
            case Expression.BinaryExpression binaryExpression:
                PrintLine($"Binary(", indent);
                PrintExpression(binaryExpression.ExpressionLeft, source, indent + 2);
                PrintLine($"operator=\"{binaryExpression.Operator}\"", indent + 1);
                PrintExpression(binaryExpression.ExpressionRight, source, indent + 2);
                PrintLine($")", indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.VariableExpression variableExpression:
                PrintLine($"Var(", indent);
                PrintLine($"name=\"{variableExpression.Identifier}\"", indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.AssignmentExpression assignmentExpression:
                PrintLine($"Assign(", indent);
                PrintExpression(assignmentExpression.ExpressionLeft, source, indent + 1);
                PrintLine($"Equals(", indent + 1);
                PrintExpression(assignmentExpression.ExpressionRight, source, indent + 2);
                PrintLine(")", indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.ConditionalExpression conditionalExpression:
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
            case Expression.FunctionCallExpression functionCallExpression:
                PrintLine($"Call(", indent);
                PrintLine($"name=\"{functionCallExpression.Identifier}\",", indent + 1);
                PrintLine($"args=(", indent + 1);
                foreach (var arg in functionCallExpression.Arguments)
                {
                    PrintExpression(arg, source, indent + 2);
                }
                PrintLine(")", indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.CastExpression castExpression:
                PrintLine($"Cast(", indent);
                PrintLine($"target=\"{castExpression.TargetType}\"", indent + 1);
                PrintExpression(castExpression.Expression, source, indent + 1);
                PrintLine(")", indent);
                break;
        }
    }

    private void PrintLine(string line, int indent)
    {
        Console.WriteLine($"{String.Concat(Enumerable.Repeat("|   ", indent))}{line}");
    }
}