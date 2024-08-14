namespace mcc2.Assembly;

public abstract record Operand
{
    public enum RegisterName
    {
        AX,
        CX,
        DX,
        DI,
        SI,
        R8,
        R9,
        R10,
        R11,
        SP,
    }
    
    public record Imm(long Value) : Operand;
    public record Pseudo(string Identifier) : Operand;
    public record Reg(RegisterName Register) : Operand;
    public record Stack(int Offset) : Operand;
    public record Data(string Identifier) : Operand;

    private Operand() { }
}