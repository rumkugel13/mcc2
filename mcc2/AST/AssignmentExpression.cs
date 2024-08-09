namespace mcc2.AST;

public class AssignmentExpression : Expression
{
    public Expression ExpressionLeft, ExpressionRight;

    public AssignmentExpression(Expression left, Expression right)
    {
        this.ExpressionLeft = left;
        this.ExpressionRight = right;
    }
}