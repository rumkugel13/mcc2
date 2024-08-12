namespace mcc2.AST;

public abstract class Declaration : BlockItem
{
    public enum StorageClasses
    {
        Static,
        Extern,
    }
}