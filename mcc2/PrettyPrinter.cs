using mcc2.AST;

namespace mcc2;

public class PrettyPrinter
{
    Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable;
    Dictionary<string, SemanticAnalyzer.StructEntry> typeTable;

    public PrettyPrinter(Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable, Dictionary<string, SemanticAnalyzer.StructEntry> typeTable)
    {
        this.symbolTable = symbolTable;
        this.typeTable = typeTable;
    }

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
        foreach (var entry in symbolTable)
        {
            PrintLine("Symbols(", indent + 1);
            PrintLine($"key=\"{entry.Key}\"", indent + 2);
            PrintLine($"value=(", indent + 2);
            PrintLine($"attributes={entry.Value.IdentifierAttributes}", indent + 3);
            PrintLine($"type={entry.Value.Type}", indent + 3);
            PrintLine(")", indent + 2);
            PrintLine(")", indent + 1);
        }
        foreach (var entry in typeTable)
        {
            PrintLine("Types(", indent + 1);
            PrintLine($"key=\"{entry.Key}\"", indent + 2);
            PrintLine($"value=(", indent + 2);
            PrintLine($"alignment={entry.Value.Alignment}", indent + 3);
            PrintLine($"size={entry.Value.Size}", indent + 3);
            PrintLine($"members=(", indent + 3);
            foreach (var member in entry.Value.Members)
            {
                PrintLine($"{member}", indent + 4);
            }
            PrintLine($")", indent + 3);
            PrintLine(")", indent + 2);
            PrintLine(")", indent + 1);
        }
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

        Type.FunctionType funcType = (Type.FunctionType)symbolTable[functionDefinition.Identifier].Type;

        PrintLine("Function(", indent++);
        PrintLine($"name=\"{functionDefinition.Identifier}\",", indent);
        if (functionDefinition.Parameters.Count > 0)
        {
            PrintLine($"parameters=(", indent);
            for (int i = 0; i < functionDefinition.Parameters.Count; i++)
            {
                PrintLine($"name=\"{functionDefinition.Parameters[i]}\", type={funcType.Parameters[i]}", indent + 1);
            }
            PrintLine(")", indent);
        }
        else
            PrintLine("parameters=()", indent);
        PrintLine($"returnType={funcType.Return}", indent);
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
                PrintLine($"type=({declaration.VariableType})", indent + 1);
                if (declaration.Initializer != null)
                {
                    PrintLine("init=(", indent + 1);
                    PrintInitializer(declaration.Initializer, source, indent + 2);
                    PrintLine(")", indent + 1);
                }
                PrintLine(")", indent);
                break;
            case Declaration.FunctionDeclaration fun:
                PrintFunctionDefinition(fun, source, indent);
                break;
        }
    }

    private void PrintInitializer(Initializer initializer, string source, int indent)
    {
        switch (initializer)
        {
            case Initializer.SingleInitializer single:
                PrintExpression(single.Expression, source, indent);
                break;
            case Initializer.CompoundInitializer compound:
                PrintLine("Compound{", indent);
                foreach (var init in compound.Initializers)
                    PrintInitializer(init, source, indent + 1);
                PrintLine("}", indent);
                break;
        }
    }

    private void PrintStatement(Statement statement, string source, int indent)
    {
        switch (statement)
        {
            case Statement.ReturnStatement ret:
                PrintLine($"Return(", indent);
                if (ret.Expression != null)
                    PrintExpression(ret.Expression, source, indent + 1);
                else
                    PrintLine("void", indent + 1);
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
            case Expression.Constant constant:
                PrintLine($"Constant(value=({constant.Value}))", indent);
                break;
            case Expression.Unary unary:
                PrintLine($"Unary(", indent);
                PrintLine($"{unary.Operator}(", indent + 1);
                PrintExpression(unary.Expression, source, indent + 2);
                PrintLine($")", indent + 1);
                PrintLine($")", indent);
                break;
            case Expression.Binary binary:
                PrintLine($"Binary(", indent);
                PrintExpression(binary.Left, source, indent + 2);
                PrintLine($"operator=\"{binary.Operator}\"", indent + 1);
                PrintExpression(binary.Right, source, indent + 2);
                PrintLine($")", indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.Variable variable:
                PrintLine($"Var(", indent);
                PrintLine($"name=\"{variable.Identifier}\"", indent + 1);
                PrintLine($"type=({variable.Type})", indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.Assignment assignment:
                PrintLine($"Assign(", indent);
                PrintExpression(assignment.Left, source, indent + 1);
                PrintLine($"Equals(", indent + 1);
                PrintExpression(assignment.Right, source, indent + 2);
                PrintLine(")", indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.Conditional conditional:
                PrintLine($"Conditional(", indent);
                PrintExpression(conditional.Condition, source, indent + 1);
                PrintLine($"Then(", indent + 1);
                PrintExpression(conditional.Then, source, indent + 2);
                PrintLine(")", indent + 1);
                if (conditional.Else != null)
                {
                    PrintLine($"Else(", indent + 1);
                    PrintExpression(conditional.Else, source, indent + 2);
                    PrintLine(")", indent + 1);
                }
                PrintLine(")", indent);
                break;
            case Expression.FunctionCall functionCall:
                PrintLine($"Call(", indent);
                PrintLine($"name=\"{functionCall.Identifier}\",", indent + 1);
                if (functionCall.Arguments.Count > 0)
                {
                    PrintLine($"args=(", indent + 1);
                    foreach (var arg in functionCall.Arguments)
                    {
                        PrintExpression(arg, source, indent + 2);
                    }
                    PrintLine(")", indent + 1);
                }
                else
                    PrintLine("args=()", indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.Cast cast:
                PrintLine($"Cast(", indent);
                PrintLine($"target=\"{cast.TargetType}\"", indent + 1);
                PrintExpression(cast.Expression, source, indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.Dereference dereference:
                PrintLine($"Dereference(", indent);
                PrintExpression(dereference.Expression, source, indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.AddressOf addressOf:
                PrintLine($"AddressOf(", indent);
                PrintExpression(addressOf.Expression, source, indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.Subscript subscript:
                PrintLine($"Subscript(", indent);
                PrintLine($"target=", indent + 1);
                PrintExpression(subscript.Left, source, indent + 2);
                PrintLine($"index=", indent + 1);
                PrintExpression(subscript.Right, source, indent + 2);
                PrintLine(")", indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.String str:
                PrintLine($"String(", indent);
                PrintLine($"value='{str.StringVal}'", indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.SizeOf sizeofExp:
                PrintLine("SizeOf(", indent);
                PrintExpression(sizeofExp.Expression, source, indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.SizeOfType sizeofType:
                PrintLine("SizeOfType(", indent);
                PrintLine($"target=\"{sizeofType.TargetType}\"", indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.Dot dot:
                PrintLine($"Dot(", indent);
                PrintLine($"structure=(", indent + 1);
                PrintExpression(dot.Structure, source, indent + 2);
                PrintLine(")", indent + 1);
                PrintLine($"member=\"{dot.Member}\"", indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.Arrow arrow:
                PrintLine($"Arrow(", indent);
                PrintLine($"pointer=(", indent + 1);
                PrintExpression(arrow.Pointer, source, indent + 2);
                PrintLine(")", indent + 1);
                PrintLine($"member=\"{arrow.Member}\"", indent + 1);
                PrintLine(")", indent);
                break;
        }
    }

    private void PrintLine(string line, int indent)
    {
        Console.WriteLine($"{String.Concat(Enumerable.Repeat("|   ", indent))}{line}");
    }
}