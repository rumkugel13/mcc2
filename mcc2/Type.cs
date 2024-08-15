namespace mcc2;

public abstract record Type
{
    private record Null() : Type;
    public record Int() : Type;
    public record Long() : Type;
    public record UInt() : Type;
    public record ULong() : Type;
    public record Double() : Type;
    public record FunctionType(List<Type> Parameters, Type Return) : Type;

    public static Type None => new Null();
    private Type() { }
}