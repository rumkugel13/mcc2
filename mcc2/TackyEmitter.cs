namespace mcc2;

using mcc2.AST;
using mcc2.TAC;

public class TackyEmitter
{
    private uint counter;

    public TACProgam Emit(ASTProgram astProgram)
    {
        return EmitProgram(astProgram);
    }

    private TACProgam EmitProgram(ASTProgram astProgram)
    {
        return new TACProgam(EmitFunction(astProgram.Function));
    }

    private Function EmitFunction(FunctionDefinition functionDefinition)
    {
        List<Instruction> instructions = [];
        EmitInstruction(functionDefinition.Body, instructions);
        return new Function(functionDefinition.Name, instructions);
    }

    private void EmitInstruction(Statement statement, List<Instruction> instructions)
    {
        switch (statement)
        {
            case ReturnStatement returnStatement:
                var val = EmitInstruction(returnStatement.Expression, instructions);
                instructions.Add(new Return(val));
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private Val EmitInstruction(Expression expression, List<Instruction> instructions)
    {
        switch (expression)
        {
            case ConstantExpression constant:
                return new Constant(constant.Value);
            case UnaryExpression unary:
                var src = EmitInstruction(unary.Expression, instructions);
                var dstName = MakeTemporary();
                var dst = new Variable(dstName);
                instructions.Add(new Unary(unary.Operator, src, dst));
                return dst;
            default:
                throw new NotImplementedException();
        }
    }

    private string MakeTemporary()
    {
        // todo: more descriptive names, e.g. function name
        return $"tmp.{counter++}";
    }
}