namespace mcc2.TAC;

public class Copy : Instruction
{
    public Val Src;
    public Variable Dst;

    public Copy(Val src, Variable dst)
    {
        this.Src = src;
        this.Dst = dst;
    }
}