namespace mcc2.TAC;

public abstract record Instruction
{
    public record Return(Val Value) : Instruction;
    public record Unary(AST.UnaryExpression.UnaryOperator UnaryOperator, Val src, Val.Variable dst) : Instruction;
    public record Binary(AST.BinaryExpression.BinaryOperator Operator, Val Src1, Val Src2, Val.Variable Dst) : Instruction;
    public record Jump(string Target) : Instruction;
    public record JumpIfZero(Val Condition, string Target) : Instruction;
    public record JumpIfNotZero(Val Condition, string Target) : Instruction;
    public record Copy(Val Src, Val.Variable Dst) : Instruction;
    public record Label(string Identifier) : Instruction;
    public record FunctionCall(string Identifier, List<Val> Arguments, Val Dst) : Instruction;

    private Instruction() { }
}