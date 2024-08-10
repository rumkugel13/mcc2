namespace mcc2.AST;

public class ASTProgram
{
    public List<FunctionDeclaration> FunctionDeclarations;

    public ASTProgram(List<FunctionDeclaration> functionDeclarations)
    {
        this.FunctionDeclarations = functionDeclarations;
    }
}