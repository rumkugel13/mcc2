using mcc2.AST;

namespace mcc2;

public class LoopLabeler
{
    private int labelCounter;

    public void Label(ASTProgram program)
    {
        for (int i = 0; i < program.Declarations.Count; i++)
        {
            Declaration? decl = program.Declarations[i];
            if (decl is Declaration.FunctionDeclaration fun)
                program.Declarations[i] = LabelFunction(fun);
        }
    }

    private Declaration.FunctionDeclaration LabelFunction(Declaration.FunctionDeclaration functionDeclaration)
    {
        if (functionDeclaration.Body != null)
        {
            return new Declaration.FunctionDeclaration(functionDeclaration.Identifier,
                functionDeclaration.Parameters,
                LabelBlock(functionDeclaration.Body, []),
                functionDeclaration.FunctionType,
                functionDeclaration.StorageClass);
        }

        return functionDeclaration;
    }

    private Block LabelBlock(Block block, List<string> currentLabels)
    {
        List<BlockItem> newItems = [];
        foreach (var item in block.BlockItems)
        {
            var newItem = item;
            if (newItem is Statement statement)
                newItem = LabelStatement(statement, currentLabels);
            newItems.Add(newItem);
        }
        return new Block(newItems);
    }

    private Statement LabelStatement(Statement statement, List<string> currentLabels)
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
                    var then = LabelStatement(ifStatement.Then, currentLabels);
                    var el = ifStatement.Else;
                    if (el != null)
                        el = LabelStatement(el, currentLabels);
                    return new Statement.IfStatement(ifStatement.Condition, then, el);
                }
            case Statement.CompoundStatement compoundStatement:
                return new Statement.CompoundStatement(LabelBlock(compoundStatement.Block, currentLabels));
            case Statement.BreakStatement breakStatement:
                {if (currentLabels.Count == 0)
                    throw SemanticError("Break statement outside of loop");
                return new Statement.BreakStatement(currentLabels[^1]);}
            case Statement.ContinueStatement continueStatement:
                {if (currentLabels.Count == 0)
                    throw SemanticError("Continue statement outside of loop");
                var last = FindLast(currentLabels, "loop") ?? throw SemanticError("Can't continue out of switch");
                return new Statement.ContinueStatement(last);}
            case Statement.WhileStatement whileStatement:
                {
                    var newLabel = MakeLoopLabel();
                    currentLabels.Add(newLabel);
                    var body = LabelStatement(whileStatement.Body, currentLabels);
                    currentLabels.RemoveAt(currentLabels.Count - 1);
                    return new Statement.WhileStatement(whileStatement.Condition, body, newLabel);
                }
            case Statement.DoWhileStatement doWhileStatement:
                {
                    var newLabel = MakeLoopLabel();
                    currentLabels.Add(newLabel);
                    var body = LabelStatement(doWhileStatement.Body, currentLabels);
                    currentLabels.RemoveAt(currentLabels.Count - 1);
                    return new Statement.DoWhileStatement(body, doWhileStatement.Condition, newLabel);
                }
            case Statement.ForStatement forStatement:
                {
                    var newLabel = MakeLoopLabel();
                    currentLabels.Add(newLabel);
                    var body = LabelStatement(forStatement.Body, currentLabels);
                    currentLabels.RemoveAt(currentLabels.Count - 1);
                    return new Statement.ForStatement(forStatement.Init, forStatement.Condition, forStatement.Post, body, newLabel);
                }
            case Statement.GotoStatement go:
                return go;
            case Statement.LabelStatement label:
                {
                    var inner = LabelStatement(label.Inner, currentLabels);
                    return new Statement.LabelStatement(label.Label, inner);
                }
            case Statement.SwitchStatement switchStatement:
                {
                    var newLabel = MakeSwitchLabel();
                    currentLabels.Add(newLabel);
                    var inner = LabelStatement(switchStatement.Inner, currentLabels);
                    currentLabels.RemoveAt(currentLabels.Count - 1);
                    return new Statement.SwitchStatement(switchStatement.Expression, inner, newLabel, switchStatement.Cases);
                }
            case Statement.CaseStatement caseStatement:
                {
                    if (currentLabels == null)
                        throw SemanticError("Case statement outside of switch");
                    var last = FindLast(currentLabels, "switch") ?? throw SemanticError("Can't continue out of loop");
                    var inner = LabelStatement(caseStatement.Inner, currentLabels);
                    return new Statement.CaseStatement(caseStatement.Expression, inner, MakeCaseLabel());
                }
            case Statement.DefaultStatement defaultStatement:
                {
                    if (currentLabels == null)
                        throw SemanticError("Default statement outside of switch");
                    var last = FindLast(currentLabels, "switch") ?? throw SemanticError("Can't continue out of loop");
                    var inner = LabelStatement(defaultStatement.Inner, currentLabels);
                    return new Statement.DefaultStatement(inner, MakeCaseLabel());
                }
            default:
                throw new NotImplementedException();
        }
    }

    private string? FindLast(List<string> labels, string match)
    {
        for (int i = labels.Count - 1; i >= 0 ; i--)
        {
            if (labels[i].StartsWith(match))
                return labels[i];
        }
        return null;
    }

    private string MakeLoopLabel()
    {
        return $"loop.{labelCounter++}";
    }

    private string MakeSwitchLabel()
    {
        return $"switch.{labelCounter++}";
    }

    private string MakeCaseLabel()
    {
        return $"case.{labelCounter++}";
    }

    private Exception SemanticError(string message)
    {
        return new Exception("Semantic Error: " + message);
    }
}