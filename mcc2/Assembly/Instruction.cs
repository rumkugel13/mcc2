namespace mcc2.Assembly;

public abstract record Instruction
{
    public enum UnaryOperator
    {
        Not,
        Neg,
        Shr,
    }

    public enum BinaryOperator
    {
        Add,
        Sub,
        Mult,
        DivDouble,
        And,
        Or,
        Xor,
    }

    public enum ConditionCode
    {
        E,
        NE,
        G,
        GE,
        L,
        LE,
        A,
        AE,
        B,
        BE,
    }

    public record Mov(AssemblyType Type, Operand Src, Operand Dst) : Instruction;
    public record Movsx(Operand Src, Operand Dst) : Instruction;
    public record MovZeroExtend(Operand Src, Operand Dst) : Instruction;
    public record Lea(Operand Src, Operand Dst) : Instruction;
    public record Cvttsd2si(AssemblyType DstType, Operand Src, Operand Dst) : Instruction;
    public record Cvtsi2sd(AssemblyType SrcType, Operand Src, Operand Dst) : Instruction;
    public record Unary(UnaryOperator Operator, AssemblyType Type, Operand Operand) : Instruction;
    public record Binary(BinaryOperator Operator, AssemblyType Type, Operand SrcOperand, Operand DstOperand) : Instruction;
    public record Cmp(AssemblyType Type, Operand OperandA, Operand OperandB) : Instruction;
    public record Idiv(AssemblyType Type, Operand Operand) : Instruction;
    public record Div(AssemblyType Type, Operand Operand) : Instruction;
    public record Cdq(AssemblyType Type) : Instruction;
    public record Jmp(string Identifier) : Instruction;
    public record JmpCC(ConditionCode Condition, string Identifier) : Instruction;
    public record SetCC(ConditionCode Condition, Operand Operand) : Instruction;
    public record Label(string Identifier) : Instruction;
    public record Push(Operand Operand) : Instruction;
    public record Call(string Identifier) : Instruction;
    public record Ret() : Instruction;

    private Instruction() { }
}