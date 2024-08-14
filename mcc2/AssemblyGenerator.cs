namespace mcc2;

using mcc2.Assembly;

public class AssemblyGenerator
{
    public static Dictionary<string, AsmSymbolTableEntry> AsmSymbolTable = [];

    private Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable;

    public AssemblyGenerator(Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable)
    {
        this.symbolTable = symbolTable;

        foreach (var entry in symbolTable)
        {
            if (entry.Value.IdentifierAttributes is IdentifierAttributes.Function funAttr)
            {
                AsmSymbolTable[entry.Key] = new AsmSymbolTableEntry.FunctionEntry(funAttr.Defined);
            }
            else
            {
                AsmSymbolTable[entry.Key] = new AsmSymbolTableEntry.ObjectEntry(GetAssemblyType(entry.Value.Type), entry.Value.IdentifierAttributes is IdentifierAttributes.Static);
            }
        }
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
                functionDefinitions.Add(new TopLevel.StaticVariable(staticVariable.Identifier, staticVariable.Global,
                    GetAlignment(staticVariable.Type), staticVariable.Init));
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
            Instruction.AssemblyType assemblyType = GetAssemblyType(((Type.FunctionType)symbolTable[function.Name].Type).Parameters[i]);
            if (i < ABIRegisters.Length)
            {
                instructions.Add(new Instruction.Mov(assemblyType, new Operand.Reg(ABIRegisters[i]), new Operand.Pseudo(function.Parameters[i])));
            }
            else
            {
                instructions.Add(new Instruction.Mov(assemblyType, new Operand.Stack(16 + (i - ABIRegisters.Length) * 8), new Operand.Pseudo(function.Parameters[i])));
            }
        }

        TopLevel.Function fn = new TopLevel.Function(function.Name, function.Global, GenerateInstructions(function.Instructions, instructions));

        PseudoReplacer stackAllocator = new PseudoReplacer();
        var bytesToAllocate = stackAllocator.Replace(fn.Instructions);

        // note: chapter 9 says we should store this per function in symboltable or ast
        // in order to round it up but we can do that here as well
        bytesToAllocate = AlignTo(bytesToAllocate, 16);
        fn.Instructions.Insert(0, new Instruction.Binary(Instruction.BinaryOperator.Sub, Instruction.AssemblyType.Quadword,
            new Operand.Imm(bytesToAllocate), new Operand.Reg(Operand.RegisterName.SP)));

        InstructionFixer instructionFixer = new InstructionFixer();
        instructionFixer.Fix(fn.Instructions);

        return fn;
    }

    public static int AlignTo(int bytes, int align)
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
                    instructions.Add(new Instruction.Mov(GetAssemblyType(ret.Value), GenerateOperand(ret.Value), new Operand.Reg(Operand.RegisterName.AX)));
                    instructions.Add(new Instruction.Ret());
                    break;
                case TAC.Instruction.Unary unary:
                    if (unary.UnaryOperator == AST.Expression.UnaryOperator.Not)
                    {
                        instructions.Add(new Instruction.Cmp(GetAssemblyType(unary.src), new Operand.Imm(0), GenerateOperand(unary.src)));
                        instructions.Add(new Instruction.Mov(GetAssemblyType(unary.dst), new Operand.Imm(0), GenerateOperand(unary.dst)));
                        instructions.Add(new Instruction.SetCC(Instruction.ConditionCode.E, GenerateOperand(unary.dst)));
                    }
                    else
                    {
                        instructions.Add(new Instruction.Mov(GetAssemblyType(unary.src), GenerateOperand(unary.src), GenerateOperand(unary.dst)));
                        instructions.Add(new Instruction.Unary(ConvertUnary(unary.UnaryOperator), GetAssemblyType(unary.src), GenerateOperand(unary.dst)));
                    }
                    break;
                case TAC.Instruction.Binary binary:
                    if (binary.Operator == AST.Expression.BinaryOperator.Divide ||
                        binary.Operator == AST.Expression.BinaryOperator.Remainder)
                    {
                        instructions.Add(new Instruction.Mov(GetAssemblyType(binary.Src1), GenerateOperand(binary.Src1), new Operand.Reg(Operand.RegisterName.AX)));
                        if (IsSignedType(binary.Src1))
                        {
                            instructions.Add(new Instruction.Cdq(GetAssemblyType(binary.Src1)));
                            instructions.Add(new Instruction.Idiv(GetAssemblyType(binary.Src1), GenerateOperand(binary.Src2)));
                        }
                        else
                        {
                            instructions.Add(new Instruction.Mov(GetAssemblyType(binary.Src1), new Operand.Imm(0), new Operand.Reg(Operand.RegisterName.DX)));
                            instructions.Add(new Instruction.Div(GetAssemblyType(binary.Src1), GenerateOperand(binary.Src2)));
                        }
                        var reg = new Operand.Reg(binary.Operator == AST.Expression.BinaryOperator.Divide ? Operand.RegisterName.AX : Operand.RegisterName.DX);
                        instructions.Add(new Instruction.Mov(GetAssemblyType(binary.Src1), reg, GenerateOperand(binary.Dst)));
                    }
                    else if (binary.Operator == AST.Expression.BinaryOperator.Equal ||
                        binary.Operator == AST.Expression.BinaryOperator.NotEqual ||
                        binary.Operator == AST.Expression.BinaryOperator.GreaterThan ||
                        binary.Operator == AST.Expression.BinaryOperator.GreaterOrEqual ||
                        binary.Operator == AST.Expression.BinaryOperator.LessThan ||
                        binary.Operator == AST.Expression.BinaryOperator.LessOrEqual)
                    {
                        instructions.Add(new Instruction.Cmp(GetAssemblyType(binary.Src1), GenerateOperand(binary.Src2), GenerateOperand(binary.Src1)));
                        instructions.Add(new Instruction.Mov(GetAssemblyType(binary.Dst), new Operand.Imm(0), GenerateOperand(binary.Dst)));
                        instructions.Add(new Instruction.SetCC(ConvertConditionCode(binary.Operator, IsSignedType(binary.Src1)), GenerateOperand(binary.Dst)));
                    }
                    else
                    {
                        instructions.Add(new Instruction.Mov(GetAssemblyType(binary.Src1), GenerateOperand(binary.Src1), GenerateOperand(binary.Dst)));
                        instructions.Add(new Instruction.Binary(ConvertBinary(binary.Operator), GetAssemblyType(binary.Src1), GenerateOperand(binary.Src2), GenerateOperand(binary.Dst)));
                    }
                    break;
                case TAC.Instruction.Jump jump:
                    instructions.Add(new Instruction.Jmp(jump.Target));
                    break;
                case TAC.Instruction.JumpIfZero jumpZ:
                    instructions.Add(new Instruction.Cmp(GetAssemblyType(jumpZ.Condition), new Operand.Imm(0), GenerateOperand(jumpZ.Condition)));
                    instructions.Add(new Instruction.JmpCC(Instruction.ConditionCode.E, jumpZ.Target));
                    break;
                case TAC.Instruction.JumpIfNotZero jumpNZ:
                    instructions.Add(new Instruction.Cmp(GetAssemblyType(jumpNZ.Condition), new Operand.Imm(0), GenerateOperand(jumpNZ.Condition)));
                    instructions.Add(new Instruction.JmpCC(Instruction.ConditionCode.NE, jumpNZ.Target));
                    break;
                case TAC.Instruction.Copy copy:
                    instructions.Add(new Instruction.Mov(GetAssemblyType(copy.Src), GenerateOperand(copy.Src), GenerateOperand(copy.Dst)));
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
                            instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.Sub, Instruction.AssemblyType.Quadword,
                                new Operand.Imm(stackPadding), new Operand.Reg(Operand.RegisterName.SP)));

                        // pass args on registers
                        int regIndex = 0;
                        foreach (var tackyArg in registerArgs)
                        {
                            var reg = ABIRegisters[regIndex];
                            var assemblyArg = GenerateOperand(tackyArg);
                            instructions.Add(new Instruction.Mov(GetAssemblyType(tackyArg), assemblyArg, new Operand.Reg(reg)));
                            regIndex++;
                        }

                        // pass args on stack
                        for (int i = stackArgs.Count - 1; i >= 0; i--)
                        {
                            TAC.Val? tackyArg = stackArgs[i];
                            var assemblyArg = GenerateOperand(tackyArg);
                            if (assemblyArg is Operand.Reg or Operand.Imm || GetAssemblyType(tackyArg) == Instruction.AssemblyType.Quadword)
                                instructions.Add(new Instruction.Push(assemblyArg));
                            else
                            {
                                instructions.Add(new Instruction.Mov(Instruction.AssemblyType.Longword, assemblyArg, new Operand.Reg(Operand.RegisterName.AX)));
                                instructions.Add(new Instruction.Push(new Operand.Reg(Operand.RegisterName.AX)));
                            }
                        }

                        instructions.Add(new Instruction.Call(functionCall.Identifier));

                        var bytesToRemove = 8 * stackArgs.Count + stackPadding;
                        if (bytesToRemove != 0)
                            instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.Add, Instruction.AssemblyType.Quadword,
                                new Operand.Imm(bytesToRemove), new Operand.Reg(Operand.RegisterName.SP)));

                        var assemblyDst = GenerateOperand(functionCall.Dst);
                        instructions.Add(new Instruction.Mov(GetAssemblyType(functionCall.Dst), new Operand.Reg(Operand.RegisterName.AX), assemblyDst));
                    }
                    break;
                case TAC.Instruction.SignExtend signExtend:
                    instructions.Add(new Instruction.Movsx(GenerateOperand(signExtend.Src), GenerateOperand(signExtend.Dst)));
                    break;
                case TAC.Instruction.Truncate truncate:
                    instructions.Add(new Instruction.Mov(Instruction.AssemblyType.Longword, GenerateOperand(truncate.Src), GenerateOperand(truncate.Dst)));
                    break;
                case TAC.Instruction.ZeroExtend zeroExtend:
                    instructions.Add(new Instruction.MovZeroExtend(GenerateOperand(zeroExtend.Src), GenerateOperand(zeroExtend.Dst)));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        return instructions;
    }

    public static int GetAlignment(Type type)
    {
        return type switch
        {
            Type.Int or Type.UInt => 4,
            Type.Long or Type.ULong => 8,
            _ => throw new NotImplementedException()
        };
    }

    public static Instruction.AssemblyType GetAssemblyType(Type type)
    {
        return type switch
        {
            Type.Int or Type.UInt => Instruction.AssemblyType.Longword,
            Type.Long or Type.ULong => Instruction.AssemblyType.Quadword,
            _ => throw new NotImplementedException()
        };
    }

    private Instruction.AssemblyType GetAssemblyType(TAC.Val val)
    {
        return val switch
        {
            TAC.Val.Constant constant => constant.Value switch
            {
                AST.Const.ConstInt or AST.Const.ConstUInt => Instruction.AssemblyType.Longword,
                AST.Const.ConstLong or AST.Const.ConstULong => Instruction.AssemblyType.Quadword,
                _ => throw new NotImplementedException()
            },
            TAC.Val.Variable var => GetAssemblyType(symbolTable[var.Name].Type),
            _ => throw new NotImplementedException()
        };
    }

    private bool IsSignedType(TAC.Val val)
    {
        return val switch
        {
            TAC.Val.Constant constant => constant.Value switch
            {
                AST.Const.ConstInt or AST.Const.ConstLong => true,
                AST.Const.ConstUInt or AST.Const.ConstULong => false,
                _ => throw new NotImplementedException()
            },
            TAC.Val.Variable var => symbolTable[var.Name].Type switch
            {
                Type.Int or Type.Long => true,
                Type.UInt or Type.ULong => false,
                _ => throw new NotImplementedException()
            },
            _ => throw new NotImplementedException()
        };
    }

    private Operand GenerateOperand(TAC.Val val)
    {
        return val switch
        {
            TAC.Val.Constant c => c.Value switch
            {
                AST.Const.ConstInt constInt => new Operand.Imm(constInt.Value),
                AST.Const.ConstLong constLong => new Operand.Imm(constLong.Value),
                AST.Const.ConstUInt constUInt => new Operand.Imm(constUInt.Value),
                AST.Const.ConstULong constULong => new Operand.Imm((long)constULong.Value),
                _ => throw new NotImplementedException()
            },
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

    private Instruction.ConditionCode ConvertConditionCode(AST.Expression.BinaryOperator binaryOperator, bool isSigned)
    {
        return binaryOperator switch
        {
            AST.Expression.BinaryOperator.Equal => Instruction.ConditionCode.E,
            AST.Expression.BinaryOperator.NotEqual => Instruction.ConditionCode.NE,
            AST.Expression.BinaryOperator.GreaterThan => isSigned ? Instruction.ConditionCode.G : Instruction.ConditionCode.A,
            AST.Expression.BinaryOperator.GreaterOrEqual => isSigned ? Instruction.ConditionCode.GE : Instruction.ConditionCode.AE,
            AST.Expression.BinaryOperator.LessThan => isSigned ? Instruction.ConditionCode.L : Instruction.ConditionCode.B,
            AST.Expression.BinaryOperator.LessOrEqual => isSigned ? Instruction.ConditionCode.LE : Instruction.ConditionCode.BE,
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