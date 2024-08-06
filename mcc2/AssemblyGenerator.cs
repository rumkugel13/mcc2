namespace mcc2;

using mcc2.Assembly;
using mcc2.AST;

public class AssemblyGenerator
{
    public AssemblyProgram Generate(ASTProgram astProgram)
    {
        return GenerateProgram(astProgram);
    }

    private AssemblyProgram GenerateProgram(ASTProgram astProgram)
    {
        return new AssemblyProgram(GenerateFunction(astProgram.Function));
    }

    private Function GenerateFunction(FunctionDefinition functionDefinition)
    {
        return new Function(functionDefinition.Name, GenerateInstructions(functionDefinition.Body));
    }

    private List<Instruction> GenerateInstructions(Statement statement)
    {
        List<Instruction> instructions = [];
        switch (statement)
        {
            case ReturnStatement ret:
                instructions.Add(new Mov(GenerateOperand(ret.Expression), new Register()));
                instructions.Add(new Ret());
            break;
        }

        return instructions;
    }

    private Operand GenerateOperand(Expression expression)
    {
        switch (expression)
        {
            case ConstantExpression c:
                return new Imm(c.Value);
        }

        return new Register();
    }
}