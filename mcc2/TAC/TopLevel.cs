namespace mcc2.TAC;

public abstract record TopLevel
{
    public record Function(string Name, bool Global, List<string> Parameters, List<Instruction> Instructions) : TopLevel;
    public record StaticVariable(string Identifier, bool Global, Type Type, List<StaticInit> Inits) : TopLevel;
    public record StaticConstant(string Identifier, Type Type, StaticInit Init) : TopLevel;

    private TopLevel() { }
}