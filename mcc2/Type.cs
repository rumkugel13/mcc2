namespace mcc2;

public abstract record Type
{
    public record Int() : Type;
    public record Long() : Type;
    public record FunctionType(List<Type> Parameters, Type Return) : Type;

    private Type() { }
}