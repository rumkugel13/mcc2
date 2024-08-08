namespace mcc2.Assembly;

public class Binary : Instruction
{
    public enum BinaryOperator
    {
        Add,
        Sub,
        Mult,
    }

    public BinaryOperator Operator;
    public Operand SrcOperand, DstOperand;

    public Binary(BinaryOperator binaryOperator, Operand src, Operand dst)
    {
        this.Operator = binaryOperator;
        this.SrcOperand = src;
        this.DstOperand = dst;
    }
}