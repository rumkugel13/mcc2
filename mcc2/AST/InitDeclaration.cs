namespace mcc2.AST;

public class InitDeclaration : ForInit
{
    public VariableDeclaration Declaration;

    public InitDeclaration(VariableDeclaration declaration)
    {
        this.Declaration = declaration;
    }
}