namespace mcc2.AST;

public abstract record Declaration : BlockItem
{
    public enum StorageClasses
    {
        Static,
        Extern,
    }

    public record FunctionDeclaration(string Identifier, List<string> Parameters, Block? Body, Type FunctionType, StorageClasses? StorageClass) : Declaration;
    public record VariableDeclaration(string Identifier, Expression? Initializer, Type VariableType, StorageClasses? StorageClass) : Declaration;

    private Declaration() { }
}