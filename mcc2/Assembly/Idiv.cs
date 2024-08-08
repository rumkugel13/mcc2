namespace mcc2.Assembly;

public class Idiv : Instruction
{
    public Operand Operand;

    public Idiv(Operand operand)
    {
        this.Operand = operand;
    }
}