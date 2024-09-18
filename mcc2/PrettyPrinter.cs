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

    public void Print(ASTProgram program)
    {
        PrintProgram(program, 0);
    }

    private void PrintProgram(ASTProgram program, int indent)
    {
        PrintLine("Program(", indent);
        foreach (var fun in program.Declarations)
            PrintDeclaration(fun, indent + 1);
        foreach (var entry in symbolTable)
        {
            PrintLine("Symbols(", indent + 1);
            PrintLine($"key=\"{entry.Key}\"", indent + 2);
            PrintLine($"value=(", indent + 2);
            PrintLine($"attributes={entry.Value.IdentifierAttributes}", indent + 3);
            PrintLine($"type={entry.Value.Type}", indent + 3);
            PrintEndLine(2, indent + 1);
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
            PrintEndLine(3, indent + 1);
        }
        PrintLine(")", indent);
    }

    private void PrintDeclaration(Declaration declaration, int indent)
    {
        switch (declaration)
        {
            case Declaration.FunctionDeclaration functionDeclaration:
                PrintFunctionDefinition(functionDeclaration, indent);
                break;
            case Declaration.VariableDeclaration variableDeclaration:
                PrintBlockItem(variableDeclaration, indent);
                break;
        }
    }

    private void PrintFunctionDefinition(Declaration.FunctionDeclaration functionDefinition, int indent)
    {
        Type.FunctionType funcType = (Type.FunctionType)symbolTable[functionDefinition.Identifier].Type;

        PrintLine("Function(", indent);
        PrintLine($"name=\"{functionDefinition.Identifier}\",", indent + 1);
        if (functionDefinition.Parameters.Count > 0)
        {
            PrintLine($"parameters=(", indent + 1);
            for (int i = 0; i < functionDefinition.Parameters.Count; i++)
            {
                PrintLine($"name=\"{functionDefinition.Parameters[i]}\", type={funcType.Parameters[i]}", indent + 2);
            }
            PrintLine(")", indent + 1);
        }
        else
            PrintLine("parameters=()", indent + 1);
        PrintLine($"returnType={funcType.Return}", indent + 1);
        PrintLine($"body=(", indent + 1);
        if (functionDefinition.Body != null)
        {
            foreach (var item in functionDefinition.Body.BlockItems)
                PrintBlockItem(item, indent + 2);
        }
        PrintEndLine(2, indent);
    }

    private void PrintBlockItem(BlockItem blockItem, int indent)
    {
        switch (blockItem)
        {
            case Statement statement:
                PrintStatement(statement, indent);
                break;
            case Declaration.VariableDeclaration declaration:
                PrintLine($"Declare(", indent);
                PrintLine($"name=\"{declaration.Identifier}\",", indent + 1);
                PrintLine($"type=({declaration.VariableType})", indent + 1);
                if (declaration.Initializer != null)
                {
                    PrintLine("init=(", indent + 1);
                    PrintInitializer(declaration.Initializer, indent + 2);
                    PrintLine(")", indent + 1);
                }
                PrintLine(")", indent);
                break;
            case Declaration.FunctionDeclaration fun:
                PrintFunctionDefinition(fun, indent);
                break;
        }
    }

    private void PrintInitializer(Initializer initializer, int indent)
    {
        switch (initializer)
        {
            case Initializer.SingleInitializer single:
                PrintExpression(single.Expression, indent);
                break;
            case Initializer.CompoundInitializer compound:
                PrintLine("Compound{", indent);
                foreach (var init in compound.Initializers)
                    PrintInitializer(init, indent + 1);
                PrintLine("}", indent);
                break;
        }
    }

    private void PrintStatement(Statement statement, int indent)
    {
        switch (statement)
        {
            case Statement.ReturnStatement ret:
                PrintLine($"Return(", indent);
                if (ret.Expression != null)
                    PrintExpression(ret.Expression, indent + 1);
                else
                    PrintLine("void", indent + 1);
                PrintLine(")", indent);
                break;
            case Statement.IfStatement ifStatement:
                PrintLine($"If(", indent);
                PrintLine("condition=", indent + 1);
                PrintExpression(ifStatement.Condition, indent + 2);
                PrintLine($"Then(", indent + 1);
                PrintStatement(ifStatement.Then, indent + 2);
                PrintLine(")", indent + 1);
                if (ifStatement.Else != null)
                {
                    PrintLine($"Else(", indent + 1);
                    PrintStatement(ifStatement.Else, indent + 2);
                    PrintLine(")", indent + 1);
                }
                PrintLine(")", indent);
                break;
            case Statement.CompoundStatement compoundStatement:
                PrintLine($"Compound(", indent);
                foreach (var item in compoundStatement.Block.BlockItems)
                    PrintBlockItem(item, indent + 1);
                PrintLine(")", indent);
                break;
            case Statement.ExpressionStatement expressionStatement:
                PrintExpression(expressionStatement.Expression, indent);
                break;
            case Statement.WhileStatement whileStatement:
                PrintLine($"While(", indent);
                PrintLine("condition=", indent + 1);
                PrintExpression(whileStatement.Condition, indent + 2);
                PrintLine(")", indent + 1);
                PrintLine("body=", indent + 1);
                PrintStatement(whileStatement.Body, indent + 2);
                PrintEndLine(2, indent);
                break;
            case Statement.DoWhileStatement doWhileStatement:
                PrintLine($"DoWhile(", indent);
                PrintLine("body=", indent + 1);
                PrintStatement(doWhileStatement.Body, indent + 2);
                PrintLine(")", indent + 1);
                PrintLine("condition=", indent + 1);
                PrintExpression(doWhileStatement.Condition, indent + 2);
                PrintEndLine(2, indent);
                break;
            case Statement.ForStatement forStatement:
                PrintLine($"For(", indent);
                PrintLine("init=", indent + 1);
                if (forStatement.Init is ForInit.InitDeclaration initDeclaration)
                    PrintDeclaration(initDeclaration.Declaration, indent + 2);
                else if (forStatement.Init is ForInit.InitExpression initExpression && initExpression.Expression != null)
                    PrintExpression(initExpression.Expression, indent + 2);
                PrintLine(")", indent + 1);
                PrintLine("condition=", indent + 1);
                if (forStatement.Condition != null)
                    PrintExpression(forStatement.Condition, indent + 2);
                else
                    PrintLine("true", indent + 2);
                PrintLine(")", indent + 1);
                PrintLine("post=", indent + 1);
                if (forStatement.Post != null)
                    PrintExpression(forStatement.Post, indent + 2);
                PrintLine(")", indent + 1);
                PrintLine("body=", indent + 1);
                PrintStatement(forStatement.Body, indent + 2);
                PrintEndLine(2, indent);
                break;
            case Statement.ContinueStatement continueStatement:
                PrintLine($"Continue()", indent);
                break;
            case Statement.BreakStatement breakStatement:
                PrintLine($"Break()", indent);
                break;
            case Statement.NullStatement:
                break;
            case Statement.GotoStatement go:
                PrintLine($"Goto({go.Label})", indent);
                break;
            case Statement.LabelStatement label:
                PrintLine($"Label({label.Label})", indent);
                PrintStatement(label.Inner, indent);
                break;
            case Statement.SwitchStatement switchStatement:
                PrintLine($"Switch(", indent);
                PrintExpression(switchStatement.Expression, indent + 1);
                PrintLine(")", indent);
                PrintStatement(switchStatement.Inner, indent);
                break;
            case Statement.CaseStatement caseStatement:
                PrintLine($"Case(", indent);
                PrintExpression(caseStatement.Expression, indent + 1);
                PrintLine(")", indent);
                PrintStatement(caseStatement.Inner, indent);
                break;
            case Statement.DefaultStatement defaultStatement:
                PrintLine($"Default()", indent);
                PrintStatement(defaultStatement.Inner, indent);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void PrintExpression(Expression expression, int indent)
    {
        switch (expression)
        {
            case Expression.Constant constant:
                PrintLine($"Constant(value=({constant.Value}))", indent);
                break;
            case Expression.Unary unary:
                PrintLine($"Unary(", indent);
                PrintLine($"{unary.Operator}(", indent + 1);
                PrintExpression(unary.Expression, indent + 2);
                PrintEndLine(2, indent);
                break;
            case Expression.Binary binary:
                PrintLine($"Binary(", indent);
                PrintExpression(binary.Left, indent + 2);
                PrintLine($"operator=\"{binary.Operator}\"", indent + 1);
                PrintExpression(binary.Right, indent + 2);
                PrintEndLine(2, indent);
                break;
            case Expression.Variable variable:
                PrintLine($"Var(", indent);
                PrintLine($"name=\"{variable.Identifier}\"", indent + 1);
                PrintLine($"type=({variable.Type})", indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.Assignment assignment:
                PrintLine($"Assign(", indent);
                PrintExpression(assignment.Left, indent + 1);
                PrintLine($"Equals(", indent + 1);
                PrintExpression(assignment.Right, indent + 2);
                PrintEndLine(2, indent);
                break;
            case Expression.Conditional conditional:
                PrintLine($"Conditional(", indent);
                PrintExpression(conditional.Condition, indent + 1);
                PrintLine($"Then(", indent + 1);
                PrintExpression(conditional.Then, indent + 2);
                PrintLine(")", indent + 1);
                if (conditional.Else != null)
                {
                    PrintLine($"Else(", indent + 1);
                    PrintExpression(conditional.Else, indent + 2);
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
                        PrintExpression(arg, indent + 2);
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
                PrintExpression(cast.Expression, indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.Dereference dereference:
                PrintLine($"Dereference(", indent);
                PrintExpression(dereference.Expression, indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.AddressOf addressOf:
                PrintLine($"AddressOf(", indent);
                PrintExpression(addressOf.Expression, indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.Subscript subscript:
                PrintLine($"Subscript(", indent);
                PrintLine($"target=", indent + 1);
                PrintExpression(subscript.Left, indent + 2);
                PrintLine($"index=", indent + 1);
                PrintExpression(subscript.Right, indent + 2);
                PrintEndLine(2, indent);
                break;
            case Expression.String str:
                PrintLine($"String(", indent);
                PrintLine($"value='{str.StringVal}'", indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.SizeOf sizeofExp:
                PrintLine("SizeOf(", indent);
                PrintExpression(sizeofExp.Expression, indent + 1);
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
                PrintExpression(dot.Structure, indent + 2);
                PrintLine(")", indent + 1);
                PrintLine($"member=\"{dot.Member}\"", indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.Arrow arrow:
                PrintLine($"Arrow(", indent);
                PrintLine($"pointer=(", indent + 1);
                PrintExpression(arrow.Pointer, indent + 2);
                PrintLine(")", indent + 1);
                PrintLine($"member=\"{arrow.Member}\"", indent + 1);
                PrintLine(")", indent);
                break;
            case Expression.PostfixIncrement inc:
                PrintLine($"PostfixIncrement(", indent);
                PrintExpression(inc.Expression, indent + 1);
                PrintEndLine(1, indent);
                break;
            case Expression.PostfixDecrement dec:
                PrintLine($"PostfixDecrement(", indent);
                PrintExpression(dec.Expression, indent + 1);
                PrintEndLine(1, indent);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void PrintLine(string line, int indent)
    {
        Console.WriteLine($"{string.Concat(Enumerable.Repeat("|   ", indent))}{line}");
    }

    private void PrintEndLine(int amount, int indent)
    {
        Console.WriteLine($"{string.Concat(Enumerable.Repeat("|   ", indent))}{string.Concat(Enumerable.Repeat(")   ", amount))}");
    }
}