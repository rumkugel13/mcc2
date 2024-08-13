namespace mcc2;

public abstract record InitialValue
{
    public record Initial(StaticInit Init) : InitialValue;
    public record Tentative() : InitialValue;
    public record NoInitializer() : InitialValue;

    private InitialValue() { }
}