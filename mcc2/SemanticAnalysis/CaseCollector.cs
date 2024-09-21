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
                var switchType = TypeChecker.GetType(switchStatement.Expression);
                HashSet<Const> cases = [];
                CollectStatement(switchStatement.Inner, newList);
                for (int i = 0; i < newList.Count; i++)
                {
                    if (newList[i] is Statement.CaseStatement ca)
                    {
                        var newConst = ConvertConstToType(((Expression.Constant)ca.Expression).Value, switchType);
                        if (!cases.Add(newConst))
                            throw SemanticError("Duplicate case statements");
                        newList[i] = new Statement.CaseStatement(new Expression.Constant(newConst, switchType), ca.Inner, ca.Label);
                    }
                }
                switchStatement.Cases.AddRange(newList);
                break;
            case Statement.CaseStatement caseStatement:
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

    private Const ConvertConstToType(Const original, Type target)
    {
        ulong value = original switch
        {
            Const.ConstInt constInt => (ulong)constInt.Value,
            Const.ConstLong constLong => (ulong)constLong.Value,
            Const.ConstUInt constUInt => (ulong)constUInt.Value,
            Const.ConstULong constULong => (ulong)constULong.Value,
            Const.ConstChar constChar => (ulong)constChar.Value,
            Const.ConstUChar constUChar => (ulong)constUChar.Value,
            _ => throw new NotImplementedException()
        };

        return target switch {
            Type.Int => new Const.ConstInt((int)value),
            Type.Long => new Const.ConstLong((long)value),
            Type.UInt => new Const.ConstUInt((uint)value),
            Type.ULong => new Const.ConstULong((ulong)value),
            Type.Char or Type.SChar => new Const.ConstChar((char)value),
            Type.UChar => new Const.ConstUChar((byte)value),
            _ => throw new NotImplementedException()
        };
    }

    private Exception SemanticError(string message)
    {
        return new Exception("Semantic Error: " + message);
    }
}