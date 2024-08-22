namespace mcc2;

using mcc2.Assembly;
using mcc2.AST;

public class AssemblyGenerator
{
    public static Dictionary<string, AsmSymbolTableEntry> AsmSymbolTable = [];

    private Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable;
    private Dictionary<string, SemanticAnalyzer.StructEntry> typeTable;
    private Dictionary<string, TopLevel.StaticConstant> staticConstants = [];
    private int counter;

    public AssemblyGenerator(Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable, Dictionary<string, SemanticAnalyzer.StructEntry> typeTable)
    {
        this.symbolTable = symbolTable;
        this.typeTable = typeTable;

        foreach (var entry in symbolTable)
        {
            if (entry.Value.IdentifierAttributes is IdentifierAttributes.Function funAttr)
            {
                AsmSymbolTable[entry.Key] = new AsmSymbolTableEntry.FunctionEntry(funAttr.Defined);
            }
            else if (entry.Value.IdentifierAttributes is IdentifierAttributes.Constant constant)
            {
                AsmSymbolTable[entry.Key] = new AsmSymbolTableEntry.ObjectEntry(GetAssemblyType(entry.Value.Type), true, true);
            }
            else
            {
                AsmSymbolTable[entry.Key] = new AsmSymbolTableEntry.ObjectEntry(GetAssemblyType(entry.Value.Type), entry.Value.IdentifierAttributes is IdentifierAttributes.Static, false);
            }
        }
    }

    public AssemblyProgram Generate(TAC.TACProgam program)
    {
        return GenerateProgram(program);
    }

    private AssemblyProgram GenerateProgram(TAC.TACProgam program)
    {
        List<TopLevel> topLevelDefinitions = [];
        foreach (var def in program.Definitions)
        {
            if (def is TAC.TopLevel.Function fun)
                topLevelDefinitions.Add(GenerateFunction(fun));
            else if (def is TAC.TopLevel.StaticVariable staticVariable)
                topLevelDefinitions.Add(new TopLevel.StaticVariable(staticVariable.Identifier, staticVariable.Global,
                    GetAssemblyAlignment(staticVariable.Type), staticVariable.Inits));
            else if (def is TAC.TopLevel.StaticConstant staticConstant)
                topLevelDefinitions.Add(new TopLevel.StaticConstant(staticConstant.Identifier, GetAssemblyAlignment(staticConstant.Type), staticConstant.Init));
        }

        foreach (var cons in staticConstants)
        {
            topLevelDefinitions.Add(cons.Value);
        }

        return new AssemblyProgram(topLevelDefinitions);
    }

    private readonly Operand.RegisterName[] ABIRegisters = [
        Operand.RegisterName.DI,
        Operand.RegisterName.SI,
        Operand.RegisterName.DX,
        Operand.RegisterName.CX,
        Operand.RegisterName.R8,
        Operand.RegisterName.R9,
    ];

    private readonly Operand.RegisterName[] ABIFloatRegisters = [
        Operand.RegisterName.XMM0,
        Operand.RegisterName.XMM1,
        Operand.RegisterName.XMM2,
        Operand.RegisterName.XMM3,
        Operand.RegisterName.XMM4,
        Operand.RegisterName.XMM5,
        Operand.RegisterName.XMM6,
        Operand.RegisterName.XMM7,
    ];

    private TopLevel.Function GenerateFunction(TAC.TopLevel.Function function)
    {
        List<Instruction> instructions = [];

        SetupParameters(function.Name, function.Parameters, instructions);

        TopLevel.Function fn = new TopLevel.Function(function.Name, function.Global, GenerateInstructions(function.Instructions, instructions));

        foreach (var cons in staticConstants)
        {
            AsmSymbolTable[cons.Key] = new AsmSymbolTableEntry.ObjectEntry(new AssemblyType.Double(), true, true);
        }

        PseudoReplacer stackAllocator = new PseudoReplacer();
        long bytesToAllocate = stackAllocator.Replace(fn.Instructions);

        // note: chapter 9 says we should store this per function in symboltable or ast
        // in order to round it up but we can do that here as well
        bytesToAllocate = AlignTo(bytesToAllocate, 16);
        fn.Instructions.Insert(0, new Instruction.Binary(Instruction.BinaryOperator.Sub, new AssemblyType.Quadword(),
            new Operand.Imm((ulong)bytesToAllocate), new Operand.Reg(Operand.RegisterName.SP)));

        InstructionFixer instructionFixer = new InstructionFixer();
        instructionFixer.Fix(fn.Instructions);

        return fn;
    }

    private void SetupParameters(string functionName, List<string> parameters, List<Instruction> instructions)
    {
        List<TAC.Val> vals = [];
        foreach (var param in parameters)
            vals.Add(new TAC.Val.Variable(param));
        ClassifyParameters(vals, out var intRegArgs, out var doubleRegArgs, out var stackArgs);

        for (int i = 0; i < intRegArgs.Count; i++)
        {
            var arg = intRegArgs[i];
            instructions.Add(new Instruction.Mov(arg.Item1, new Operand.Reg(ABIRegisters[i]), arg.Item2));
        }

        for (int i = 0; i < doubleRegArgs.Count; i++)
        {
            var arg = doubleRegArgs[i];
            instructions.Add(new Instruction.Mov(new AssemblyType.Double(), new Operand.Reg(ABIFloatRegisters[i]), arg));
        }

        for (int i = 0; i < stackArgs.Count; i++)
        {
            var arg = stackArgs[i];
            instructions.Add(new Instruction.Mov(arg.Item1, new Operand.Memory(Operand.RegisterName.BP, 16 + i * 8), arg.Item2));
        }
    }

    public static long AlignTo(long bytes, long align)
    {
        return align * ((bytes + align - 1) / align);
    }

    private void ClassifyParameters(List<TAC.Val> parameters,
        out List<(AssemblyType, Operand)> intRegArgs,
        out List<Operand> doubleRegArgs,
        out List<(AssemblyType, Operand)> stackArgs)
    {
        intRegArgs = [];
        doubleRegArgs = [];
        stackArgs = [];

        for (int i = 0; i < parameters.Count; i++)
        {
            AssemblyType assemblyType = GetAssemblyType(parameters[i]);
            var operand = GenerateOperand(parameters[i]);
            var typedOperand = (assemblyType, operand);
            if (assemblyType is AssemblyType.Double)
            {
                if (doubleRegArgs.Count < ABIFloatRegisters.Length)
                    doubleRegArgs.Add(operand);
                else
                    stackArgs.Add(typedOperand);
            }
            else
            {
                if (intRegArgs.Count < ABIRegisters.Length)
                    intRegArgs.Add(typedOperand);
                else
                    stackArgs.Add(typedOperand);
            }
        }
    }

    private void GenerateFunctionCall(TAC.Instruction.FunctionCall functionCall, List<Instruction> instructions)
    {
        ClassifyParameters(functionCall.Arguments, out var intRegArgs, out var doubleRegArgs, out var stackArgs);

        var stackPadding = stackArgs.Count % 2 != 0 ? 8 : 0;

        if (stackPadding != 0)
            instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.Sub, new AssemblyType.Quadword(),
                new Operand.Imm((ulong)stackPadding), new Operand.Reg(Operand.RegisterName.SP)));

        // pass args on registers
        int regIndex = 0;
        foreach (var tackyArg in intRegArgs)
        {
            var reg = ABIRegisters[regIndex];
            instructions.Add(new Instruction.Mov(tackyArg.Item1, tackyArg.Item2, new Operand.Reg(reg)));
            regIndex++;
        }

        regIndex = 0;
        foreach (var assemblyArg in doubleRegArgs)
        {
            var reg = ABIFloatRegisters[regIndex];
            instructions.Add(new Instruction.Mov(new AssemblyType.Double(), assemblyArg, new Operand.Reg(reg)));
            regIndex++;
        }

        // pass args on stack
        for (int i = stackArgs.Count - 1; i >= 0; i--)
        {
            var tackyArg = stackArgs[i];
            if (tackyArg.Item2 is Operand.Reg or Operand.Imm || tackyArg.Item1 is AssemblyType.Quadword or AssemblyType.Double)
                instructions.Add(new Instruction.Push(tackyArg.Item2));
            else
            {
                instructions.Add(new Instruction.Mov(tackyArg.Item1, tackyArg.Item2, new Operand.Reg(Operand.RegisterName.AX)));
                instructions.Add(new Instruction.Push(new Operand.Reg(Operand.RegisterName.AX)));
            }
        }

        instructions.Add(new Instruction.Call(functionCall.Identifier));

        var bytesToRemove = 8 * stackArgs.Count + stackPadding;
        if (bytesToRemove != 0)
            instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.Add, new AssemblyType.Quadword(),
                new Operand.Imm((ulong)bytesToRemove), new Operand.Reg(Operand.RegisterName.SP)));

        if (functionCall.Dst != null)
        {
            var assemblyDst = GenerateOperand(functionCall.Dst);
            var returnType = GetAssemblyType(functionCall.Dst);
            if (returnType is AssemblyType.Double)
                instructions.Add(new Instruction.Mov(new AssemblyType.Double(), new Operand.Reg(Operand.RegisterName.XMM0), assemblyDst));
            else
                instructions.Add(new Instruction.Mov(returnType, new Operand.Reg(Operand.RegisterName.AX), assemblyDst));
        }
    }

    private List<Instruction> GenerateInstructions(List<TAC.Instruction> tacInstructions, List<Instruction> instructions)
    {
        foreach (var inst in tacInstructions)
        {
            switch (inst)
            {
                case TAC.Instruction.Return ret:
                    if (ret.Value != null)
                        if (GetAssemblyType(ret.Value) is AssemblyType.Double)
                            instructions.Add(new Instruction.Mov(new AssemblyType.Double(), GenerateOperand(ret.Value), new Operand.Reg(Operand.RegisterName.XMM0)));
                        else
                            instructions.Add(new Instruction.Mov(GetAssemblyType(ret.Value), GenerateOperand(ret.Value), new Operand.Reg(Operand.RegisterName.AX)));
                    instructions.Add(new Instruction.Ret());
                    break;
                case TAC.Instruction.Unary unary:
                    if (unary.UnaryOperator == AST.Expression.UnaryOperator.Not)
                    {
                        if (GetAssemblyType(unary.src) is AssemblyType.Double)
                        {
                            instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.Xor, new AssemblyType.Double(), new Operand.Reg(Operand.RegisterName.XMM0), new Operand.Reg(Operand.RegisterName.XMM0)));
                            instructions.Add(new Instruction.Cmp(new AssemblyType.Double(), GenerateOperand(unary.src), new Operand.Reg(Operand.RegisterName.XMM0)));
                            instructions.Add(new Instruction.Mov(GetAssemblyType(unary.dst), new Operand.Imm(0), GenerateOperand(unary.dst)));
                            instructions.Add(new Instruction.SetCC(Instruction.ConditionCode.E, GenerateOperand(unary.dst)));
                        }
                        else
                        {
                            instructions.Add(new Instruction.Cmp(GetAssemblyType(unary.src), new Operand.Imm(0), GenerateOperand(unary.src)));
                            instructions.Add(new Instruction.Mov(GetAssemblyType(unary.dst), new Operand.Imm(0), GenerateOperand(unary.dst)));
                            instructions.Add(new Instruction.SetCC(Instruction.ConditionCode.E, GenerateOperand(unary.dst)));
                        }
                    }
                    else if (unary.UnaryOperator == AST.Expression.UnaryOperator.Negate && GetAssemblyType(unary.src) is AssemblyType.Double)
                    {
                        instructions.Add(new Instruction.Mov(new AssemblyType.Double(), GenerateOperand(unary.src), GenerateOperand(unary.dst)));
                        var constLabel = GenerateStaticConstant(new AST.Const.ConstDouble(-0.0), 16);
                        instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.Xor, new AssemblyType.Double(), new Operand.Data(constLabel), GenerateOperand(unary.dst)));
                    }
                    else
                    {
                        instructions.Add(new Instruction.Mov(GetAssemblyType(unary.src), GenerateOperand(unary.src), GenerateOperand(unary.dst)));
                        instructions.Add(new Instruction.Unary(ConvertUnary(unary.UnaryOperator), GetAssemblyType(unary.src), GenerateOperand(unary.dst)));
                    }
                    break;
                case TAC.Instruction.Binary binary:
                    if (binary.Operator == AST.Expression.BinaryOperator.Divide && GetAssemblyType(binary.Src1) is not AssemblyType.Double ||
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
                        if (GetAssemblyType(binary.Src1) is AssemblyType.Double)
                        {
                            instructions.Add(new Instruction.Cmp(GetAssemblyType(binary.Src1), GenerateOperand(binary.Src2), GenerateOperand(binary.Src1)));
                            instructions.Add(new Instruction.Mov(GetAssemblyType(binary.Dst), new Operand.Imm(0), GenerateOperand(binary.Dst)));
                            instructions.Add(new Instruction.SetCC(ConvertConditionCode(binary.Operator, false), GenerateOperand(binary.Dst)));
                        }
                        else
                        {
                            instructions.Add(new Instruction.Cmp(GetAssemblyType(binary.Src1), GenerateOperand(binary.Src2), GenerateOperand(binary.Src1)));
                            instructions.Add(new Instruction.Mov(GetAssemblyType(binary.Dst), new Operand.Imm(0), GenerateOperand(binary.Dst)));
                            instructions.Add(new Instruction.SetCC(ConvertConditionCode(binary.Operator, IsSignedType(binary.Src1)), GenerateOperand(binary.Dst)));
                        }
                    }
                    else
                    {
                        instructions.Add(new Instruction.Mov(GetAssemblyType(binary.Src1), GenerateOperand(binary.Src1), GenerateOperand(binary.Dst)));
                        if (binary.Operator == AST.Expression.BinaryOperator.Divide && GetAssemblyType(binary.Src1) is AssemblyType.Double)
                            instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.DivDouble, GetAssemblyType(binary.Src1), GenerateOperand(binary.Src2), GenerateOperand(binary.Dst)));
                        else
                            instructions.Add(new Instruction.Binary(ConvertBinary(binary.Operator), GetAssemblyType(binary.Src1), GenerateOperand(binary.Src2), GenerateOperand(binary.Dst)));
                    }
                    break;
                case TAC.Instruction.Jump jump:
                    instructions.Add(new Instruction.Jmp(jump.Target));
                    break;
                case TAC.Instruction.JumpIfZero jumpZ:
                    if (GetAssemblyType(jumpZ.Condition) is AssemblyType.Double)
                    {
                        instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.Xor, new AssemblyType.Double(), new Operand.Reg(Operand.RegisterName.XMM0), new Operand.Reg(Operand.RegisterName.XMM0)));
                        instructions.Add(new Instruction.Cmp(new AssemblyType.Double(), GenerateOperand(jumpZ.Condition), new Operand.Reg(Operand.RegisterName.XMM0)));
                        instructions.Add(new Instruction.JmpCC(Instruction.ConditionCode.E, jumpZ.Target));
                    }
                    else
                    {
                        instructions.Add(new Instruction.Cmp(GetAssemblyType(jumpZ.Condition), new Operand.Imm(0), GenerateOperand(jumpZ.Condition)));
                        instructions.Add(new Instruction.JmpCC(Instruction.ConditionCode.E, jumpZ.Target));
                    }
                    break;
                case TAC.Instruction.JumpIfNotZero jumpNZ:
                    if (GetAssemblyType(jumpNZ.Condition) is AssemblyType.Double)
                    {
                        instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.Xor, new AssemblyType.Double(), new Operand.Reg(Operand.RegisterName.XMM0), new Operand.Reg(Operand.RegisterName.XMM0)));
                        instructions.Add(new Instruction.Cmp(new AssemblyType.Double(), GenerateOperand(jumpNZ.Condition), new Operand.Reg(Operand.RegisterName.XMM0)));
                        instructions.Add(new Instruction.JmpCC(Instruction.ConditionCode.NE, jumpNZ.Target));
                    }
                    else
                    {
                        instructions.Add(new Instruction.Cmp(GetAssemblyType(jumpNZ.Condition), new Operand.Imm(0), GenerateOperand(jumpNZ.Condition)));
                        instructions.Add(new Instruction.JmpCC(Instruction.ConditionCode.NE, jumpNZ.Target));
                    }
                    break;
                case TAC.Instruction.Copy copy:
                    instructions.Add(new Instruction.Mov(GetAssemblyType(copy.Src), GenerateOperand(copy.Src), GenerateOperand(copy.Dst)));
                    break;
                case TAC.Instruction.Label label:
                    instructions.Add(new Instruction.Label(label.Identifier));
                    break;
                case TAC.Instruction.FunctionCall functionCall:
                    GenerateFunctionCall(functionCall, instructions);
                    break;
                case TAC.Instruction.SignExtend signExtend:
                    instructions.Add(new Instruction.Movsx(GetAssemblyType(signExtend.Src), GetAssemblyType(signExtend.Dst), GenerateOperand(signExtend.Src), GenerateOperand(signExtend.Dst)));
                    break;
                case TAC.Instruction.Truncate truncate:
                    instructions.Add(new Instruction.Mov(GetAssemblyType(truncate.Dst), GenerateOperand(truncate.Src), GenerateOperand(truncate.Dst)));
                    break;
                case TAC.Instruction.ZeroExtend zeroExtend:
                    instructions.Add(new Instruction.MovZeroExtend(GetAssemblyType(zeroExtend.Src), GetAssemblyType(zeroExtend.Dst), GenerateOperand(zeroExtend.Src), GenerateOperand(zeroExtend.Dst)));
                    break;
                case TAC.Instruction.DoubleToInt doubleToInt:
                    if (GetAssemblyType(doubleToInt.Dst) is AssemblyType.Byte)
                    {
                        instructions.Add(new Instruction.Cvttsd2si(new AssemblyType.Longword(), GenerateOperand(doubleToInt.Src), new Operand.Reg(Operand.RegisterName.AX)));
                        instructions.Add(new Instruction.Mov(new AssemblyType.Byte(), new Operand.Reg(Operand.RegisterName.AX), GenerateOperand(doubleToInt.Dst)));
                    }
                    else
                        instructions.Add(new Instruction.Cvttsd2si(GetAssemblyType(doubleToInt.Dst), GenerateOperand(doubleToInt.Src), GenerateOperand(doubleToInt.Dst)));
                    break;
                case TAC.Instruction.DoubleToUInt doubleToUInt:
                    if (GetAssemblyType(doubleToUInt.Dst) is AssemblyType.Byte)
                    {
                        instructions.Add(new Instruction.Cvttsd2si(new AssemblyType.Longword(), GenerateOperand(doubleToUInt.Src), new Operand.Reg(Operand.RegisterName.AX)));
                        instructions.Add(new Instruction.Mov(new AssemblyType.Byte(), new Operand.Reg(Operand.RegisterName.AX), GenerateOperand(doubleToUInt.Dst)));
                    }
                    else if (GetAssemblyType(doubleToUInt.Dst) is AssemblyType.Longword)
                    {
                        instructions.Add(new Instruction.Cvttsd2si(new AssemblyType.Quadword(), GenerateOperand(doubleToUInt.Src), new Operand.Reg(Operand.RegisterName.AX)));
                        instructions.Add(new Instruction.Mov(new AssemblyType.Longword(), new Operand.Reg(Operand.RegisterName.AX), GenerateOperand(doubleToUInt.Dst)));
                    }
                    else
                    {
                        var upperBoundLabel = GenerateStaticConstant(new AST.Const.ConstDouble(9223372036854775808.0), 8);
                        instructions.Add(new Instruction.Cmp(new AssemblyType.Double(), new Operand.Data(upperBoundLabel), GenerateOperand(doubleToUInt.Src)));
                        var outOfRangeLabel = $".L_outOfRange.{counter}";
                        instructions.Add(new Instruction.JmpCC(Instruction.ConditionCode.AE, outOfRangeLabel));
                        instructions.Add(new Instruction.Cvttsd2si(new AssemblyType.Quadword(), GenerateOperand(doubleToUInt.Src), GenerateOperand(doubleToUInt.Dst)));
                        var endLabel = $".L_outOfRange_end.{counter++}";
                        instructions.Add(new Instruction.Jmp(endLabel));
                        instructions.Add(new Instruction.Label(outOfRangeLabel));
                        instructions.Add(new Instruction.Mov(new AssemblyType.Double(), GenerateOperand(doubleToUInt.Src), new Operand.Reg(Operand.RegisterName.XMM1)));
                        instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.Sub, new AssemblyType.Double(), new Operand.Data(upperBoundLabel), new Operand.Reg(Operand.RegisterName.XMM1)));
                        instructions.Add(new Instruction.Cvttsd2si(new AssemblyType.Quadword(), new Operand.Reg(Operand.RegisterName.XMM1), GenerateOperand(doubleToUInt.Dst)));
                        instructions.Add(new Instruction.Mov(new AssemblyType.Quadword(), new Operand.Imm(9223372036854775808), new Operand.Reg(Operand.RegisterName.DX)));
                        instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.Add, new AssemblyType.Quadword(), new Operand.Reg(Operand.RegisterName.DX), GenerateOperand(doubleToUInt.Dst)));
                        instructions.Add(new Instruction.Label(endLabel));
                    }
                    break;
                case TAC.Instruction.IntToDouble intToDouble:
                    if (GetAssemblyType(intToDouble.Src) is AssemblyType.Byte)
                    {
                        instructions.Add(new Instruction.Movsx(new AssemblyType.Byte(), new AssemblyType.Longword(), GenerateOperand(intToDouble.Src), new Operand.Reg(Operand.RegisterName.AX)));
                        instructions.Add(new Instruction.Cvtsi2sd(new AssemblyType.Longword(), new Operand.Reg(Operand.RegisterName.AX), GenerateOperand(intToDouble.Dst)));
                    }
                    else
                        instructions.Add(new Instruction.Cvtsi2sd(GetAssemblyType(intToDouble.Src), GenerateOperand(intToDouble.Src), GenerateOperand(intToDouble.Dst)));
                    break;
                case TAC.Instruction.UIntToDouble uintToDouble:
                    if (GetAssemblyType(uintToDouble.Src) is AssemblyType.Byte)
                    {
                        instructions.Add(new Instruction.MovZeroExtend(new AssemblyType.Byte(), new AssemblyType.Longword(), GenerateOperand(uintToDouble.Src), new Operand.Reg(Operand.RegisterName.AX)));
                        instructions.Add(new Instruction.Cvtsi2sd(new AssemblyType.Longword(), new Operand.Reg(Operand.RegisterName.AX), GenerateOperand(uintToDouble.Dst)));
                    }
                    else if (GetAssemblyType(uintToDouble.Src) is AssemblyType.Longword)
                    {
                        instructions.Add(new Instruction.MovZeroExtend(new AssemblyType.Longword(), new AssemblyType.Quadword(), GenerateOperand(uintToDouble.Src), new Operand.Reg(Operand.RegisterName.AX)));
                        instructions.Add(new Instruction.Cvtsi2sd(new AssemblyType.Quadword(), new Operand.Reg(Operand.RegisterName.AX), GenerateOperand(uintToDouble.Dst)));
                    }
                    else
                    {
                        instructions.Add(new Instruction.Cmp(new AssemblyType.Quadword(), new Operand.Imm(0), GenerateOperand(uintToDouble.Src)));
                        var outOfRangeLabel = $".L_outOfRange.{counter}";
                        instructions.Add(new Instruction.JmpCC(Instruction.ConditionCode.L, outOfRangeLabel));
                        instructions.Add(new Instruction.Cvtsi2sd(new AssemblyType.Quadword(), GenerateOperand(uintToDouble.Src), GenerateOperand(uintToDouble.Dst)));
                        var endLabel = $".L_outOfRange_end.{counter++}";
                        instructions.Add(new Instruction.Jmp(endLabel));
                        instructions.Add(new Instruction.Label(outOfRangeLabel));
                        instructions.Add(new Instruction.Mov(new AssemblyType.Quadword(), GenerateOperand(uintToDouble.Src), new Operand.Reg(Operand.RegisterName.AX)));
                        instructions.Add(new Instruction.Mov(new AssemblyType.Quadword(), new Operand.Reg(Operand.RegisterName.AX), new Operand.Reg(Operand.RegisterName.DX)));
                        instructions.Add(new Instruction.Unary(Instruction.UnaryOperator.Shr, new AssemblyType.Quadword(), new Operand.Reg(Operand.RegisterName.DX)));
                        instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.And, new AssemblyType.Quadword(), new Operand.Imm(1), new Operand.Reg(Operand.RegisterName.AX)));
                        instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.Or, new AssemblyType.Quadword(), new Operand.Reg(Operand.RegisterName.AX), new Operand.Reg(Operand.RegisterName.DX)));
                        instructions.Add(new Instruction.Cvtsi2sd(new AssemblyType.Quadword(), new Operand.Reg(Operand.RegisterName.DX), GenerateOperand(uintToDouble.Dst)));
                        instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.Add, new AssemblyType.Double(), GenerateOperand(uintToDouble.Dst), GenerateOperand(uintToDouble.Dst)));
                        instructions.Add(new Instruction.Label(endLabel));
                    }
                    break;
                case TAC.Instruction.Load load:
                    instructions.Add(new Instruction.Mov(new AssemblyType.Quadword(), GenerateOperand(load.SrcPtr), new Operand.Reg(Operand.RegisterName.AX)));
                    instructions.Add(new Instruction.Mov(GetAssemblyType(load.Dst), new Operand.Memory(Operand.RegisterName.AX, 0), GenerateOperand(load.Dst)));
                    break;
                case TAC.Instruction.Store store:
                    instructions.Add(new Instruction.Mov(new AssemblyType.Quadword(), GenerateOperand(store.DstPtr), new Operand.Reg(Operand.RegisterName.AX)));
                    instructions.Add(new Instruction.Mov(GetAssemblyType(store.Src), GenerateOperand(store.Src), new Operand.Memory(Operand.RegisterName.AX, 0)));
                    break;
                case TAC.Instruction.GetAddress getAddress:
                    instructions.Add(new Instruction.Lea(GenerateOperand(getAddress.Src), GenerateOperand(getAddress.Dst)));
                    break;
                case TAC.Instruction.CopyToOffset copyToOffset:
                    instructions.Add(new Instruction.Mov(GetAssemblyType(copyToOffset.Src), GenerateOperand(copyToOffset.Src), new Operand.PseudoMemory(copyToOffset.Dst, copyToOffset.Offset)));
                    break;
                case TAC.Instruction.AddPointer addPointer:
                    if (addPointer.Index is TAC.Val.Constant constant)
                    {
                        instructions.Add(new Instruction.Mov(new AssemblyType.Quadword(), GenerateOperand(addPointer.Pointer), new Operand.Reg(Operand.RegisterName.AX)));
                        instructions.Add(new Instruction.Lea(new Operand.Memory(Operand.RegisterName.AX, (long)GetValue(constant.Value) * addPointer.Scale), GenerateOperand(addPointer.Dst)));
                    }
                    else if (addPointer.Scale is 1 or 2 or 4 or 8)
                    {
                        instructions.Add(new Instruction.Mov(new AssemblyType.Quadword(), GenerateOperand(addPointer.Pointer), new Operand.Reg(Operand.RegisterName.AX)));
                        instructions.Add(new Instruction.Mov(new AssemblyType.Quadword(), GenerateOperand(addPointer.Index), new Operand.Reg(Operand.RegisterName.DX)));
                        instructions.Add(new Instruction.Lea(new Operand.Indexed(Operand.RegisterName.AX, Operand.RegisterName.DX, addPointer.Scale), GenerateOperand(addPointer.Dst)));
                    }
                    else
                    {
                        instructions.Add(new Instruction.Mov(new AssemblyType.Quadword(), GenerateOperand(addPointer.Pointer), new Operand.Reg(Operand.RegisterName.AX)));
                        instructions.Add(new Instruction.Mov(new AssemblyType.Quadword(), GenerateOperand(addPointer.Index), new Operand.Reg(Operand.RegisterName.DX)));
                        instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.Mult, new AssemblyType.Quadword(), new Operand.Imm((ulong)addPointer.Scale), new Operand.Reg(Operand.RegisterName.DX)));
                        instructions.Add(new Instruction.Lea(new Operand.Indexed(Operand.RegisterName.AX, Operand.RegisterName.DX, 1), GenerateOperand(addPointer.Dst)));
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        return instructions;
    }

    public static int GetAssemblyAlignment(Type type)
    {
        return type switch
        {
            Type.Int or Type.UInt => 4,
            Type.Long or Type.ULong or Type.Double or Type.Pointer => 8,
            Type.Array array => TypeChecker.GetTypeSize(array, SemanticAnalyzer.TypeTable) >= 16 ? 16 : GetAssemblyAlignment(array.Element),
            Type.Char or Type.SChar or Type.UChar => 1,
            _ => throw new NotImplementedException()
        };
    }

    public static AssemblyType GetAssemblyType(Type type)
    {
        return type switch
        {
            Type.Int or Type.UInt => new AssemblyType.Longword(),
            Type.Long or Type.ULong or Type.Pointer => new AssemblyType.Quadword(),
            Type.Double => new AssemblyType.Double(),
            Type.Array array => new AssemblyType.ByteArray(TypeChecker.GetTypeSize(array.Element, SemanticAnalyzer.TypeTable) * array.Size, GetAssemblyAlignment(array)),
            Type.Char or Type.SChar or Type.UChar => new AssemblyType.Byte(),
            _ => throw new NotImplementedException()
        };
    }

    private AssemblyType GetAssemblyType(TAC.Val val)
    {
        return val switch
        {
            TAC.Val.Constant constant => constant.Value switch
            {
                AST.Const.ConstInt or AST.Const.ConstUInt => new AssemblyType.Longword(),
                AST.Const.ConstLong or AST.Const.ConstULong => new AssemblyType.Quadword(),
                AST.Const.ConstDouble => new AssemblyType.Double(),
                AST.Const.ConstChar => new AssemblyType.Byte(),
                AST.Const.ConstUChar => new AssemblyType.Byte(),
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
                AST.Const.ConstInt or AST.Const.ConstLong or AST.Const.ConstChar => true,
                AST.Const.ConstUInt or AST.Const.ConstULong or AST.Const.ConstUChar => false,
                _ => throw new NotImplementedException()
            },
            TAC.Val.Variable var => symbolTable[var.Name].Type switch
            {
                Type.Int or Type.Long or Type.Char or Type.SChar => true,
                Type.UInt or Type.ULong or Type.Pointer or Type.UChar => false,
                _ => throw new NotImplementedException()
            },
            _ => throw new NotImplementedException()
        };
    }

    private ulong GetValue(Const value)
    {
        return value switch
        {
            AST.Const.ConstInt constInt => (ulong)constInt.Value,
            AST.Const.ConstLong constLong => (ulong)constLong.Value,
            AST.Const.ConstUInt constUInt => (ulong)constUInt.Value,
            AST.Const.ConstULong constULong => (ulong)constULong.Value,
            AST.Const.ConstChar constChar => (ulong)constChar.Value,
            AST.Const.ConstUChar constUChar => (ulong)constUChar.Value,
            _ => throw new NotImplementedException()
        };
    }

    private Operand GenerateOperand(TAC.Val val)
    {
        if (val is TAC.Val.Constant constant && constant.Value is AST.Const.ConstDouble constDouble)
        {
            var constLabel = GenerateStaticConstant(constDouble, 8);
            return new Operand.Pseudo(constLabel);
        }
        if (val is TAC.Val.Variable var && symbolTable.TryGetValue(var.Name, out var symbol) && symbol.Type is Type.Array)
        {
            return new Operand.PseudoMemory(var.Name, 0);
        }

        return val switch
        {
            TAC.Val.Constant c => c.Value switch
            {
                AST.Const.ConstInt constInt => new Operand.Imm((ulong)constInt.Value),
                AST.Const.ConstLong constLong => new Operand.Imm((ulong)constLong.Value),
                AST.Const.ConstUInt constUInt => new Operand.Imm((ulong)constUInt.Value),
                AST.Const.ConstULong constULong => new Operand.Imm((ulong)constULong.Value),
                AST.Const.ConstChar constChar => new Operand.Imm((ulong)constChar.Value),
                AST.Const.ConstUChar constUChar => new Operand.Imm((ulong)constUChar.Value),
                _ => throw new NotImplementedException()
            },
            TAC.Val.Variable v => new Operand.Pseudo(v.Name),
            _ => throw new NotImplementedException(),
        };
    }

    private string GenerateStaticConstant(AST.Const.ConstDouble constDouble, int alignment)
    {
        var constLabel = $".Lconst{alignment}_0x{BitConverter.DoubleToUInt64Bits(constDouble.Value).ToString("X")}";
        var staticConst = new TopLevel.StaticConstant(constLabel, alignment, new StaticInit.DoubleInit(constDouble.Value));
        staticConstants[constLabel] = staticConst;
        return constLabel;
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