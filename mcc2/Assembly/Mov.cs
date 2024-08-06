namespace mcc2.Assembly;

public class Mov : Instruction
{
    public Operand src, dst;

    public Mov(Operand src, Operand dst)
    {
        this.src = src;
        this.dst = dst;
    }
}