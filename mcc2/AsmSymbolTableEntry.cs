namespace mcc2;

public abstract record AsmSymbolTableEntry
{
    public record ObjectEntry(Assembly.AssemblyType AssemblyType, bool IsStatic, bool IsConstant) : AsmSymbolTableEntry;
    public record FunctionEntry(bool Defined) : AsmSymbolTableEntry;

    private AsmSymbolTableEntry() { }
}