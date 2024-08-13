using mcc2.AST;

namespace mcc2;

public class SemanticAnalyzer
{
    public struct MapEntry
    {
        public string NewName;
        public bool FromCurrentScope, HasLinkage;
    }

    public struct SymbolEntry
    {
        public Type Type;
        public IdentifierAttributes IdentifierAttributes;
    }

    public void Analyze(ASTProgram program, Dictionary<string, SymbolEntry> symbolTable)
    {
        new IdentifierResolver().Resolve(program);
        new TypeChecker().Check(program, symbolTable);
        new LoopLabeler().Label(program);
    }
}