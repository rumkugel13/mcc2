namespace mcc2.Assembly;

public class Stack : Operand
{
    public int Offset;

    public Stack(int offset)
    {
        this.Offset = offset;
    }
}