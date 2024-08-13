namespace mcc2;

public abstract record InitialValue
{
    public record Initial(int Init) : InitialValue;
    public record Tentative() : InitialValue;
    public record NoInitializer() : InitialValue;

    private InitialValue() { }
}