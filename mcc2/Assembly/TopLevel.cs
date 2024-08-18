namespace mcc2.Assembly;

public abstract record TopLevel
{
    public record Function(string Name, bool Global, List<Instruction> Instructions) : TopLevel;
    public record StaticVariable(string Identifier, bool Global, int Alignment, List<StaticInit> Inits) : TopLevel;
    public record StaticConstant(string Identifier, int Alignment, StaticInit Init) : TopLevel;

    private TopLevel() { }
}