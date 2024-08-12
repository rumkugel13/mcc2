namespace mcc2.AST;

public class ASTProgram
{
    public List<Declaration> Declarations;

    public ASTProgram(List<Declaration> declarations)
    {
        this.Declarations = declarations;
    }
}