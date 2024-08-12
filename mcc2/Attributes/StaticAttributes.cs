namespace mcc2.Attributes;

public class StaticAttributes : IdentifierAttributes
{
    public InitialValue InitialValue;
    public bool Global;

    public StaticAttributes(InitialValue initialValue, bool global)
    {
        this.InitialValue = initialValue;
        this.Global = global;
    }
}