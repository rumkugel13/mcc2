namespace mcc2.AST;

public abstract record Declarator
{
    public record IdentifierDeclarator(string Identifier) : Declarator;
    public record PointerDeclarator(Declarator Declarator) : Declarator;
    public record FunctionDeclarator(List<ParameterInfo> Parameters, Declarator Declarator) : Declarator;
    public record ArrayDeclarator(Declarator Declarator, long Size) : Declarator;

    private Declarator() { }
}