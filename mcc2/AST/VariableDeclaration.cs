namespace mcc2.AST;

public class VariableDeclaration : Declaration
{
    public string Identifier;
    public Expression? Initializer;
    public StorageClasses? StorageClass;

    public VariableDeclaration(string identifier, Expression? init, StorageClasses? storageClass)
    {
        this.Identifier = identifier;
        this.Initializer = init;
        this.StorageClass = storageClass;
    }
}