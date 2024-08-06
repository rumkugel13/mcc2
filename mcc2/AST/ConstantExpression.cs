namespace mcc2.AST;

public class ConstantExpression : Expression
{
    public int Value;

    public ConstantExpression(int value)
    {
        this.Value = value;
    }
}