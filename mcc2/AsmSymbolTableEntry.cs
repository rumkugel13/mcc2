namespace mcc2;

public abstract record AsmSymbolTableEntry
{
    public record ObjectEntry(Assembly.AssemblyType AssemblyType, bool IsStatic, bool IsConstant) : AsmSymbolTableEntry;
    public record FunctionEntry(bool Defined, bool ReturnOnStack, List<Assembly.Operand.RegisterName> ParamRegisters, List<Assembly.Operand.RegisterName> ReturnRegisters) : AsmSymbolTableEntry;

    private AsmSymbolTableEntry() { }
}