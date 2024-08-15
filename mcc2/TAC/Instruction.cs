namespace mcc2.TAC;

public abstract record Instruction
{
    public record Return(Val Value) : Instruction;
    public record Unary(AST.Expression.UnaryOperator UnaryOperator, Val src, Val.Variable dst) : Instruction;
    public record Binary(AST.Expression.BinaryOperator Operator, Val Src1, Val Src2, Val.Variable Dst) : Instruction;
    public record Jump(string Target) : Instruction;
    public record JumpIfZero(Val Condition, string Target) : Instruction;
    public record JumpIfNotZero(Val Condition, string Target) : Instruction;
    public record Copy(Val Src, Val.Variable Dst) : Instruction;
    public record Label(string Identifier) : Instruction;
    public record FunctionCall(string Identifier, List<Val> Arguments, Val Dst) : Instruction;
    public record SignExtend(Val Src, Val Dst) : Instruction;
    public record Truncate(Val Src, Val Dst) : Instruction;
    public record ZeroExtend(Val Src, Val Dst) : Instruction;
    public record DoubleToInt(Val Src, Val Dst) : Instruction;
    public record DoubleToUInt(Val Src, Val Dst) : Instruction;
    public record IntToDouble(Val Src, Val Dst) : Instruction;
    public record UIntToDouble(Val Src, Val Dst) : Instruction;

    private Instruction() { }
}