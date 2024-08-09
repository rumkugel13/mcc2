namespace mcc2.AST;

public class InitDeclaration : ForInit
{
    public Declaration Declaration;

    public InitDeclaration(Declaration declaration)
    {
        this.Declaration = declaration;
    }
}