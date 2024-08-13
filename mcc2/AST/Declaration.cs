namespace mcc2.AST;

public abstract record Declaration : BlockItem
{
    public enum StorageClasses
    {
        Static,
        Extern,
    }

    public record FunctionDeclaration(string Identifier, List<string> Parameters, Block? Body, StorageClasses? StorageClass) : Declaration;
    public record VariableDeclaration(string Identifier, Expression? Initializer, StorageClasses? StorageClass) : Declaration;

    private Declaration() { }
}