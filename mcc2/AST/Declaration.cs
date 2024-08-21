namespace mcc2.AST;

public abstract record Declaration : BlockItem
{
    public enum StorageClasses
    {
        Static,
        Extern,
    }

    public record FunctionDeclaration(string Identifier, List<string> Parameters, Block? Body, Type FunctionType, StorageClasses? StorageClass) : Declaration;
    public record VariableDeclaration(string Identifier, Initializer? Initializer, Type VariableType, StorageClasses? StorageClass) : Declaration;
    public record StructDeclaration(string Identifier, List<MemberDeclaration> Members) : Declaration;

    private Declaration() { }
}

public record MemberDeclaration(string MemberName, Type MemberType);