namespace mcc2.Assembly;

public class Imm : Operand
{
    public int Value;

    public Imm(int value)
    {
        this.Value = value;
    }
}