namespace mcc2.Attributes;

public abstract record IdentifierAttributes
{
    public record FunctionAttributes(bool Defined, bool Global) : IdentifierAttributes;
    public record LocalAttributes() : IdentifierAttributes;
    public record StaticAttributes(InitialValue InitialValue, bool Global) : IdentifierAttributes;

    private IdentifierAttributes() { }
}