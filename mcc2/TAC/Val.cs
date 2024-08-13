using mcc2.AST;

namespace mcc2.TAC;

public abstract record Val
{
    public record Constant(Const Value) : Val;
    public record Variable(string Name) : Val;

    private Val() { }
}