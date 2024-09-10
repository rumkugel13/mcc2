using mcc2.AST;

namespace mcc2;

public class LoopLabeler
{
    private int loopCounter;

    public void Label(ASTProgram program)
    {
        for (int i = 0; i < program.Declarations.Count; i++)
        {
            Declaration? decl = program.Declarations[i];
            if (decl is Declaration.FunctionDeclaration fun)
                program.Declarations[i] = LabelFunction(fun, null);
        }
    }

    private Declaration.FunctionDeclaration LabelFunction(Declaration.FunctionDeclaration functionDeclaration, string? currentLabel)
    {
        if (functionDeclaration.Body != null)
        {
            return new Declaration.FunctionDeclaration(functionDeclaration.Identifier,
                functionDeclaration.Parameters,
                LabelBlock(functionDeclaration.Body, null),
                functionDeclaration.FunctionType,
                functionDeclaration.StorageClass);
        }

        return functionDeclaration;
    }

    private Block LabelBlock(Block block, string? currentLabel)
    {
        List<BlockItem> newItems = [];
        foreach (var item in block.BlockItems)
        {
            var newItem = item;
            if (newItem is Statement statement)
                newItem = LabelStatement(statement, currentLabel);
            newItems.Add(newItem);
        }
        return new Block(newItems);
    }

    private Statement LabelStatement(Statement statement, string? currentLabel)
    {
        switch (statement)
        {
            case Statement.ReturnStatement returnStatement:
                return returnStatement;
            case Statement.ExpressionStatement expressionStatement:
                return expressionStatement;
            case Statement.NullStatement nullStatement:
                return nullStatement;
            case Statement.IfStatement ifStatement:
                {
                    var then = LabelStatement(ifStatement.Then, currentLabel);
                    var el = ifStatement.Else;
                    if (el != null)
                        el = LabelStatement(el, currentLabel);
                    return new Statement.IfStatement(ifStatement.Condition, then, el);
                }
            case Statement.CompoundStatement compoundStatement:
                return new Statement.CompoundStatement(LabelBlock(compoundStatement.Block, currentLabel));
            case Statement.BreakStatement breakStatement:
                if (currentLabel == null)
                    throw SemanticError("Break statement outside of loop");
                return new Statement.BreakStatement(currentLabel);
            case Statement.ContinueStatement continueStatement:
                if (currentLabel == null)
                    throw SemanticError("Continue statement outside of loop");
                return new Statement.ContinueStatement(currentLabel);
            case Statement.WhileStatement whileStatement:
                {
                    var newLabel = MakeLabel();
                    var body = LabelStatement(whileStatement.Body, newLabel);
                    return new Statement.WhileStatement(whileStatement.Condition, body, newLabel);
                }
            case Statement.DoWhileStatement doWhileStatement:
                {
                    var newLabel = MakeLabel();
                    var body = LabelStatement(doWhileStatement.Body, newLabel);
                    return new Statement.DoWhileStatement(body, doWhileStatement.Condition, newLabel);
                }
            case Statement.ForStatement forStatement:
                {
                    var newLabel = MakeLabel();
                    var body = LabelStatement(forStatement.Body, newLabel);
                    return new Statement.ForStatement(forStatement.Init, forStatement.Condition, forStatement.Post, body, newLabel);
                }
            default:
                throw new NotImplementedException();
        }
    }

    private string MakeLabel()
    {
        return $"loop.{loopCounter++}";
    }

    private Exception SemanticError(string message)
    {
        return new Exception("Semantic Error: " + message);
    }
}