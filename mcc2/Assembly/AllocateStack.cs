namespace mcc2.Assembly;

public class AllocateStack : Instruction
{
    public int Bytes;

    public AllocateStack(int bytes)
    {
        this.Bytes = bytes;
    }
}