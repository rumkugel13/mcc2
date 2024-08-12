namespace mcc2.Attributes;

public class FunctionAttributes : IdentifierAttributes
{
    public bool Defined;
    public bool Global;

    public FunctionAttributes(bool defined, bool global)
    {
        this.Defined = defined;
        this.Global = global;
    }
}