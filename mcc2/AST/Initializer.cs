namespace mcc2.AST;

public abstract record Initializer
{
    public record SingleInitializer(Expression Expression, Type Type) : Initializer;
    public record CompoundInitializer(List<Initializer> Initializers, Type Type) : Initializer;

    private Initializer() { }
}