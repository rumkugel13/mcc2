namespace mcc2.Assembly;

public class Push : Instruction
{
    public Operand Operand;

    public Push(Operand operand)
    {
        this.Operand = operand;
    }
}