namespace mcc2;

using mcc2.Assembly;

public class AssemblyGenerator
{
    private Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable;

    public AssemblyGenerator(Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable)
    {
        this.symbolTable = symbolTable;
    }

    public AssemblyProgram Generate(TAC.TACProgam program)
    {
        return GenerateProgram(program);
    }

    private AssemblyProgram GenerateProgram(TAC.TACProgam program)
    {
        List<TopLevel> functionDefinitions = [];
        foreach (var def in program.Definitions)
            if (def is TAC.TopLevel.Function fun)
                functionDefinitions.Add(GenerateFunction(fun));
            else if (def is TAC.TopLevel.StaticVariable staticVariable)
                functionDefinitions.Add(new TopLevel.StaticVariable(staticVariable.Identifier, staticVariable.Global, ((StaticInit.IntInit)staticVariable.Init).Value));
        return new AssemblyProgram(functionDefinitions);
    }

    private readonly Operand.RegisterName[] ABIRegisters = [
        Operand.RegisterName.DI,
        Operand.RegisterName.SI,
        Operand.RegisterName.DX,
        Operand.RegisterName.CX,
        Operand.RegisterName.R8,
        Operand.RegisterName.R9,
    ];

    private TopLevel.Function GenerateFunction(TAC.TopLevel.Function function)
    {
        List<Instruction> instructions = [];

        for (int i = 0; i < function.Parameters.Count; i++)
        {
            if (i < ABIRegisters.Length)
            {
                instructions.Add(new Instruction.Mov(new Operand.Reg(ABIRegisters[i]), new Operand.Pseudo(function.Parameters[i])));
            }
            else
            {
                instructions.Add(new Instruction.Mov(new Operand.Stack(16 + (i - ABIRegisters.Length) * 8), new Operand.Pseudo(function.Parameters[i])));
            }
        }
        
        TopLevel.Function fn = new TopLevel.Function(function.Name, function.Global, GenerateInstructions(function.Instructions, instructions));

        PseudoReplacer stackAllocator = new PseudoReplacer(this.symbolTable);
        var bytesToAllocate = stackAllocator.Replace(fn.Instructions);

        // note: chapter 9 says we should store this per function in symboltable or ast
        // in order to round it up but we can do that here as well
        bytesToAllocate = AlignTo(bytesToAllocate, 16);
        fn.Instructions.Insert(0, new Instruction.AllocateStack(bytesToAllocate));

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
                case TAC.Instruction.Return ret:
                    instructions.Add(new Instruction.Mov(GenerateOperand(ret.Value), new Operand.Reg(Operand.RegisterName.AX)));
                    instructions.Add(new Instruction.Ret());
                    break;
                case TAC.Instruction.Unary unary:
                    if (unary.UnaryOperator == AST.Expression.UnaryOperator.Not)
                    {
                        instructions.Add(new Instruction.Cmp(new Operand.Imm(0), GenerateOperand(unary.src)));
                        instructions.Add(new Instruction.Mov(new Operand.Imm(0), GenerateOperand(unary.dst)));
                        instructions.Add(new Instruction.SetCC(Instruction.ConditionCode.E, GenerateOperand(unary.dst)));
                    }
                    else
                    {
                        instructions.Add(new Instruction.Mov(GenerateOperand(unary.src), GenerateOperand(unary.dst)));
                        instructions.Add(new Instruction.Unary(ConvertUnary(unary.UnaryOperator), GenerateOperand(unary.dst)));
                    }
                    break;
                case TAC.Instruction.Binary binary:
                    if (binary.Operator == AST.Expression.BinaryOperator.Divide ||
                        binary.Operator == AST.Expression.BinaryOperator.Remainder)
                    {
                        instructions.Add(new Instruction.Mov(GenerateOperand(binary.Src1), new Operand.Reg(Operand.RegisterName.AX)));
                        instructions.Add(new Instruction.Cdq());
                        instructions.Add(new Instruction.Idiv(GenerateOperand(binary.Src2)));
                        var reg = new Operand.Reg(binary.Operator == AST.Expression.BinaryOperator.Divide ?
                            Operand.RegisterName.AX :
                            Operand.RegisterName.DX);
                        instructions.Add(new Instruction.Mov(reg, GenerateOperand(binary.Dst)));
                    }
                    else if(binary.Operator == AST.Expression.BinaryOperator.Equal ||
                        binary.Operator == AST.Expression.BinaryOperator.NotEqual ||
                        binary.Operator == AST.Expression.BinaryOperator.GreaterThan ||
                        binary.Operator == AST.Expression.BinaryOperator.GreaterOrEqual ||
                        binary.Operator == AST.Expression.BinaryOperator.LessThan ||
                        binary.Operator == AST.Expression.BinaryOperator.LessOrEqual)
                    {
                        instructions.Add(new Instruction.Cmp(GenerateOperand(binary.Src2), GenerateOperand(binary.Src1)));
                        instructions.Add(new Instruction.Mov(new Operand.Imm(0), GenerateOperand(binary.Dst)));
                        instructions.Add(new Instruction.SetCC(ConvertConditionCode(binary.Operator), GenerateOperand(binary.Dst)));
                    }
                    else
                    {
                        instructions.Add(new Instruction.Mov(GenerateOperand(binary.Src1), GenerateOperand(binary.Dst)));
                        instructions.Add(new Instruction.Binary(ConvertBinary(binary.Operator), GenerateOperand(binary.Src2), GenerateOperand(binary.Dst)));
                    }
                    break;
                case TAC.Instruction.Jump jump:
                    instructions.Add(new Instruction.Jmp(jump.Target));
                    break;
                case TAC.Instruction.JumpIfZero jumpZ:
                    instructions.Add(new Instruction.Cmp(new Operand.Imm(0), GenerateOperand(jumpZ.Condition)));
                    instructions.Add(new Instruction.JmpCC(Instruction.ConditionCode.E, jumpZ.Target));
                    break;
                case TAC.Instruction.JumpIfNotZero jumpZ:
                    instructions.Add(new Instruction.Cmp(new Operand.Imm(0), GenerateOperand(jumpZ.Condition)));
                    instructions.Add(new Instruction.JmpCC(Instruction.ConditionCode.NE, jumpZ.Target));
                    break;
                case TAC.Instruction.Copy copy:
                    instructions.Add(new Instruction.Mov(GenerateOperand(copy.Src), GenerateOperand(copy.Dst)));
                    break;
                case TAC.Instruction.Label label:
                    instructions.Add(new Instruction.Label(label.Identifier));
                    break;
                case TAC.Instruction.FunctionCall functionCall:
                    {
                        var registerArgs = functionCall.Arguments[0..Math.Min(functionCall.Arguments.Count, ABIRegisters.Length)];
                        var stackArgs = functionCall.Arguments[registerArgs.Count..];
                        var stackPadding = stackArgs.Count % 2 != 0 ? 8 : 0;

                        if (stackPadding != 0)
                            instructions.Add(new Instruction.AllocateStack(stackPadding));

                        // pass args on registers
                        int regIndex = 0;
                        foreach (var tackyArg in registerArgs)
                        {
                            var reg = ABIRegisters[regIndex];
                            var assemblyArg = GenerateOperand(tackyArg);
                            instructions.Add(new Instruction.Mov(assemblyArg, new Operand.Reg(reg)));
                            regIndex++;
                        }

                        // pass args on stack
                        for (int i = stackArgs.Count - 1; i >= 0; i--)
                        {
                            TAC.Val? tackyArg = stackArgs[i];
                            var assemblyArg = GenerateOperand(tackyArg);
                            if (assemblyArg is Operand.Reg or Operand.Imm)
                                instructions.Add(new Instruction.Push(assemblyArg));
                            else
                            {
                                instructions.Add(new Instruction.Mov(assemblyArg, new Operand.Reg(Operand.RegisterName.AX)));
                                instructions.Add(new Instruction.Push(new Operand.Reg(Operand.RegisterName.AX)));
                            }
                        }

                        instructions.Add(new Instruction.Call(functionCall.Identifier));

                        var bytesToRemove = 8 * stackArgs.Count + stackPadding;
                        if (bytesToRemove != 0)
                            instructions.Add(new Instruction.DeallocateStack(bytesToRemove));

                        var assemblyDst = GenerateOperand(functionCall.Dst);
                        instructions.Add(new Instruction.Mov(new Operand.Reg(Operand.RegisterName.AX), assemblyDst));
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
            TAC.Val.Constant c => new Operand.Imm(((AST.Const.ConstInt)c.Value).Value),
            TAC.Val.Variable v => new Operand.Pseudo(v.Name),
            _ => throw new NotImplementedException(),
        };
    }

    private Instruction.BinaryOperator ConvertBinary(AST.Expression.BinaryOperator binaryOperator)
    {
        return binaryOperator switch
        {
            AST.Expression.BinaryOperator.Add => Instruction.BinaryOperator.Add,
            AST.Expression.BinaryOperator.Subtract => Instruction.BinaryOperator.Sub,
            AST.Expression.BinaryOperator.Multiply => Instruction.BinaryOperator.Mult,
            _ => throw new NotImplementedException()
        };
    }

    private Instruction.ConditionCode ConvertConditionCode(AST.Expression.BinaryOperator binaryOperator)
    {
        return binaryOperator switch
        {
            AST.Expression.BinaryOperator.Equal => Instruction.ConditionCode.E,
            AST.Expression.BinaryOperator.NotEqual => Instruction.ConditionCode.NE,
            AST.Expression.BinaryOperator.GreaterThan => Instruction.ConditionCode.G,
            AST.Expression.BinaryOperator.GreaterOrEqual => Instruction.ConditionCode.GE,
            AST.Expression.BinaryOperator.LessThan => Instruction.ConditionCode.L,
            AST.Expression.BinaryOperator.LessOrEqual => Instruction.ConditionCode.LE,
             _ => throw new NotImplementedException()
        };
    }

    private Instruction.UnaryOperator ConvertUnary(AST.Expression.UnaryOperator unaryOperator)
    {
        return unaryOperator switch
        {
            AST.Expression.UnaryOperator.Complement => Instruction.UnaryOperator.Not,
            AST.Expression.UnaryOperator.Negate => Instruction.UnaryOperator.Neg,
            _ => throw new NotImplementedException()
        };
    }
}