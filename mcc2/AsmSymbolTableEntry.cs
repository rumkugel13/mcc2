using static mcc2.Assembly.Instruction;

namespace mcc2;

public abstract record AsmSymbolTableEntry
{
    public record ObjectEntry(AssemblyType AssemblyType, bool IsStatic) : AsmSymbolTableEntry;
    public record FunctionEntry(bool Defined) : AsmSymbolTableEntry;

    private AsmSymbolTableEntry() { }
}