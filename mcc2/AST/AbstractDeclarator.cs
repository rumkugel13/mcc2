namespace mcc2.AST;

public abstract record AbstractDeclarator
{
    public record AbstractBase() : AbstractDeclarator;
    public record AbstractPointer(AbstractDeclarator AbstractDeclarator) : AbstractDeclarator;

    private AbstractDeclarator() { }
}