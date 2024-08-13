namespace mcc2.Assembly;

public abstract record TopLevel
{
    public record Function(string Name, bool Global, List<Instruction> Instructions) : TopLevel;
    public record StaticVariable(string Identifier, bool Global, int Init) : TopLevel;

    private TopLevel() { }
}