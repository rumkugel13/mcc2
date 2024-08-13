namespace mcc2.Assembly;

public abstract record Instruction
{
    public enum UnaryOperator
    {
        Not,
        Neg,
    }

    public enum BinaryOperator
    {
        Add,
        Sub,
        Mult,
    }

    public enum ConditionCode
    {
        E,
        NE,
        G,
        GE,
        L,
        LE
    }

    public record Mov(Operand Src, Operand Dst) : Instruction;
    public record Unary(UnaryOperator Operator, Operand Operand) : Instruction;
    public record Binary(BinaryOperator Operator, Operand SrcOperand, Operand DstOperand) : Instruction;
    public record Cmp(Operand OperandA, Operand OperandB) : Instruction;
    public record Idiv(Operand Operand) : Instruction;
    public record Cdq : Instruction;
    public record Jmp(string Identifier) : Instruction;
    public record JmpCC(ConditionCode Condition, string Identifier) : Instruction;
    public record SetCC(ConditionCode Condition, Operand Operand) : Instruction;
    public record Label(string Identifier) : Instruction;
    public record AllocateStack(int Bytes) : Instruction;
    public record DeallocateStack(int Bytes) : Instruction;
    public record Push(Operand Operand) : Instruction;
    public record Call(string Identifier) : Instruction;
    public record Ret() : Instruction;

    private Instruction() { }
}