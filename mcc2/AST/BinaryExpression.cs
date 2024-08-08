namespace mcc2.AST;

public class BinaryExpression : Expression
{
    public enum BinaryOperator
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Remainder,
    }

    public BinaryOperator Operator;
    public Expression ExpressionLeft, ExpressionRight;

    public BinaryExpression(BinaryOperator binaryOperator, Expression left, Expression right)
    {
        this.Operator = binaryOperator;
        this.ExpressionLeft = left;
        this.ExpressionRight = right;
    }
}