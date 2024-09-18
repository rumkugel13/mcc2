using mcc2.AST;

namespace mcc2;

public class CaseCollector
{
    public void Collect(ASTProgram program)
    {
        foreach (var decl in program.Declarations)
            CollectDecls(decl);
    }

    private void CollectDecls(Declaration decl)
    {
        if (decl is Declaration.FunctionDeclaration func && func.Body != null)
            foreach (var item in func.Body.BlockItems)
            CollectBlockItem(item, []);
    }

    private void CollectBlockItem(BlockItem item, List<Statement> statements)
    {
        if (item is Statement stat)
            CollectStatement(stat, statements);
    }

    private void CollectStatement(Statement statement, List<Statement> statements)
    {
        switch (statement)
        {
            case Statement.ReturnStatement ret:
                break;
            case Statement.IfStatement ifStatement:
                CollectStatement(ifStatement.Then, statements);
                if (ifStatement.Else != null)
                    CollectStatement(ifStatement.Else, statements);
                break;
            case Statement.CompoundStatement compoundStatement:
                foreach (var item in compoundStatement.Block.BlockItems)
                    CollectBlockItem(item, statements);
                break;
            case Statement.ExpressionStatement expressionStatement:
                break;
            case Statement.WhileStatement whileStatement:
                CollectStatement(whileStatement.Body, statements);
                break;
            case Statement.DoWhileStatement doWhileStatement:
                CollectStatement(doWhileStatement.Body, statements);
                break;
            case Statement.ForStatement forStatement:
                CollectStatement(forStatement.Body, statements);
                break;
            case Statement.ContinueStatement continueStatement:
                break;
            case Statement.BreakStatement breakStatement:
                break;
            case Statement.NullStatement nullStatement:
                break;
            case Statement.GotoStatement go:
                break;
            case Statement.LabelStatement label:
                CollectStatement(label.Inner, statements);
                break;
            case Statement.SwitchStatement switchStatement:
                List<Statement> newList = [];
                CollectStatement(switchStatement.Inner, newList);
                switchStatement.Cases.AddRange(newList);
                break;
            case Statement.CaseStatement caseStatement:
                if (statements.Any(a => a is Statement.CaseStatement c && c.Expression == caseStatement.Expression))
                    throw SemanticError("Duplicate case statement");
                statements.Add(caseStatement);
                CollectStatement(caseStatement.Inner, statements);
                break;
            case Statement.DefaultStatement defaultStatement:
                if (statements.Any(a => a is Statement.DefaultStatement))
                    throw SemanticError("Duplicate default statement");
                statements.Add(defaultStatement);
                CollectStatement(defaultStatement.Inner, statements);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private Exception SemanticError(string message)
    {
        return new Exception("Semantic Error: " + message);
    }
}