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
                {
                    var src = EmitInstruction(unary.Expression, instructions);
                    var dstName = MakeTemporary();
                    var dst = new Variable(dstName);
                    instructions.Add(new Unary(unary.Operator, src, dst));
                    return dst;
                }
            case BinaryExpression binary:
                {
                    if (binary.Operator == BinaryExpression.BinaryOperator.And || 
                        binary.Operator == BinaryExpression.BinaryOperator.Or)
                    {
                        return EmitShortCurcuit(binary, instructions);
                    }

                    var v1 = EmitInstruction(binary.ExpressionLeft, instructions);
                    var v2 = EmitInstruction(binary.ExpressionRight, instructions);
                    var dstName = MakeTemporary();
                    var dst = new Variable(dstName);
                    instructions.Add(new Binary(binary.Operator, v1, v2, dst));
                    return dst;
                }
            default:
                throw new NotImplementedException();
        }
    }

    private Val EmitShortCurcuit(BinaryExpression binary, List<Instruction> instructions)
    {
        var dstName = MakeTemporary();
        var dst = new Variable(dstName);

        if (binary.Operator == BinaryExpression.BinaryOperator.And)
        {
            var v1 = EmitInstruction(binary.ExpressionLeft, instructions);
            var falseLabel = MakeLabel();
            instructions.Add(new JumpIfZero(v1, falseLabel));
            var v2 = EmitInstruction(binary.ExpressionRight, instructions);
            instructions.Add(new JumpIfZero(v2, falseLabel));
            instructions.Add(new Copy(new Constant(1), dst));
            var endLabel = MakeLabel();
            instructions.Add(new Jump(endLabel));
            instructions.Add(new Label(falseLabel));
            instructions.Add(new Copy(new Constant(0), dst));
            instructions.Add(new Label(endLabel));
        }
        else
        {
            var v1 = EmitInstruction(binary.ExpressionLeft, instructions);
            var trueLabel = MakeLabel();
            instructions.Add(new JumpIfNotZero(v1, trueLabel));
            var v2 = EmitInstruction(binary.ExpressionRight, instructions);
            instructions.Add(new JumpIfNotZero(v2, trueLabel));
            instructions.Add(new Copy(new Constant(0), dst));
            var endLabel = MakeLabel();
            instructions.Add(new Jump(endLabel));
            instructions.Add(new Label(trueLabel));
            instructions.Add(new Copy(new Constant(1), dst));
            instructions.Add(new Label(endLabel));
        }
        return dst;
    }

    private string MakeTemporary()
    {
        // todo: more descriptive names, e.g. function name
        return $"tmp.{counter++}";
    }

    private string MakeLabel()
    {
         return $"jmp.{counter++}";
    }
}