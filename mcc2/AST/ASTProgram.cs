namespace mcc2.AST;

public class ASTProgram
{
    public FunctionDefinition Function;

    public ASTProgram(FunctionDefinition function)
    {
        this.Function = function;
    }
}