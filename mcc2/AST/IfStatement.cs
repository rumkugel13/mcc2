namespace mcc2.AST;

public class IfStatement : Statement
{
    public Expression Condition;
    public Statement Then;
    public Statement? Else;

    public IfStatement(Expression condition, Statement then, Statement? elseStatement)
    {
        this.Condition = condition;
        this.Then = then;
        this.Else = elseStatement;
    }
}