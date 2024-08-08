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
                    instructions.Add(new Unary(Convert(unary.UnaryOperator), GenerateOperand(unary.dst)));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        return instructions;
    }

    private Operand GenerateOperand(TAC.Val val)
    {
        switch (val)
        {
            case TAC.Constant c:
                return new Imm(c.Value);
            case TAC.Variable v:
                return new Pseudo(v.Name);
            default:
                throw new NotImplementedException();
        }
    }

    private Unary.UnaryOperator Convert(AST.UnaryExpression.UnaryOperator unaryOperator)
    {
        return unaryOperator switch {
            AST.UnaryExpression.UnaryOperator.Complement => Unary.UnaryOperator.Not,
            AST.UnaryExpression.UnaryOperator.Negate => Unary.UnaryOperator.Neg,
            _ => throw new NotImplementedException()
        };
    }
}