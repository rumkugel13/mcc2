namespace mcc2;

using mcc2.Assembly;

public class AssemblyGenerator
{
    public AssemblyProgram Generate(TAC.TACProgam program)
    {
        return GenerateProgram(program);
    }

    private AssemblyProgram GenerateProgram(TAC.TACProgam program)
    {
        return new AssemblyProgram(GenerateFunction(program.FunctionDefinition));
    }

    private Function GenerateFunction(TAC.Function function)
    {
        Function fn = new Function(function.Name, GenerateInstructions(function.Instructions));

        PseudoReplacer stackAllocator = new PseudoReplacer();
        var bytes = stackAllocator.Replace(fn.Instructions);

        fn.Instructions.Insert(0, new AllocateStack(bytes));

        InstructionFixer instructionFixer = new InstructionFixer();
        instructionFixer.Fix(fn.Instructions);

        return fn;
    }

    private List<Instruction> GenerateInstructions(List<TAC.Instruction> tacInstructions)
    {
        List<Instruction> instructions = [];
        foreach (var inst in tacInstructions)
        {
            switch (inst)
            {
                case TAC.Return ret:
                    instructions.Add(new Mov(GenerateOperand(ret.Value), new Reg(Reg.RegisterName.AX)));
                    instructions.Add(new Ret());
                    break;
                case TAC.Unary unary:
                    instructions.Add(new Mov(GenerateOperand(unary.src), GenerateOperand(unary.dst)));
                    instructions.Add(new Unary(ConvertUnary(unary.UnaryOperator), GenerateOperand(unary.dst)));
                    break;
                case TAC.Binary binary:
                    if (binary.Operator == AST.BinaryExpression.BinaryOperator.Divide ||
                        binary.Operator == AST.BinaryExpression.BinaryOperator.Remainder)
                    {
                        instructions.Add(new Mov(GenerateOperand(binary.Src1), new Reg(Reg.RegisterName.AX)));
                        instructions.Add(new Cdq());
                        instructions.Add(new Idiv(GenerateOperand(binary.Src2)));
                        var reg = new Reg(binary.Operator == AST.BinaryExpression.BinaryOperator.Divide ?
                            Reg.RegisterName.AX :
                            Reg.RegisterName.DX);
                        instructions.Add(new Mov(reg, GenerateOperand(binary.Dst)));
                    }
                    else
                    {
                        instructions.Add(new Mov(GenerateOperand(binary.Src1), GenerateOperand(binary.Dst)));
                        instructions.Add(new Binary(ConvertBinary(binary.Operator), GenerateOperand(binary.Src2), GenerateOperand(binary.Dst)));
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        return instructions;
    }

    private Operand GenerateOperand(TAC.Val val)
    {
        return val switch
        {
            TAC.Constant c => new Imm(c.Value),
            TAC.Variable v => new Pseudo(v.Name),
            _ => throw new NotImplementedException(),
        };
    }

    private Binary.BinaryOperator ConvertBinary(AST.BinaryExpression.BinaryOperator binaryOperator)
    {
        return binaryOperator switch
        {
            AST.BinaryExpression.BinaryOperator.Add => Binary.BinaryOperator.Add,
            AST.BinaryExpression.BinaryOperator.Subtract => Binary.BinaryOperator.Sub,
            AST.BinaryExpression.BinaryOperator.Multiply => Binary.BinaryOperator.Mult,
            _ => throw new NotImplementedException()
        };
    }

    private Unary.UnaryOperator ConvertUnary(AST.UnaryExpression.UnaryOperator unaryOperator)
    {
        return unaryOperator switch
        {
            AST.UnaryExpression.UnaryOperator.Complement => Unary.UnaryOperator.Not,
            AST.UnaryExpression.UnaryOperator.Negate => Unary.UnaryOperator.Neg,
            _ => throw new NotImplementedException()
        };
    }
}