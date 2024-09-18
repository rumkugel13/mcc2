using mcc2.AST;

namespace mcc2;

public class SemanticAnalyzer
{
    public struct StructEntry
    {
        public long Alignment;
        public long Size;
        public List<MemberEntry> Members;
    }

    public record struct MemberEntry
    {
        public string MemberName;
        public Type MemberType;
        public long Offset;
    }

    public struct SymbolEntry
    {
        public Type Type;
        public IdentifierAttributes IdentifierAttributes;
    }

    public static Dictionary<string, SymbolEntry> SymbolTable = [];
    public static Dictionary<string, StructEntry> TypeTable = [];

    public void Analyze(ASTProgram program, Dictionary<string, SymbolEntry> symbolTable, Dictionary<string, StructEntry> typeTable)
    {
        SymbolTable = symbolTable;
        TypeTable = typeTable;
        new IdentifierResolver().Resolve(program);
        new LabelValidator().Validate(program);
        new TypeChecker().Check(program, symbolTable, typeTable);
        new LoopLabeler().Label(program);
    }
}