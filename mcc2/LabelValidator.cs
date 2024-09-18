using mcc2.AST;

namespace mcc2;

public class LabelValidator
{
    public void Validate(ASTProgram program)
    {
        foreach (var decl in program.Declarations)
        {
            if (decl is Declaration.FunctionDeclaration func)
            {
                ValidateFunction(func);
            }
        }
    }

    private void ValidateFunction(Declaration.FunctionDeclaration func)
    {
        if (func.Body == null)
            return;

        HashSet<string> gotoLabels = [];
        Queue<Statement.GotoStatement> toCheck = [];

        foreach (var item in func.Body.BlockItems)
        {
            ValidateBlockItem(item, gotoLabels, toCheck);
        }

        foreach (var go in toCheck)
        {
            if (!gotoLabels.Contains(go.Label))
                throw LabelError($"Unknown goto label {go.Label}");
        }
    }

    private void ValidateBlockItem(BlockItem item, HashSet<string> gotoLabels, Queue<Statement.GotoStatement> toCheck)
    {
        if (item is Statement stat)
            ValidateStatement(stat, gotoLabels, toCheck);
    }

    private void ValidateStatement(Statement stat, HashSet<string> gotoLabels, Queue<Statement.GotoStatement> toCheck)
    {
        switch (stat)
        {
            case Statement.ReturnStatement ret:
                break;
            case Statement.IfStatement ifStatement:
                ValidateStatement(ifStatement.Then, gotoLabels, toCheck);
                if (ifStatement.Else != null)
                    ValidateStatement(ifStatement.Else, gotoLabels, toCheck);
                break;
            case Statement.CompoundStatement compoundStatement:
                foreach (var item in compoundStatement.Block.BlockItems)
                    ValidateBlockItem(item, gotoLabels, toCheck);
                break;
            case Statement.ExpressionStatement expressionStatement:
                break;
            case Statement.WhileStatement whileStatement:
                ValidateStatement(whileStatement.Body, gotoLabels, toCheck);
                break;
            case Statement.DoWhileStatement doWhileStatement:
                ValidateStatement(doWhileStatement.Body, gotoLabels, toCheck);
                break;
            case Statement.ForStatement forStatement:
                ValidateStatement(forStatement.Body, gotoLabels, toCheck);
                break;
            case Statement.ContinueStatement continueStatement:
                break;
            case Statement.BreakStatement breakStatement:
                break;
            case Statement.NullStatement nullStatement:
                break;
            case Statement.GotoStatement go:
                if (!gotoLabels.Contains(go.Label))
                    toCheck.Enqueue(go);
                break;
            case Statement.LabelStatement label:
                if (!gotoLabels.Add(label.Label))
                    throw LabelError($"Label {label.Label} already exists");
                ValidateStatement(label.Inner, gotoLabels, toCheck);
                break;
            case Statement.SwitchStatement switchStatement:
                ValidateStatement(switchStatement.Inner, gotoLabels, toCheck);
                break;
            case Statement.CaseStatement caseStatement:
                ValidateStatement(caseStatement.Inner, gotoLabels, toCheck);
                break;
            case Statement.DefaultStatement defaultStatement:
                ValidateStatement(defaultStatement.Inner, gotoLabels, toCheck);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private Exception LabelError(string message)
    {
        return new Exception("Label Error: " + message);
    }
}