namespace mcc2.Assembly;

public class Unary : Instruction
{
    public enum UnaryOperator
    {
        Not,
        Neg,
    }

    public UnaryOperator Operator;
    public Operand Operand;

    public Unary(UnaryOperator op, Operand operand)
    {
        this.Operator = op;
        this.Operand = operand;
    }
}