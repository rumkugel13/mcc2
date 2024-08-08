namespace mcc2.Assembly;

public class Cmp : Instruction
{
    public Operand OperandA, OperandB;

    public Cmp(Operand a, Operand b)
    {
        this.OperandA = a;
        this.OperandB = b;
    }
}