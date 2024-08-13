namespace mcc2;

public abstract record Type
{
    public record FunctionType(int ParameterCount) : Type;
    public record Int() : Type;

    private Type() { }
}