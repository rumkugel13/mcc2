namespace mcc2.AST;

public class ConditionalExpression : Expression
{
    public Expression Condition, Then, Else;

    public ConditionalExpression(Expression condition, Expression thenExp, Expression elseExp)
    {
        this.Condition = condition;
        this.Then = thenExp;
        this.Else = elseExp;
    }
}