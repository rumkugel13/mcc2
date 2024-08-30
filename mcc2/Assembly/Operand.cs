namespace mcc2.Assembly;

public abstract record Operand
{
    public enum RegisterName
    {
        AX,
        BX,
        CX,
        DX,
        DI,
        SI,
        R8,
        R9,
        R10,
        R11,
        R12,
        R13,
        R14,
        R15,
        SP,
        BP,
        XMM0,
        XMM1,
        XMM2,
        XMM3,
        XMM4,
        XMM5,
        XMM6,
        XMM7,
        XMM8,
        XMM9,
        XMM10,
        XMM11,
        XMM12,
        XMM13,
        XMM14,
        XMM15,        
    }

    public enum ClassType
    {
        Memory,
        SSE,
        Integer,
    }
    
    public record Imm(ulong Value) : Operand;
    public record Pseudo(string Identifier) : Operand;
    public record Reg(RegisterName Register) : Operand;
    public record Memory(RegisterName Register, long Offset) : Operand;
    public record Data(string Identifier, long Offset) : Operand;
    public record Indexed(RegisterName Base, RegisterName Index, long Scale) : Operand;
    public record PseudoMemory(string Identifier, long Offset) : Operand;

    private Operand() { }
}