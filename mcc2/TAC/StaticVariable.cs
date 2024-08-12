namespace mcc2.TAC;

public class StaticVariable : TopLevel
{
    public string Identifier;
    public bool Global;
    public int Init;

    public StaticVariable(string identifier, bool global, int init)
    {
        this.Identifier = identifier;
        this.Global = global;
        this.Init = init;
    }
}