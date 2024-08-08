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
                    if (unary.UnaryOperator == AST.UnaryExpression.UnaryOperator.Not)
                    {
                        instructions.Add(new Cmp(new Imm(0), GenerateOperand(unary.src)));
                        instructions.Add(new Mov(new Imm(0), GenerateOperand(unary.dst)));
                        instructions.Add(new SetCC(JmpCC.ConditionCode.E, GenerateOperand(unary.dst)));
                    }
                    else
                    {
                        instructions.Add(new Mov(GenerateOperand(unary.src), GenerateOperand(unary.dst)));
                        instructions.Add(new Unary(ConvertUnary(unary.UnaryOperator), GenerateOperand(unary.dst)));
                    }
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
                    else if(binary.Operator == AST.BinaryExpression.BinaryOperator.Equal ||
                        binary.Operator == AST.BinaryExpression.BinaryOperator.NotEqual ||
                        binary.Operator == AST.BinaryExpression.BinaryOperator.GreaterThan ||
                        binary.Operator == AST.BinaryExpression.BinaryOperator.GreaterOrEqual ||
                        binary.Operator == AST.BinaryExpression.BinaryOperator.LessThan ||
                        binary.Operator == AST.BinaryExpression.BinaryOperator.LessOrEqual)
                    {
                        instructions.Add(new Cmp(GenerateOperand(binary.Src2), GenerateOperand(binary.Src1)));
                        instructions.Add(new Mov(new Imm(0), GenerateOperand(binary.Dst)));
                        instructions.Add(new SetCC(ConvertConditionCode(binary.Operator), GenerateOperand(binary.Dst)));
                    }
                    else
                    {
                        instructions.Add(new Mov(GenerateOperand(binary.Src1), GenerateOperand(binary.Dst)));
                        instructions.Add(new Binary(ConvertBinary(binary.Operator), GenerateOperand(binary.Src2), GenerateOperand(binary.Dst)));
                    }
                    break;
                case TAC.Jump jump:
                    instructions.Add(new Jmp(jump.Identifier));
                    break;
                case TAC.JumpIfZero jumpZ:
                    instructions.Add(new Cmp(new Imm(0), GenerateOperand(jumpZ.Condition)));
                    instructions.Add(new JmpCC(JmpCC.ConditionCode.E, jumpZ.Target));
                    break;
                case TAC.JumpIfNotZero jumpZ:
                    instructions.Add(new Cmp(new Imm(0), GenerateOperand(jumpZ.Condition)));
                    instructions.Add(new JmpCC(JmpCC.ConditionCode.NE, jumpZ.Target));
                    break;
                case TAC.Copy copy:
                    instructions.Add(new Mov(GenerateOperand(copy.Src), GenerateOperand(copy.Dst)));
                    break;
                case TAC.Label label:
                    instructions.Add(new Label(label.Identifier));
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

    private JmpCC.ConditionCode ConvertConditionCode(AST.BinaryExpression.BinaryOperator binaryOperator)
    {
        return binaryOperator switch
        {
            AST.BinaryExpression.BinaryOperator.Equal => JmpCC.ConditionCode.E,
            AST.BinaryExpression.BinaryOperator.NotEqual => JmpCC.ConditionCode.NE,
            AST.BinaryExpression.BinaryOperator.GreaterThan => JmpCC.ConditionCode.G,
            AST.BinaryExpression.BinaryOperator.GreaterOrEqual => JmpCC.ConditionCode.GE,
            AST.BinaryExpression.BinaryOperator.LessThan => JmpCC.ConditionCode.L,
            AST.BinaryExpression.BinaryOperator.LessOrEqual => JmpCC.ConditionCode.LE,
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