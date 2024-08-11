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
        List<Function> functionDefinitions = [];
        foreach (var fun in program.FunctionDefinitions)
            functionDefinitions.Add(GenerateFunction(fun));
        return new AssemblyProgram(functionDefinitions);
    }

    private readonly Reg.RegisterName[] ABIRegisters = [
        Reg.RegisterName.DI,
        Reg.RegisterName.SI,
        Reg.RegisterName.DX,
        Reg.RegisterName.CX,
        Reg.RegisterName.R8,
        Reg.RegisterName.R9,
    ];

    private Function GenerateFunction(TAC.Function function)
    {
        List<Instruction> instructions = [];

        for (int i = 0; i < function.Parameters.Count; i++)
        {
            if (i < ABIRegisters.Length)
            {
                instructions.Add(new Mov(new Reg(ABIRegisters[i]), new Pseudo(function.Parameters[i])));
            }
            else
            {
                instructions.Add(new Mov(new Stack(16 + (i - ABIRegisters.Length) * 8), new Pseudo(function.Parameters[i])));
            }
        }
        
        Function fn = new Function(function.Name, GenerateInstructions(function.Instructions, instructions));

        PseudoReplacer stackAllocator = new PseudoReplacer();
        var bytesToAllocate = stackAllocator.Replace(fn.Instructions);

        // note: chapter 9 says we should store this per function in symboltable or ast
        // in order to round it up but we can do that here as well
        bytesToAllocate = AlignTo(bytesToAllocate, 16);
        fn.Instructions.Insert(0, new AllocateStack(bytesToAllocate));

        InstructionFixer instructionFixer = new InstructionFixer();
        instructionFixer.Fix(fn.Instructions);

        return fn;
    }

    private int AlignTo(int bytes, int align)
    {
        return align * ((bytes + align - 1) / align);
    }

    private List<Instruction> GenerateInstructions(List<TAC.Instruction> tacInstructions, List<Instruction> instructions)
    {
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
                case TAC.FunctionCall functionCall:
                    {
                        var registerArgs = functionCall.Arguments[0..Math.Min(functionCall.Arguments.Count, ABIRegisters.Length)];
                        var stackArgs = functionCall.Arguments[registerArgs.Count..];
                        var stackPadding = stackArgs.Count % 2 != 0 ? 8 : 0;

                        if (stackPadding != 0)
                            instructions.Add(new AllocateStack(stackPadding));

                        // pass args on registers
                        int regIndex = 0;
                        foreach (var tackyArg in registerArgs)
                        {
                            var reg = ABIRegisters[regIndex];
                            var assemblyArg = GenerateOperand(tackyArg);
                            instructions.Add(new Mov(assemblyArg, new Reg(reg)));
                            regIndex++;
                        }

                        // pass args on stack
                        for (int i = stackArgs.Count - 1; i >= 0; i--)
                        {
                            TAC.Val? tackyArg = stackArgs[i];
                            var assemblyArg = GenerateOperand(tackyArg);
                            if (assemblyArg is Reg or Imm)
                                instructions.Add(new Push(assemblyArg));
                            else
                            {
                                instructions.Add(new Mov(assemblyArg, new Reg(Reg.RegisterName.AX)));
                                instructions.Add(new Push(new Reg(Reg.RegisterName.AX)));
                            }
                        }

                        instructions.Add(new Call(functionCall.Identifier));

                        var bytesToRemove = 8 * stackArgs.Count + stackPadding;
                        if (bytesToRemove != 0)
                            instructions.Add(new DeallocateStack(bytesToRemove));

                        var assemblyDst = GenerateOperand(functionCall.Dst);
                        instructions.Add(new Mov(new Reg(Reg.RegisterName.AX), assemblyDst));
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