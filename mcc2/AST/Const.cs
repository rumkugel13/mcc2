namespace mcc2.AST;

public abstract record Const
{
    public record ConstInt(int Value) : Const;
    public record ConstLong(long Value) : Const;
}