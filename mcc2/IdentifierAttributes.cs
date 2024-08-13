namespace mcc2;

public abstract record IdentifierAttributes
{
    public record Function(bool Defined, bool Global) : IdentifierAttributes;
    public record Local() : IdentifierAttributes;
    public record Static(InitialValue InitialValue, bool Global) : IdentifierAttributes;

    private IdentifierAttributes() { }
}