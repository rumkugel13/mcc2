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
                bool returnsOnStack = false;
                if (entry.Value.Type is Type.FunctionType funcType && (TypeChecker.IsComplete(funcType.Return, typeTable) || funcType.Return is Type.Void))
                {
                    returnsOnStack = ReturnsOnStack(entry.Key);
                }
                AsmSymbolTable[entry.Key] = new AsmSymbolTableEntry.FunctionEntry(funAttr.Defined, returnsOnStack);
            }
            else if (entry.Value.IdentifierAttributes is IdentifierAttributes.Constant constant)
            {
                AsmSymbolTable[entry.Key] = new AsmSymbolTableEntry.ObjectEntry(GetAssemblyType(entry.Value.Type), true, true);
            }
            else if (entry.Value.IdentifierAttributes is IdentifierAttributes.Static stat && !TypeChecker.IsComplete(entry.Value.Type, typeTable))
            {
                AsmSymbolTable[entry.Key] = new AsmSymbolTableEntry.ObjectEntry(new AssemblyType.Byte(), true, true);
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

        SetupParameters(function.Parameters, ReturnsOnStack(function.Name), instructions);

        TopLevel.Function fn = new TopLevel.Function(function.Name, function.Global, GenerateInstructions(function.Instructions, instructions));

        foreach (var cons in staticConstants)
        {
            AsmSymbolTable[cons.Key] = new AsmSymbolTableEntry.ObjectEntry(new AssemblyType.Double(), true, true);
        }

        PseudoReplacer stackAllocator = new PseudoReplacer(function.Name);
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

    private bool ReturnsOnStack(string funcName)
    {
        return symbolTable[funcName].Type is Type.FunctionType funcType && funcType.Return is Type.Structure structure &&
            ClassifyStructure(typeTable[structure.Identifier])[0] == Operand.ClassType.Memory;
    }

    private void SetupParameters(List<string> parameters, bool returnInMemory, List<Instruction> instructions)
    {
        List<TAC.Val> vals = [];
        foreach (var param in parameters)
            vals.Add(new TAC.Val.Variable(param));
        (List<(AssemblyType, Operand)> intRegArgs, List<Operand> doubleRegArgs, List<(AssemblyType, Operand)> stackArgs) = ClassifyParameters(vals, returnInMemory);

        var regIndex = 0;

        if (returnInMemory)
        {
            instructions.Add(new Instruction.Mov(new AssemblyType.Quadword(), new Operand.Reg(Operand.RegisterName.DI), new Operand.Memory(Operand.RegisterName.BP, -8)));
            regIndex = 1;
        }

        foreach ((AssemblyType assemblyType, Operand operand) in intRegArgs)
        {
            var reg = ABIRegisters[regIndex];
            if (assemblyType is AssemblyType.ByteArray byteArray)
                CopyBytesFromReg(reg, operand, byteArray.Size, instructions);
            else
                instructions.Add(new Instruction.Mov(assemblyType, new Operand.Reg(reg), operand));
            regIndex++;
        }

        for (int i = 0; i < doubleRegArgs.Count; i++)
        {
            var arg = doubleRegArgs[i];
            instructions.Add(new Instruction.Mov(new AssemblyType.Double(), new Operand.Reg(ABIFloatRegisters[i]), arg));
        }

        var offset = 16;
        for (int i = 0; i < stackArgs.Count; i++)
        {
            (AssemblyType assemblyType, Operand operand) = stackArgs[i];
            if (assemblyType is AssemblyType.ByteArray byteArray)
                CopyBytes(new Operand.Memory(Operand.RegisterName.BP, offset), operand, byteArray.Size, instructions);
            else
                instructions.Add(new Instruction.Mov(assemblyType, new Operand.Memory(Operand.RegisterName.BP, offset), operand));
            offset += 8;
        }
    }

    public static long AlignTo(long bytes, long align)
    {
        return align * ((bytes + align - 1) / align);
    }

    private (List<(AssemblyType, Operand)> intRetVals, List<Operand> doubleRetVals, bool returnedInMemory) ClassifyReturnValue(TAC.Val returnValue)
    {
        AssemblyType assemblyType = GetAssemblyType(returnValue);

        if (assemblyType is AssemblyType.Double)
        {
            var operand = GenerateOperand(returnValue);
            return ([], [operand], false);
        }
        else if (IsScalar(returnValue))
        {
            var typedOperand = (assemblyType, GenerateOperand(returnValue));
            return ([typedOperand], [], false);
        }
        else
        {
            var parameterName = ((TAC.Val.Variable)returnValue).Name;
            var structDef = typeTable[((Type.Structure)(symbolTable[parameterName]).Type).Identifier];
            var classes = ClassifyStructure(structDef);
            var structSize = structDef.Size;
            if (classes[0] == Operand.ClassType.Memory)
                return ([], [], true);
            else
            {
                List<(AssemblyType, Operand)> intRetVals = [];
                List<Operand> doubleRetVals = [];
                long offset = 0;
                foreach (var classType in classes)
                {
                    var classOperand = new Operand.PseudoMemory(parameterName, offset);
                    if (classType is Operand.ClassType.SSE)
                        doubleRetVals.Add(classOperand);
                    else if (classType is Operand.ClassType.Integer)
                    {
                        var eightByteType = GetEightbyteType(offset, structSize);
                        intRetVals.Add((eightByteType, classOperand));
                    }
                    else
                        throw new NotImplementedException();
                    offset += 8;
                }
                return (intRetVals, doubleRetVals, false);
            }
        }
    }

    private (List<(AssemblyType, Operand)> intRegArgs, List<Operand> doubleRegArgs, List<(AssemblyType, Operand)> stackArgs) ClassifyParameters(List<TAC.Val> parameters, bool returnInMemory)
    {
        List<(AssemblyType, Operand)> intRegArgs = [];
        List<Operand> doubleRegArgs = [];
        List<(AssemblyType, Operand)> stackArgs = [];

        var intRegsAvailable = returnInMemory ? ABIRegisters.Length - 1 : ABIRegisters.Length;  // 5 or 6

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
            else if (IsScalar(parameters[i]))
            {
                if (intRegArgs.Count < intRegsAvailable)
                    intRegArgs.Add(typedOperand);
                else
                    stackArgs.Add(typedOperand);
            }
            else
            {
                var parameterName = ((TAC.Val.Variable)parameters[i]).Name;
                var structDef = typeTable[((Type.Structure)(symbolTable[parameterName]).Type).Identifier];
                var classes = ClassifyStructure(structDef);
                bool useStack = true;
                var structSize = structDef.Size;

                if (classes[0] != Operand.ClassType.Memory)
                {
                    List<(AssemblyType, Operand)> tentativeInts = [];
                    List<Operand> tentativeDoubles = [];
                    long offset = 0;
                    foreach (var classType in classes)
                    {
                        var classOperand = new Operand.PseudoMemory(parameterName, offset);
                        if (classType == Operand.ClassType.SSE)
                            tentativeDoubles.Add(classOperand);
                        else
                        {
                            var eightByteType = GetEightbyteType(offset, structSize);
                            tentativeInts.Add((eightByteType, classOperand));
                        }
                        offset += 8;
                    }

                    if (tentativeDoubles.Count + doubleRegArgs.Count <= 8 &&
                        tentativeInts.Count + intRegArgs.Count <= intRegsAvailable)
                    {
                        doubleRegArgs.AddRange(tentativeDoubles);
                        intRegArgs.AddRange(tentativeInts);
                        useStack = false;
                    }
                }

                if (useStack)
                {
                    long offset = 0;
                    foreach (var classType in classes)
                    {
                        var classOperand = new Operand.PseudoMemory(parameterName, offset);
                        var eightByteType = GetEightbyteType(offset, structSize);
                        stackArgs.Add((eightByteType, classOperand));
                        offset += 8;
                    }
                }
            }
        }

        return (intRegArgs, doubleRegArgs, stackArgs);
    }

    private AssemblyType GetEightbyteType(long offset, long structSize)
    {
        var bytesFromEnd = structSize - offset;
        if (bytesFromEnd >= 8)
            return new AssemblyType.Quadword();
        if (bytesFromEnd == 4)
            return new AssemblyType.Longword();
        if (bytesFromEnd == 1)
            return new AssemblyType.Byte();
        return new AssemblyType.ByteArray(bytesFromEnd, 8);
    }

    private List<Operand.ClassType> ClassifyStructure(SemanticAnalyzer.StructEntry structEntry)
    {
        // todo: cache the result
        if (structEntry.Size > 16)
        {
            List<Operand.ClassType> result = [];
            long size = structEntry.Size;
            while (size > 0)
            {
                result.Add(Operand.ClassType.Memory);
                size -= 8;
            }
            return result;
        }
        List<Type> scalarTypes = GetMemberTypes(structEntry);
        if (structEntry.Size > 8)
        {
            if (scalarTypes[0] is Type.Double && scalarTypes[^1] is Type.Double)
                return [Operand.ClassType.SSE, Operand.ClassType.SSE];
            if (scalarTypes[0] is Type.Double)
                return [Operand.ClassType.SSE, Operand.ClassType.Integer];
            if (scalarTypes[^1] is Type.Double)
                return [Operand.ClassType.Integer, Operand.ClassType.SSE];
            return [Operand.ClassType.Integer, Operand.ClassType.Integer];
        }
        else if (scalarTypes[0] is Type.Double)
            return [Operand.ClassType.SSE];
        else
            return [Operand.ClassType.Integer];
    }

    private List<Type> GetMemberTypes(SemanticAnalyzer.StructEntry structEntry)
    {
        List<Type> flatMembers = [];
        foreach (var member in structEntry.Members)
        {
            if (member.MemberType is Type.Array array)
            {
                for (int i = 0; i < array.Size; i++)
                    flatMembers.Add(array.Element);
            }
            else if (member.MemberType is Type.Structure structure)
            {
                flatMembers.AddRange(GetMemberTypes(typeTable[structure.Identifier]));
            }
            else
                flatMembers.Add(member.MemberType);
        }
        return flatMembers;
    }

    private void GenerateFunctionCall(TAC.Instruction.FunctionCall functionCall, List<Instruction> instructions)
    {
        bool returnInMemory = false;
        List<(AssemblyType, Operand)> intDests = [];
        List<Operand> doubleDests = [];
        long regIndex = 0;

        if (functionCall.Dst != null)
            (intDests, doubleDests, returnInMemory) = ClassifyReturnValue(functionCall.Dst);

        if (returnInMemory)
        {
            var dstOperand = GenerateOperand(functionCall.Dst!);
            instructions.Add(new Instruction.Lea(dstOperand, new Operand.Reg(Operand.RegisterName.DI)));
            regIndex = 1;
        }

        (List<(AssemblyType, Operand)> intRegArgs, List<Operand> doubleRegArgs, List<(AssemblyType, Operand)> stackArgs) = ClassifyParameters(functionCall.Arguments, returnInMemory);

        var stackPadding = stackArgs.Count % 2 != 0 ? 8 : 0;

        if (stackPadding != 0)
            instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.Sub, new AssemblyType.Quadword(),
                new Operand.Imm((ulong)stackPadding), new Operand.Reg(Operand.RegisterName.SP)));

        foreach ((AssemblyType assemblyType, Operand operand) in intRegArgs)
        {
            var reg = ABIRegisters[regIndex];
            if (assemblyType is AssemblyType.ByteArray byteArray)
                CopyBytesToReg(operand, reg, byteArray.Size, instructions);
            else
                instructions.Add(new Instruction.Mov(assemblyType, operand, new Operand.Reg(reg)));
            regIndex++;
        }

        regIndex = 0;
        foreach (var assemblyArg in doubleRegArgs)
        {
            var reg = ABIFloatRegisters[regIndex];
            instructions.Add(new Instruction.Mov(new AssemblyType.Double(), assemblyArg, new Operand.Reg(reg)));
            regIndex++;
        }

        for (int i = stackArgs.Count - 1; i >= 0; i--)
        {
            (AssemblyType assemblyType, Operand operand) = stackArgs[i];
            if (assemblyType is AssemblyType.ByteArray byteArray)
            {
                instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.Sub, new AssemblyType.Quadword(), new Operand.Imm(8), new Operand.Reg(Operand.RegisterName.SP)));
                CopyBytes(operand, new Operand.Memory(Operand.RegisterName.SP, 0), byteArray.Size, instructions);
            }
            else if (operand is Operand.Reg or Operand.Imm || assemblyType is AssemblyType.Quadword or AssemblyType.Double)
                instructions.Add(new Instruction.Push(operand));
            else
            {
                instructions.Add(new Instruction.Mov(assemblyType, operand, new Operand.Reg(Operand.RegisterName.AX)));
                instructions.Add(new Instruction.Push(new Operand.Reg(Operand.RegisterName.AX)));
            }
        }

        instructions.Add(new Instruction.Call(functionCall.Identifier));

        var bytesToRemove = 8 * stackArgs.Count + stackPadding;
        if (bytesToRemove != 0)
            instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.Add, new AssemblyType.Quadword(),
                new Operand.Imm((ulong)bytesToRemove), new Operand.Reg(Operand.RegisterName.SP)));

        if (functionCall.Dst != null && !returnInMemory)
        {
            Operand.RegisterName[] intReturnRegs = [Operand.RegisterName.AX, Operand.RegisterName.DX];
            Operand.RegisterName[] doubleReturnRegs = [Operand.RegisterName.XMM0, Operand.RegisterName.XMM1];

            regIndex = 0;
            foreach ((AssemblyType assemblyType, Operand operand) in intDests)
            {
                var reg = intReturnRegs[regIndex];
                if (assemblyType is AssemblyType.ByteArray byteArray)
                    CopyBytesFromReg(reg, operand, byteArray.Size, instructions);
                else
                    instructions.Add(new Instruction.Mov(assemblyType, new Operand.Reg(reg), operand));
                regIndex++;
            }

            regIndex = 0;
            foreach (Operand operand in doubleDests)
            {
                var reg = doubleReturnRegs[regIndex];
                instructions.Add(new Instruction.Mov(new AssemblyType.Double(), new Operand.Reg(reg), operand));
                regIndex++;
            }
        }
    }

    private void CopyBytesToReg(Operand operand, Operand.RegisterName reg, long size, List<Instruction> instructions)
    {
        var offset = size - 1;
        while (offset >= 0)
        {
            var srcByte = AddOffset(operand, offset);
            instructions.Add(new Instruction.Mov(new AssemblyType.Byte(), srcByte, new Operand.Reg(reg)));
            if (offset > 0)
                instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.Shl, new AssemblyType.Quadword(), new Operand.Imm(8), new Operand.Reg(reg)));
            offset -= 1;
        }
    }

    private void CopyBytesFromReg(Operand.RegisterName reg, Operand operand, long size, List<Instruction> instructions)
    {
        var offset = 0;
        while (offset < size)
        {
            var dstByte = AddOffset(operand, offset);
            instructions.Add(new Instruction.Mov(new AssemblyType.Byte(), new Operand.Reg(reg), dstByte));
            if (offset < size - 1)
                instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.ShrTwoOp, new AssemblyType.Quadword(), new Operand.Imm(8), new Operand.Reg(reg)));
            offset += 1;
        }
    }

    private List<Instruction> GenerateInstructions(List<TAC.Instruction> tacInstructions, List<Instruction> instructions)
    {
        foreach (var inst in tacInstructions)
        {
            switch (inst)
            {
                case TAC.Instruction.Return ret:
                    {
                        if (ret.Value == null)
                        {
                            instructions.Add(new Instruction.Ret());
                            break;
                        }

                        (List<(AssemblyType, Operand)> intRetVals, List<Operand> doubleRetVals, bool returnedInMemory) = ClassifyReturnValue(ret.Value);

                        if (returnedInMemory)
                        {
                            instructions.Add(new Instruction.Mov(new AssemblyType.Quadword(), new Operand.Memory(Operand.RegisterName.BP, -8), new Operand.Reg(Operand.RegisterName.AX)));
                            var returnStorage = new Operand.Memory(Operand.RegisterName.AX, 0);
                            var retOperand = GenerateOperand(ret.Value);
                            var t = GetAssemblyType(ret.Value);
                            CopyBytes(retOperand, returnStorage, GetSize(ret.Value), instructions);
                        }
                        else
                        {
                            Operand.RegisterName[] intReturnRegs = [Operand.RegisterName.AX, Operand.RegisterName.DX];
                            Operand.RegisterName[] doubleReturnRegs = [Operand.RegisterName.XMM0, Operand.RegisterName.XMM1];

                            var regIndex = 0;
                            foreach ((AssemblyType assemblyType, Operand operand) in intRetVals)
                            {
                                var reg = intReturnRegs[regIndex];
                                if (assemblyType is AssemblyType.ByteArray byteArray)
                                    CopyBytesToReg(operand, reg, byteArray.Size, instructions);
                                else
                                    instructions.Add(new Instruction.Mov(assemblyType, operand, new Operand.Reg(reg)));
                                regIndex++;
                            }

                            regIndex = 0;
                            foreach (Operand operand in doubleRetVals)
                            {
                                var reg = doubleReturnRegs[regIndex];
                                instructions.Add(new Instruction.Mov(new AssemblyType.Double(), operand, new Operand.Reg(reg)));
                                regIndex++;
                            }
                        }
                        instructions.Add(new Instruction.Ret());
                    }
                    break;
                case TAC.Instruction.Unary unary:
                    if (unary.UnaryOperator == AST.Expression.UnaryOperator.Not)
                    {
                        if (GetAssemblyType(unary.Src) is AssemblyType.Double)
                        {
                            instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.Xor, new AssemblyType.Double(), new Operand.Reg(Operand.RegisterName.XMM0), new Operand.Reg(Operand.RegisterName.XMM0)));
                            instructions.Add(new Instruction.Cmp(new AssemblyType.Double(), GenerateOperand(unary.Src), new Operand.Reg(Operand.RegisterName.XMM0)));
                            instructions.Add(new Instruction.Mov(GetAssemblyType(unary.Dst), new Operand.Imm(0), GenerateOperand(unary.Dst)));
                            instructions.Add(new Instruction.SetCC(Instruction.ConditionCode.E, GenerateOperand(unary.Dst)));
                        }
                        else
                        {
                            instructions.Add(new Instruction.Cmp(GetAssemblyType(unary.Src), new Operand.Imm(0), GenerateOperand(unary.Src)));
                            instructions.Add(new Instruction.Mov(GetAssemblyType(unary.Dst), new Operand.Imm(0), GenerateOperand(unary.Dst)));
                            instructions.Add(new Instruction.SetCC(Instruction.ConditionCode.E, GenerateOperand(unary.Dst)));
                        }
                    }
                    else if (unary.UnaryOperator == AST.Expression.UnaryOperator.Negate && GetAssemblyType(unary.Src) is AssemblyType.Double)
                    {
                        instructions.Add(new Instruction.Mov(new AssemblyType.Double(), GenerateOperand(unary.Src), GenerateOperand(unary.Dst)));
                        var constLabel = GenerateStaticConstant(new AST.Const.ConstDouble(-0.0), 16);
                        instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.Xor, new AssemblyType.Double(), new Operand.Data(constLabel, 0), GenerateOperand(unary.Dst)));
                    }
                    else
                    {
                        instructions.Add(new Instruction.Mov(GetAssemblyType(unary.Src), GenerateOperand(unary.Src), GenerateOperand(unary.Dst)));
                        instructions.Add(new Instruction.Unary(ConvertUnary(unary.UnaryOperator), GetAssemblyType(unary.Src), GenerateOperand(unary.Dst)));
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
                    if (IsScalar(copy.Src))
                        instructions.Add(new Instruction.Mov(GetAssemblyType(copy.Src), GenerateOperand(copy.Src), GenerateOperand(copy.Dst)));
                    else
                        CopyBytes(GenerateOperand(copy.Src), GenerateOperand(copy.Dst), GetSize(copy.Src), instructions);
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
                        instructions.Add(new Instruction.Cmp(new AssemblyType.Double(), new Operand.Data(upperBoundLabel, 0), GenerateOperand(doubleToUInt.Src)));
                        var outOfRangeLabel = $".L_outOfRange.{counter}";
                        instructions.Add(new Instruction.JmpCC(Instruction.ConditionCode.AE, outOfRangeLabel));
                        instructions.Add(new Instruction.Cvttsd2si(new AssemblyType.Quadword(), GenerateOperand(doubleToUInt.Src), GenerateOperand(doubleToUInt.Dst)));
                        var endLabel = $".L_outOfRange_end.{counter++}";
                        instructions.Add(new Instruction.Jmp(endLabel));
                        instructions.Add(new Instruction.Label(outOfRangeLabel));
                        instructions.Add(new Instruction.Mov(new AssemblyType.Double(), GenerateOperand(doubleToUInt.Src), new Operand.Reg(Operand.RegisterName.XMM1)));
                        instructions.Add(new Instruction.Binary(Instruction.BinaryOperator.Sub, new AssemblyType.Double(), new Operand.Data(upperBoundLabel, 0), new Operand.Reg(Operand.RegisterName.XMM1)));
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
                    if (IsScalar(load.Dst))
                    {
                        instructions.Add(new Instruction.Mov(new AssemblyType.Quadword(), GenerateOperand(load.SrcPtr), new Operand.Reg(Operand.RegisterName.AX)));
                        instructions.Add(new Instruction.Mov(GetAssemblyType(load.Dst), new Operand.Memory(Operand.RegisterName.AX, 0), GenerateOperand(load.Dst)));
                    }
                    else
                    {
                        instructions.Add(new Instruction.Mov(new AssemblyType.Quadword(), GenerateOperand(load.SrcPtr), new Operand.Reg(Operand.RegisterName.AX)));
                        CopyBytes(new Operand.Memory(Operand.RegisterName.AX, 0), GenerateOperand(load.Dst), GetSize(load.Dst), instructions);
                    }
                    break;
                case TAC.Instruction.Store store:
                    if (IsScalar(store.Src))
                    {
                        instructions.Add(new Instruction.Mov(new AssemblyType.Quadword(), GenerateOperand(store.DstPtr), new Operand.Reg(Operand.RegisterName.AX)));
                        instructions.Add(new Instruction.Mov(GetAssemblyType(store.Src), GenerateOperand(store.Src), new Operand.Memory(Operand.RegisterName.AX, 0)));
                    }
                    else
                    {
                        instructions.Add(new Instruction.Mov(new AssemblyType.Quadword(), GenerateOperand(store.DstPtr), new Operand.Reg(Operand.RegisterName.AX)));
                        CopyBytes(GenerateOperand(store.Src), new Operand.Memory(Operand.RegisterName.AX, 0), GetSize(store.Src), instructions);
                    }
                    break;
                case TAC.Instruction.GetAddress getAddress:
                    instructions.Add(new Instruction.Lea(GenerateOperand(getAddress.Src), GenerateOperand(getAddress.Dst)));
                    break;
                case TAC.Instruction.CopyToOffset copyToOffset:
                    if (IsScalar(copyToOffset.Src))
                        instructions.Add(new Instruction.Mov(GetAssemblyType(copyToOffset.Src), GenerateOperand(copyToOffset.Src), new Operand.PseudoMemory(copyToOffset.Dst, copyToOffset.Offset)));
                    else
                    {
                        CopyBytes(GenerateOperand(copyToOffset.Src), new Operand.PseudoMemory(copyToOffset.Dst, copyToOffset.Offset), GetSize(copyToOffset.Src), instructions);
                    }
                    break;
                case TAC.Instruction.CopyFromOffset copyFromOffset:
                    if (IsScalar(copyFromOffset.Dst))
                        instructions.Add(new Instruction.Mov(GetAssemblyType(copyFromOffset.Dst), new Operand.PseudoMemory(copyFromOffset.Src, copyFromOffset.Offset), GenerateOperand(copyFromOffset.Dst)));
                    else
                    {
                        CopyBytes(new Operand.PseudoMemory(copyFromOffset.Src, copyFromOffset.Offset), GenerateOperand(copyFromOffset.Dst), GetSize(copyFromOffset.Dst), instructions);
                    }
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

    private void CopyBytes(Operand Src, Operand Dst, long bytes, List<Instruction> instructions)
    {
        if (bytes == 0)
            return;

        AssemblyType assemblyType = new AssemblyType.Byte();
        long operandSize = 1;
        if (bytes >= 8)
        {
            assemblyType = new AssemblyType.Quadword();
            operandSize = 8;
        }
        else if (bytes >= 4)
        {
            assemblyType = new AssemblyType.Longword();
            operandSize = 4;
        }

        var nextSrc = AddOffset(Src, operandSize);
        var nextDst = AddOffset(Dst, operandSize);
        var bytesLeft = bytes - operandSize;
        instructions.Add(new Instruction.Mov(assemblyType, Src, Dst));
        CopyBytes(nextSrc, nextDst, bytesLeft, instructions);
    }

    private Operand AddOffset(Operand operand, long bytes)
    {
        return operand switch
        {
            Operand.Memory memory => new Operand.Memory(memory.Register, memory.Offset + bytes),
            Operand.PseudoMemory pseudo => new Operand.PseudoMemory(pseudo.Identifier, pseudo.Offset + bytes),
            _ => throw new NotImplementedException(),
        };
    }

    public static long GetAssemblyAlignment(Type type)
    {
        return type switch
        {
            Type.Int or Type.UInt => 4,
            Type.Long or Type.ULong or Type.Double or Type.Pointer => 8,
            Type.Array array => TypeChecker.GetTypeSize(array, SemanticAnalyzer.TypeTable) >= 16 ? 16 : GetAssemblyAlignment(array.Element),
            Type.Char or Type.SChar or Type.UChar => 1,
            Type.Structure structure => SemanticAnalyzer.TypeTable[structure.Identifier].Alignment,
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
            Type.Structure structure => new AssemblyType.ByteArray(SemanticAnalyzer.TypeTable[structure.Identifier].Size, SemanticAnalyzer.TypeTable[structure.Identifier].Alignment),
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

    private bool IsScalar(TAC.Val val)
    {
        return val switch
        {
            TAC.Val.Constant constant => true,
            TAC.Val.Variable var => TypeChecker.IsScalar(symbolTable[var.Name].Type),
            _ => throw new NotImplementedException()
        };
    }

    private long GetSize(TAC.Val val)
    {
        return val switch
        {
            TAC.Val.Constant constant => constant.Value switch
            {
                AST.Const.ConstInt or AST.Const.ConstUInt => 4,
                AST.Const.ConstLong or AST.Const.ConstULong => 8,
                AST.Const.ConstDouble => 8,
                AST.Const.ConstChar => 1,
                AST.Const.ConstUChar => 1,
                _ => throw new NotImplementedException()
            },
            TAC.Val.Variable var => TypeChecker.GetTypeSize(symbolTable[var.Name].Type, typeTable),
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
        if (val is TAC.Val.Variable var2 && symbolTable.TryGetValue(var2.Name, out var symbol2) && symbol2.Type is Type.Structure)
        {
            return new Operand.PseudoMemory(var2.Name, 0);
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