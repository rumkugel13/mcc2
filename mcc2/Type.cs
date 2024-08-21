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
    public record Pointer(Type Referenced) : Type;
    public record Array(Type Element, long Size) : Type;
    public record Char() : Type;
    public record SChar() : Type;
    public record UChar() : Type;
    public record Void() : Type;

    public static Type None => new Null();
    private Type() { }
}