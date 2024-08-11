namespace mcc2.Assembly;

public class DeallocateStack : Instruction
{
    public int Bytes;

    public DeallocateStack(int bytes)
    {
        this.Bytes = bytes;
    }
}