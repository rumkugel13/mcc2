using mcc2.AST;
using mcc2.CFG;
using mcc2.TAC;

namespace mcc2;

public class TackyOptimizer()
{
    public TACProgam Optimize(TACProgam program, CompilerDriver.Optimizations optimizations)
    {
        for (int i = 0; i < program.Definitions.Count; i++)
        {
            TopLevel? topLevel = program.Definitions[i];
            if (topLevel is TopLevel.Function func)
                program.Definitions[i] = OptimizeFunction(func, optimizations);
        }

        return program;
    }

    private TopLevel.Function OptimizeFunction(TopLevel.Function function, CompilerDriver.Optimizations optimizations)
    {
        return new TopLevel.Function(function.Name, function.Global, function.Parameters, OptimizeFunctionBody(function.Instructions, optimizations));
    }

    private List<Instruction> OptimizeFunctionBody(List<Instruction> instructions, CompilerDriver.Optimizations optimizations)
    {
        if (instructions.Count == 0)
            return instructions;

        while (true)
        {
            var aliasedVars = AddressTakenAnalysis(instructions);

            List<Instruction> postConstantFolding = optimizations.HasFlag(CompilerDriver.Optimizations.FoldConstants) ? ConstantFolding(instructions) : instructions;

            Graph cfg = MakeControlFlowGraph(postConstantFolding);

            if (optimizations.HasFlag(CompilerDriver.Optimizations.EliminateUnreachableCode))
                cfg = UnreachableCodeElimination(cfg);

            if (optimizations.HasFlag(CompilerDriver.Optimizations.PropagateCopies))
                cfg = CopyPropagation(cfg, aliasedVars);

            if (optimizations.HasFlag(CompilerDriver.Optimizations.EliminateDeadStores))
                cfg = DeadStoreElimination(cfg, aliasedVars);

            List<Instruction> optimized = ControlFlowGraphToInstructions(cfg);

            if (AreEqual(optimized, instructions) || optimized.Count == 0)
                return optimized;

            instructions = optimized;
        }
    }

    private bool AreEqual(List<Instruction> list1, List<Instruction> list2)
    {
        if (list1.Count != list2.Count)
            return false;

        for (int i = 0; i < list1.Count; i++)
        {
            if (list1[i] is Instruction.FunctionCall f1 && list2[i] is Instruction.FunctionCall f2)
            {
                if (f1.Identifier != f2.Identifier || f1.Dst != f2.Dst || f1.Arguments.Count != f2.Arguments.Count ||
                    !f1.Arguments.SequenceEqual(f2.Arguments))
                    return false;
            }
            else if (list1[i] != list2[i])
                return false;
        }

        return true;
    }

    private List<Val.Variable> AddressTakenAnalysis(List<Instruction> instructions)
    {
        List<Val.Variable> result = [];

        foreach (var inst in instructions)
        {
            if (inst is Instruction.GetAddress getAddr)
                result.Add((Val.Variable)getAddr.Src);
        }
        return result;
    }

    private List<Instruction> ConstantFolding(List<Instruction> instructions)
    {
        List<Instruction> result = [];

        foreach (var inst in instructions)
        {
            switch (inst)
            {
                case Instruction.Unary unary:
                    if (unary.Src is Val.Constant constant)
                    {
                        result.Add(new Instruction.Copy(UnaryFold(unary.UnaryOperator, constant), unary.Dst));
                    }
                    else
                        result.Add(inst);
                    break;
                case Instruction.Binary binary:
                    if (binary.Src1 is Val.Constant constant1 && binary.Src2 is Val.Constant constant2)
                    {
                        result.Add(new Instruction.Copy(BinaryFold(binary.Operator, constant1, constant2), binary.Dst));
                    }
                    else
                        result.Add(inst);
                    break;
                case Instruction.JumpIfZero jz:
                    if (jz.Condition is Val.Constant jzConst)
                    {
                        if (GetValue(jzConst.Value) == 0)
                            result.Add(new Instruction.Jump(jz.Target));
                    }
                    else
                        result.Add(inst);
                    break;
                case Instruction.JumpIfNotZero jnz:
                    if (jnz.Condition is Val.Constant jnzConst)
                    {
                        if (GetValue(jnzConst.Value) != 0)
                            result.Add(new Instruction.Jump(jnz.Target));
                    }
                    else
                        result.Add(inst);
                    break;
                case Instruction.IntToDouble i2d:
                    if (i2d.Src is Val.Constant i2dConst)
                    {
                        switch (i2dConst.Value)
                        {
                            case Const.ConstChar c:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstDouble((double)(sbyte)c.Value)), i2d.Dst));
                                break;
                            case Const.ConstInt c:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstDouble((double)c.Value)), i2d.Dst));
                                break;
                            case Const.ConstLong c:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstDouble((double)c.Value)), i2d.Dst));
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    else
                        result.Add(inst);
                    break;
                case Instruction.UIntToDouble u2d:
                    if (u2d.Src is Val.Constant u2dConst)
                    {
                        switch (u2dConst.Value)
                        {
                            case Const.ConstUChar c:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstDouble((double)(byte)c.Value)), u2d.Dst));
                                break;
                            case Const.ConstUInt c:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstDouble((double)c.Value)), u2d.Dst));
                                break;
                            case Const.ConstULong c:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstDouble((double)c.Value)), u2d.Dst));
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    else
                        result.Add(inst);
                    break;
                case Instruction.DoubleToInt d2i:
                    if (d2i.Src is Val.Constant d2iConst && d2iConst.Value is Const.ConstDouble dval)
                    {
                        switch (GetType(d2i.Dst))
                        {
                            case Type.Char c:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstChar((sbyte)dval.Value)), d2i.Dst));
                                break;
                            case Type.SChar c:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstChar((sbyte)dval.Value)), d2i.Dst));
                                break;
                            case Type.Int i:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstInt((int)dval.Value)), d2i.Dst));
                                break;
                            case Type.Long l:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstLong((long)dval.Value)), d2i.Dst));
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    else
                        result.Add(inst);
                    break;
                case Instruction.DoubleToUInt d2u:
                    if (d2u.Src is Val.Constant d2uConst && d2uConst.Value is Const.ConstDouble dval2)
                    {
                        switch (GetType(d2u.Dst))
                        {
                            case Type.UChar uc:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstUChar((byte)dval2.Value)), d2u.Dst));
                                break;
                            case Type.UInt ui:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstUInt((uint)dval2.Value)), d2u.Dst));
                                break;
                            case Type.ULong ul:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstULong((ulong)dval2.Value)), d2u.Dst));
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    else
                        result.Add(inst);
                    break;
                case Instruction.SignExtend se:
                    if (se.Src is Val.Constant seConst)
                    {
                        if (seConst.Value is Const.ConstChar smallC)
                        {
                            switch (GetType(se.Dst))
                            {
                                case Type.Int:
                                    result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstInt((int)(sbyte)smallC.Value)), se.Dst));
                                    break;
                                case Type.UInt:
                                    result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstUInt((uint)(int)(sbyte)smallC.Value)), se.Dst));
                                    break;
                                case Type.Long:
                                    result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstLong((long)(sbyte)smallC.Value)), se.Dst));
                                    break;
                                case Type.ULong:
                                    result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstULong((ulong)(long)(sbyte)smallC.Value)), se.Dst));
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                        }
                        else if (seConst.Value is Const.ConstInt smallI)
                        {
                            if (GetType(se.Dst) is Type.Long)
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstLong((long)smallI.Value)), se.Dst));
                            else if (GetType(se.Dst) is Type.ULong or Type.Pointer)
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstULong((ulong)(long)smallI.Value)), se.Dst));
                            else
                                throw new NotImplementedException();
                        }
                        else
                            result.Add(inst);
                    }
                    else
                        result.Add(inst);
                    break;
                case Instruction.ZeroExtend ze:
                    if (ze.Src is Val.Constant zeConst)
                    {
                        if (zeConst.Value is Const.ConstUChar smallC)
                        {
                            switch (GetType(ze.Dst))
                            {
                                case Type.Int:
                                    result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstInt((int)(uint)(byte)smallC.Value)), ze.Dst));
                                    break;
                                case Type.UInt:
                                    result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstUInt((uint)(byte)smallC.Value)), ze.Dst));
                                    break;
                                case Type.Long:
                                    result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstLong((long)(ulong)(byte)smallC.Value)), ze.Dst));
                                    break;
                                case Type.ULong:
                                    result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstULong((ulong)(byte)smallC.Value)), ze.Dst));
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                        }
                        else if (zeConst.Value is Const.ConstUInt smallI)
                        {
                            if (GetType(ze.Dst) is Type.Long)
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstLong((long)(ulong)smallI.Value)), ze.Dst));
                            else if (GetType(ze.Dst) is Type.ULong or Type.Pointer)
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstULong((ulong)smallI.Value)), ze.Dst));
                            else
                                throw new NotImplementedException();
                        }
                        else
                            result.Add(inst);
                    }
                    else
                        result.Add(inst);
                    break;
                case Instruction.Truncate trun:
                    if (trun.Src is Val.Constant trunConst)
                    {
                        long value = trunConst.Value switch
                        {
                            Const.ConstLong c => (long)c.Value,
                            Const.ConstULong c => (long)c.Value,
                            Const.ConstInt c => (long)c.Value,
                            Const.ConstUInt c => (long)c.Value,
                            _ => throw new NotImplementedException()
                        };

                        switch (GetType(trun.Dst))
                        {
                            case Type.Int:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstInt((int)value)), trun.Dst));
                                break;
                            case Type.UInt:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstUInt((uint)value)), trun.Dst));
                                break;
                            case Type.Char:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstChar((byte)value)), trun.Dst));
                                break;
                            case Type.SChar:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstChar((sbyte)value)), trun.Dst));
                                break;
                            case Type.UChar:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstUChar((byte)value)), trun.Dst));
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    else
                        result.Add(inst);
                    break;
                default:
                    result.Add(inst);
                    break;
            }
        }

        return result;
    }

    private Type GetType(TAC.Val val)
    {
        return val switch
        {
            TAC.Val.Constant constant => constant.Value switch
            {
                AST.Const.ConstInt => new Type.Int(),
                AST.Const.ConstUInt => new Type.UInt(),
                AST.Const.ConstLong => new Type.Long(),
                AST.Const.ConstULong => new Type.ULong(),
                AST.Const.ConstDouble => new Type.Double(),
                AST.Const.ConstChar => new Type.Char(),
                AST.Const.ConstUChar => new Type.UChar(),
                _ => throw new NotImplementedException()
            },
            TAC.Val.Variable var => SemanticAnalyzer.SymbolTable[var.Name].Type,
            _ => throw new NotImplementedException()
        };
    }

    private Val.Constant UnaryFold(AST.Expression.UnaryOperator op, Val.Constant constant)
    {
        return op switch
        {
            AST.Expression.UnaryOperator.Complement => constant.Value switch
            {
                Const.ConstInt c => new Val.Constant(new Const.ConstInt(~c.Value)),
                Const.ConstLong c => new Val.Constant(new Const.ConstLong(~c.Value)),
                Const.ConstChar c => new Val.Constant(new Const.ConstChar(~c.Value)),
                Const.ConstUInt c => new Val.Constant(new Const.ConstUInt(~c.Value)),
                Const.ConstULong c => new Val.Constant(new Const.ConstULong(~c.Value)),
                Const.ConstUChar c => new Val.Constant(new Const.ConstUChar(~c.Value)),
                _ => constant,
            },
            AST.Expression.UnaryOperator.Negate => constant.Value switch
            {
                Const.ConstInt c => new Val.Constant(new Const.ConstInt(-c.Value)),
                Const.ConstLong c => new Val.Constant(new Const.ConstLong(-c.Value)),
                Const.ConstChar c => new Val.Constant(new Const.ConstChar(-c.Value)),
                Const.ConstUInt c => new Val.Constant(new Const.ConstUInt((uint)-(int)c.Value)),
                Const.ConstULong c => new Val.Constant(new Const.ConstULong((ulong)-(long)c.Value)),
                Const.ConstUChar c => new Val.Constant(new Const.ConstUChar(-c.Value)),
                Const.ConstDouble c => new Val.Constant(new Const.ConstDouble(-c.Value)),
                _ => constant,
            },
            AST.Expression.UnaryOperator.Not => constant.Value switch
            {
                Const.ConstInt c => new Val.Constant(new Const.ConstInt(c.Value == 0 ? 1 : 0)),
                Const.ConstLong c => new Val.Constant(new Const.ConstInt(c.Value == 0 ? 1 : 0)),
                Const.ConstChar c => new Val.Constant(new Const.ConstInt(c.Value == 0 ? 1 : 0)),
                Const.ConstUInt c => new Val.Constant(new Const.ConstInt(c.Value == 0 ? 1 : 0)),
                Const.ConstULong c => new Val.Constant(new Const.ConstInt(c.Value == 0 ? 1 : 0)),
                Const.ConstUChar c => new Val.Constant(new Const.ConstInt(c.Value == 0 ? 1 : 0)),
                Const.ConstDouble c => new Val.Constant(new Const.ConstInt(c.Value == 0 ? 1 : 0)),
                _ => constant,
            },
            _ => constant,
        };
    }

    private Val.Constant BinaryFold(AST.Expression.BinaryOperator op, Val.Constant c1, Val.Constant c2)
    {
        switch (op)
        {
            case Expression.BinaryOperator.Add:
                return (c1.Value, c2.Value)
                switch
                {
                    (Const.ConstInt int1, Const.ConstInt int2) => new Val.Constant(new Const.ConstInt(int1.Value + int2.Value)),
                    (Const.ConstLong long1, Const.ConstLong long2) => new Val.Constant(new Const.ConstLong(long1.Value + long2.Value)),
                    (Const.ConstChar char1, Const.ConstChar char2) => new Val.Constant(new Const.ConstChar(char1.Value + char2.Value)),
                    (Const.ConstUInt uint1, Const.ConstUInt uint2) => new Val.Constant(new Const.ConstUInt(uint1.Value + uint2.Value)),
                    (Const.ConstULong ulong1, Const.ConstULong ulong2) => new Val.Constant(new Const.ConstULong(ulong1.Value + ulong2.Value)),
                    (Const.ConstUChar uchar1, Const.ConstUChar uchar2) => new Val.Constant(new Const.ConstUChar(uchar1.Value + uchar2.Value)),
                    (Const.ConstDouble double1, Const.ConstDouble double2) => new Val.Constant(new Const.ConstDouble(double1.Value + double2.Value)),
                    _ => throw new NotImplementedException(),
                };
            case Expression.BinaryOperator.Subtract:
                return (c1.Value, c2.Value)
                    switch
                {
                    (Const.ConstInt int1, Const.ConstInt int2) => new Val.Constant(new Const.ConstInt(int1.Value - int2.Value)),
                    (Const.ConstLong long1, Const.ConstLong long2) => new Val.Constant(new Const.ConstLong(long1.Value - long2.Value)),
                    (Const.ConstChar char1, Const.ConstChar char2) => new Val.Constant(new Const.ConstChar(char1.Value - char2.Value)),
                    (Const.ConstUInt uint1, Const.ConstUInt uint2) => new Val.Constant(new Const.ConstUInt(uint1.Value - uint2.Value)),
                    (Const.ConstULong ulong1, Const.ConstULong ulong2) => new Val.Constant(new Const.ConstULong(ulong1.Value - ulong2.Value)),
                    (Const.ConstUChar uchar1, Const.ConstUChar uchar2) => new Val.Constant(new Const.ConstUChar(uchar1.Value - uchar2.Value)),
                    (Const.ConstDouble double1, Const.ConstDouble double2) => new Val.Constant(new Const.ConstDouble(double1.Value - double2.Value)),
                    _ => throw new NotImplementedException(),
                };
            case Expression.BinaryOperator.Multiply:
                return (c1.Value, c2.Value)
                    switch
                {
                    (Const.ConstInt int1, Const.ConstInt int2) => new Val.Constant(new Const.ConstInt(int1.Value * int2.Value)),
                    (Const.ConstLong long1, Const.ConstLong long2) => new Val.Constant(new Const.ConstLong(long1.Value * long2.Value)),
                    (Const.ConstChar char1, Const.ConstChar char2) => new Val.Constant(new Const.ConstChar(char1.Value * char2.Value)),
                    (Const.ConstUInt uint1, Const.ConstUInt uint2) => new Val.Constant(new Const.ConstUInt(uint1.Value * uint2.Value)),
                    (Const.ConstULong ulong1, Const.ConstULong ulong2) => new Val.Constant(new Const.ConstULong(ulong1.Value * ulong2.Value)),
                    (Const.ConstUChar uchar1, Const.ConstUChar uchar2) => new Val.Constant(new Const.ConstUChar(uchar1.Value * uchar2.Value)),
                    (Const.ConstDouble double1, Const.ConstDouble double2) => new Val.Constant(new Const.ConstDouble(double1.Value * double2.Value)),
                    _ => throw new NotImplementedException(),
                };
            case Expression.BinaryOperator.Divide:
                if (GetValue(c2.Value) == 0 && c1.Value is not Const.ConstDouble)
                {
                    return new Val.Constant(c2.Value);
                }
                return (c1.Value, c2.Value)
                    switch
                {
                    (Const.ConstInt int1, Const.ConstInt int2) => new Val.Constant(new Const.ConstInt(int1.Value / int2.Value)),
                    (Const.ConstLong long1, Const.ConstLong long2) => new Val.Constant(new Const.ConstLong(long1.Value / long2.Value)),
                    (Const.ConstChar char1, Const.ConstChar char2) => new Val.Constant(new Const.ConstChar(char1.Value / char2.Value)),
                    (Const.ConstUInt uint1, Const.ConstUInt uint2) => new Val.Constant(new Const.ConstUInt(uint1.Value / uint2.Value)),
                    (Const.ConstULong ulong1, Const.ConstULong ulong2) => new Val.Constant(new Const.ConstULong(ulong1.Value / ulong2.Value)),
                    (Const.ConstUChar uchar1, Const.ConstUChar uchar2) => new Val.Constant(new Const.ConstUChar(uchar1.Value / uchar2.Value)),
                    (Const.ConstDouble double1, Const.ConstDouble double2) => new Val.Constant(new Const.ConstDouble(double1.Value / double2.Value)),
                    _ => throw new NotImplementedException(),
                };
            case Expression.BinaryOperator.Remainder:
                if (GetValue(c2.Value) == 0 && c1.Value is not Const.ConstDouble)
                {
                    return new Val.Constant(c1.Value);
                }
                return (c1.Value, c2.Value)
                    switch
                {
                    (Const.ConstInt int1, Const.ConstInt int2) => new Val.Constant(new Const.ConstInt(int1.Value % int2.Value)),
                    (Const.ConstLong long1, Const.ConstLong long2) => new Val.Constant(new Const.ConstLong(long1.Value % long2.Value)),
                    (Const.ConstChar char1, Const.ConstChar char2) => new Val.Constant(new Const.ConstChar(char1.Value % char2.Value)),
                    (Const.ConstUInt uint1, Const.ConstUInt uint2) => new Val.Constant(new Const.ConstUInt(uint1.Value % uint2.Value)),
                    (Const.ConstULong ulong1, Const.ConstULong ulong2) => new Val.Constant(new Const.ConstULong(ulong1.Value % ulong2.Value)),
                    (Const.ConstUChar uchar1, Const.ConstUChar uchar2) => new Val.Constant(new Const.ConstUChar(uchar1.Value % uchar2.Value)),
                    (Const.ConstDouble double1, Const.ConstDouble double2) => new Val.Constant(new Const.ConstDouble(double1.Value % double2.Value)),
                    _ => throw new NotImplementedException(),
                };
            case Expression.BinaryOperator.And:
                return (c1.Value, c2.Value)
                        switch
                {
                    (Const.ConstInt int1, Const.ConstInt int2) => new Val.Constant(new Const.ConstInt((int1.Value != 0 && int2.Value != 0) ? 1 : 0)),
                    (Const.ConstLong long1, Const.ConstLong long2) => new Val.Constant(new Const.ConstInt((long1.Value != 0 && long2.Value != 0) ? 1 : 0)),
                    (Const.ConstChar char1, Const.ConstChar char2) => new Val.Constant(new Const.ConstInt((char1.Value != 0 && char2.Value != 0) ? 1 : 0)),
                    (Const.ConstUInt uint1, Const.ConstUInt uint2) => new Val.Constant(new Const.ConstInt((uint1.Value != 0 && uint2.Value != 0) ? 1 : 0)),
                    (Const.ConstULong ulong1, Const.ConstULong ulong2) => new Val.Constant(new Const.ConstInt((ulong1.Value != 0 && ulong2.Value != 0) ? 1 : 0)),
                    (Const.ConstUChar uchar1, Const.ConstUChar uchar2) => new Val.Constant(new Const.ConstInt((uchar1.Value != 0 && uchar2.Value != 0) ? 1 : 0)),
                    (Const.ConstDouble double1, Const.ConstDouble double2) => new Val.Constant(new Const.ConstInt((double1.Value != 0 && double2.Value != 0) ? 1 : 0)),
                    _ => throw new NotImplementedException(),
                };
            case Expression.BinaryOperator.Or:
                return (c1.Value, c2.Value)
                        switch
                {
                    (Const.ConstInt int1, Const.ConstInt int2) => new Val.Constant(new Const.ConstInt((int1.Value != 0 || int2.Value != 0) ? 1 : 0)),
                    (Const.ConstLong long1, Const.ConstLong long2) => new Val.Constant(new Const.ConstInt((long1.Value != 0 || long2.Value != 0) ? 1 : 0)),
                    (Const.ConstChar char1, Const.ConstChar char2) => new Val.Constant(new Const.ConstInt((char1.Value != 0 || char2.Value != 0) ? 1 : 0)),
                    (Const.ConstUInt uint1, Const.ConstUInt uint2) => new Val.Constant(new Const.ConstInt((uint1.Value != 0 || uint2.Value != 0) ? 1 : 0)),
                    (Const.ConstULong ulong1, Const.ConstULong ulong2) => new Val.Constant(new Const.ConstInt((ulong1.Value != 0 || ulong2.Value != 0) ? 1 : 0)),
                    (Const.ConstUChar uchar1, Const.ConstUChar uchar2) => new Val.Constant(new Const.ConstInt((uchar1.Value != 0 || uchar2.Value != 0) ? 1 : 0)),
                    (Const.ConstDouble double1, Const.ConstDouble double2) => new Val.Constant(new Const.ConstInt((double1.Value != 0 || double2.Value != 0) ? 1 : 0)),
                    _ => throw new NotImplementedException(),
                };
            case Expression.BinaryOperator.Equal:
                return (c1.Value, c2.Value)
                    switch
                {
                    (Const.ConstInt int1, Const.ConstInt int2) => new Val.Constant(new Const.ConstInt(int1.Value == int2.Value ? 1 : 0)),
                    (Const.ConstLong long1, Const.ConstLong long2) => new Val.Constant(new Const.ConstInt(long1.Value == long2.Value ? 1 : 0)),
                    (Const.ConstChar char1, Const.ConstChar char2) => new Val.Constant(new Const.ConstInt(char1.Value == char2.Value ? 1 : 0)),
                    (Const.ConstUInt uint1, Const.ConstUInt uint2) => new Val.Constant(new Const.ConstInt(uint1.Value == uint2.Value ? 1 : 0)),
                    (Const.ConstULong ulong1, Const.ConstULong ulong2) => new Val.Constant(new Const.ConstInt(ulong1.Value == ulong2.Value ? 1 : 0)),
                    (Const.ConstUChar uchar1, Const.ConstUChar uchar2) => new Val.Constant(new Const.ConstInt(uchar1.Value == uchar2.Value ? 1 : 0)),
                    (Const.ConstDouble double1, Const.ConstDouble double2) => new Val.Constant(new Const.ConstInt(double1.Value == double2.Value ? 1 : 0)),
                    _ => throw new NotImplementedException(),
                };
            case Expression.BinaryOperator.NotEqual:
                return (c1.Value, c2.Value)
                    switch
                {
                    (Const.ConstInt int1, Const.ConstInt int2) => new Val.Constant(new Const.ConstInt(int1.Value != int2.Value ? 1 : 0)),
                    (Const.ConstLong long1, Const.ConstLong long2) => new Val.Constant(new Const.ConstInt(long1.Value != long2.Value ? 1 : 0)),
                    (Const.ConstChar char1, Const.ConstChar char2) => new Val.Constant(new Const.ConstInt(char1.Value != char2.Value ? 1 : 0)),
                    (Const.ConstUInt uint1, Const.ConstUInt uint2) => new Val.Constant(new Const.ConstInt(uint1.Value != uint2.Value ? 1 : 0)),
                    (Const.ConstULong ulong1, Const.ConstULong ulong2) => new Val.Constant(new Const.ConstInt(ulong1.Value != ulong2.Value ? 1 : 0)),
                    (Const.ConstUChar uchar1, Const.ConstUChar uchar2) => new Val.Constant(new Const.ConstInt(uchar1.Value != uchar2.Value ? 1 : 0)),
                    (Const.ConstDouble double1, Const.ConstDouble double2) => new Val.Constant(new Const.ConstInt(double1.Value != double2.Value ? 1 : 0)),
                    _ => throw new NotImplementedException(),
                };
            case Expression.BinaryOperator.LessThan:
                return (c1.Value, c2.Value)
                    switch
                {
                    (Const.ConstInt int1, Const.ConstInt int2) => new Val.Constant(new Const.ConstInt(int1.Value < int2.Value ? 1 : 0)),
                    (Const.ConstLong long1, Const.ConstLong long2) => new Val.Constant(new Const.ConstInt(long1.Value < long2.Value ? 1 : 0)),
                    (Const.ConstChar char1, Const.ConstChar char2) => new Val.Constant(new Const.ConstInt(char1.Value < char2.Value ? 1 : 0)),
                    (Const.ConstUInt uint1, Const.ConstUInt uint2) => new Val.Constant(new Const.ConstInt(uint1.Value < uint2.Value ? 1 : 0)),
                    (Const.ConstULong ulong1, Const.ConstULong ulong2) => new Val.Constant(new Const.ConstInt(ulong1.Value < ulong2.Value ? 1 : 0)),
                    (Const.ConstUChar uchar1, Const.ConstUChar uchar2) => new Val.Constant(new Const.ConstInt(uchar1.Value < uchar2.Value ? 1 : 0)),
                    (Const.ConstDouble double1, Const.ConstDouble double2) => new Val.Constant(new Const.ConstInt(double1.Value < double2.Value ? 1 : 0)),
                    _ => throw new NotImplementedException(),
                };
            case Expression.BinaryOperator.LessOrEqual:
                return (c1.Value, c2.Value)
                    switch
                {
                    (Const.ConstInt int1, Const.ConstInt int2) => new Val.Constant(new Const.ConstInt(int1.Value <= int2.Value ? 1 : 0)),
                    (Const.ConstLong long1, Const.ConstLong long2) => new Val.Constant(new Const.ConstInt(long1.Value <= long2.Value ? 1 : 0)),
                    (Const.ConstChar char1, Const.ConstChar char2) => new Val.Constant(new Const.ConstInt(char1.Value <= char2.Value ? 1 : 0)),
                    (Const.ConstUInt uint1, Const.ConstUInt uint2) => new Val.Constant(new Const.ConstInt(uint1.Value <= uint2.Value ? 1 : 0)),
                    (Const.ConstULong ulong1, Const.ConstULong ulong2) => new Val.Constant(new Const.ConstInt(ulong1.Value <= ulong2.Value ? 1 : 0)),
                    (Const.ConstUChar uchar1, Const.ConstUChar uchar2) => new Val.Constant(new Const.ConstInt(uchar1.Value <= uchar2.Value ? 1 : 0)),
                    (Const.ConstDouble double1, Const.ConstDouble double2) => new Val.Constant(new Const.ConstInt(double1.Value <= double2.Value ? 1 : 0)),
                    _ => throw new NotImplementedException(),
                };
            case Expression.BinaryOperator.GreaterThan:
                return (c1.Value, c2.Value)
                   switch
                {
                    (Const.ConstInt int1, Const.ConstInt int2) => new Val.Constant(new Const.ConstInt(int1.Value > int2.Value ? 1 : 0)),
                    (Const.ConstLong long1, Const.ConstLong long2) => new Val.Constant(new Const.ConstInt(long1.Value > long2.Value ? 1 : 0)),
                    (Const.ConstChar char1, Const.ConstChar char2) => new Val.Constant(new Const.ConstInt(char1.Value > char2.Value ? 1 : 0)),
                    (Const.ConstUInt uint1, Const.ConstUInt uint2) => new Val.Constant(new Const.ConstInt(uint1.Value > uint2.Value ? 1 : 0)),
                    (Const.ConstULong ulong1, Const.ConstULong ulong2) => new Val.Constant(new Const.ConstInt(ulong1.Value > ulong2.Value ? 1 : 0)),
                    (Const.ConstUChar uchar1, Const.ConstUChar uchar2) => new Val.Constant(new Const.ConstInt(uchar1.Value > uchar2.Value ? 1 : 0)),
                    (Const.ConstDouble double1, Const.ConstDouble double2) => new Val.Constant(new Const.ConstInt(double1.Value > double2.Value ? 1 : 0)),
                    _ => throw new NotImplementedException(),
                };
            case Expression.BinaryOperator.GreaterOrEqual:
                return (c1.Value, c2.Value)
                    switch
                {
                    (Const.ConstInt int1, Const.ConstInt int2) => new Val.Constant(new Const.ConstInt(int1.Value >= int2.Value ? 1 : 0)),
                    (Const.ConstLong long1, Const.ConstLong long2) => new Val.Constant(new Const.ConstInt(long1.Value >= long2.Value ? 1 : 0)),
                    (Const.ConstChar char1, Const.ConstChar char2) => new Val.Constant(new Const.ConstInt(char1.Value >= char2.Value ? 1 : 0)),
                    (Const.ConstUInt uint1, Const.ConstUInt uint2) => new Val.Constant(new Const.ConstInt(uint1.Value >= uint2.Value ? 1 : 0)),
                    (Const.ConstULong ulong1, Const.ConstULong ulong2) => new Val.Constant(new Const.ConstInt(ulong1.Value >= ulong2.Value ? 1 : 0)),
                    (Const.ConstUChar uchar1, Const.ConstUChar uchar2) => new Val.Constant(new Const.ConstInt(uchar1.Value >= uchar2.Value ? 1 : 0)),
                    (Const.ConstDouble double1, Const.ConstDouble double2) => new Val.Constant(new Const.ConstInt(double1.Value >= double2.Value ? 1 : 0)),
                    _ => throw new NotImplementedException(),
                };
            default:
                throw new NotImplementedException();
        }
    }

    private long GetValue(Const value)
    {
        return value switch
        {
            AST.Const.ConstInt constInt => (long)constInt.Value,
            AST.Const.ConstLong constLong => (long)constLong.Value,
            AST.Const.ConstUInt constUInt => (long)constUInt.Value,
            AST.Const.ConstULong constULong => (long)constULong.Value,
            AST.Const.ConstChar constChar => (long)constChar.Value,
            AST.Const.ConstUChar constUChar => (long)constUChar.Value,
            AST.Const.ConstDouble constDouble => (long)constDouble.Value,
            _ => throw new NotImplementedException()
        };
    }

    private Graph MakeControlFlowGraph(List<Instruction> instructions)
    {
        var partitions = PartitionIntoBasicBlocks(instructions);
        List<Node> blocks = [new Node.EntryNode([]), new Node.ExitNode([])];
        int counter = (int)Node.BlockId.START;
        foreach (var part in partitions)
        {
            blocks.Add(new Node.BasicBlock(counter++, part, [], []));
        }
        Graph graph = new Graph(blocks);
        AddAllEdges(graph);
        return graph;
    }

    private void AddAllEdges(Graph graph)
    {
        AddEdge(graph, (int)Node.BlockId.ENTRY, (int)Node.BlockId.START);

        foreach (var graphNode in graph.Nodes)
        {
            if (graphNode is Node.EntryNode or Node.ExitNode)
                continue;

            var node = (Node.BasicBlock)graphNode;
            int nextId = (node.Id == graph.Nodes.Count - 1) ? (int)Node.BlockId.EXIT : node.Id + 1;

            var inst = node.Instructions[^1];
            switch (inst)
            {
                case Instruction.Return ret:
                    AddEdge(graph, node.Id, (int)Node.BlockId.EXIT);
                    break;
                case Instruction.Jump jump:
                    var targetId = GetBlockByLabel(graph, jump.Target);
                    AddEdge(graph, node.Id, targetId);
                    break;
                case Instruction.JumpIfZero jumpIfZero:
                    targetId = GetBlockByLabel(graph, jumpIfZero.Target);
                    AddEdge(graph, node.Id, targetId);
                    AddEdge(graph, node.Id, nextId);
                    break;
                case Instruction.JumpIfNotZero jumpIfNotZero:
                    targetId = GetBlockByLabel(graph, jumpIfNotZero.Target);
                    AddEdge(graph, node.Id, targetId);
                    AddEdge(graph, node.Id, nextId);
                    break;
                default:
                    AddEdge(graph, node.Id, nextId);
                    break;
            }
        }
    }

    private int GetBlockByLabel(Graph graph, string label)
    {
        foreach (var node in graph.Nodes)
        {
            if (node is Node.BasicBlock basic && basic.Instructions[0] is Instruction.Label l && l.Identifier == label)
                return basic.Id;
        }
        throw new Exception($"Optimizer Error: Couldn't find block with label {label}");
    }

    private void AddEdge(Graph graph, int node1, int node2)
    {
        switch (graph.Nodes[node1])
        {
            case Node.EntryNode entry:
                entry.Successors.Add(node2);
                break;
            case Node.BasicBlock basic:
                basic.Successors.Add(node2);
                break;
        }

        switch (graph.Nodes[node2])
        {
            case Node.BasicBlock basic:
                basic.Predecessors.Add(node1);
                break;
            case Node.ExitNode entry:
                entry.Predecessors.Add(node1);
                break;
        }
    }

    private void RemoveEdge(Graph graph, int node1, int node2)
    {
        switch (FindNode(graph, node1))
        {
            case Node.EntryNode entry:
                entry.Successors.Remove(node2);
                break;
            case Node.BasicBlock basic:
                basic.Successors.Remove(node2);
                break;
        }

        switch (FindNode(graph, node2))
        {
            case Node.BasicBlock basic:
                basic.Predecessors.Remove(node1);
                break;
            case Node.ExitNode entry:
                entry.Predecessors.Remove(node1);
                break;
        }
    }

    private Node FindNode(Graph graph, int id)
    {
        if (id == (int)Node.BlockId.ENTRY)
            return graph.Nodes[(int)Node.BlockId.ENTRY];
        else if (id == (int)Node.BlockId.EXIT)
            return graph.Nodes[(int)Node.BlockId.EXIT];
        else
            foreach (var node in graph.Nodes)
            {
                if (node is Node.BasicBlock basic && basic.Id == id)
                    return basic;
            }
        throw new Exception($"Optimizer Error: Couldn't find block with id {id}");
    }

    private List<List<Instruction>> PartitionIntoBasicBlocks(List<Instruction> instructions)
    {
        List<List<Instruction>> finishedBlocks = [];
        List<Instruction> currentBlock = [];
        foreach (var inst in instructions)
        {
            if (inst is Instruction.Label)
            {
                if (currentBlock.Count != 0)
                    finishedBlocks.Add(currentBlock);
                currentBlock = [inst];
            }
            else if (inst is Instruction.Jump or Instruction.JumpIfZero or Instruction.JumpIfNotZero or Instruction.Return)
            {
                currentBlock.Add(inst);
                finishedBlocks.Add(currentBlock);
                currentBlock = [];
            }
            else
                currentBlock.Add(inst);
        }

        if (currentBlock.Count != 0)
            finishedBlocks.Add(currentBlock);

        return finishedBlocks;
    }

    private Graph UnreachableCodeElimination(Graph cfg)
    {
        HashSet<int> visited = [];
        FindReachableBlocks(cfg, (int)Node.BlockId.ENTRY, visited);
        var toRemove = cfg.Nodes.Where(a => a is Node.BasicBlock basic && !visited.Contains(basic.Id)).ToList();
        foreach (var node in toRemove)
        {
            if (node is Node.BasicBlock basic)
                while (basic.Successors.Count > 0)
                {
                    RemoveEdge(cfg, basic.Id, basic.Successors[0]);
                }
        }

        cfg.Nodes = cfg.Nodes.Where(a => (a is Node.BasicBlock basic && visited.Contains(basic.Id)) || a is Node.EntryNode or Node.ExitNode).ToList();

        RemoveRedundantJumps(cfg);
        RemoveUselessLabels(cfg);

        return cfg;
    }

    private void RemoveUselessLabels(Graph graph)
    {
        //note: make sure nodes are ordered by id
        int i = (int)Node.BlockId.START;
        while (i < graph.Nodes.Count)
        {
            var block = graph.Nodes[i];
            if (block is Node.BasicBlock basic && basic.Instructions.Count > 0 && basic.Instructions[0] is Instruction.Label)
            {
                bool keepLabel = false;
                var defaultPredecessor = graph.Nodes[i - 1];
                foreach (var predecessorId in basic.Predecessors)
                {
                    if (predecessorId != defaultPredecessor.Id && predecessorId != (int)Node.BlockId.ENTRY)
                    {
                        keepLabel = true;
                        break;
                    }
                }

                if (!keepLabel)
                    basic.Instructions.RemoveAt(0);
            }

            i += 1;
        }
    }

    private void RemoveRedundantJumps(Graph graph)
    {
        //note: make sure nodes are ordered by id
        int i = (int)Node.BlockId.START;
        while (i < graph.Nodes.Count - 1)
        {
            var block = graph.Nodes[i];
            if (block is Node.BasicBlock basic && basic.Instructions[^1] is Instruction.Jump or Instruction.JumpIfZero or Instruction.JumpIfNotZero)
            {
                bool keepJump = false;
                var defaultSuccessor = graph.Nodes[i + 1];
                foreach (var successorId in basic.Successors)
                {
                    if (successorId != defaultSuccessor.Id)
                    {
                        keepJump = true;
                        break;
                    }
                }

                if (!keepJump)
                    basic.Instructions.RemoveAt(basic.Instructions.Count - 1);
            }

            i += 1;
        }
    }

    private void FindReachableBlocks(Graph cfg, int start, HashSet<int> visited)
    {
        if (!visited.Add(start) || start == (int)Node.BlockId.EXIT)
            return;

        switch (cfg.Nodes[start])
        {
            case Node.EntryNode entry:
                foreach (var successor in entry.Successors)
                    FindReachableBlocks(cfg, successor, visited);
                break;
            case Node.BasicBlock basic:
                foreach (var successor in basic.Successors)
                    FindReachableBlocks(cfg, successor, visited);
                break;
        }
    }

    private Dictionary<(int blockId, int instIndex), List<Instruction.Copy>> annotatedCopyInstructions = [];
    private Dictionary<int, List<Instruction.Copy>> annotatedCopyBlocks = [];

    private Graph CopyPropagation(Graph cfg, List<Val.Variable> aliasedVars)
    {
        annotatedCopyBlocks.Clear();
        annotatedCopyInstructions.Clear();
        FindReachingCopies(cfg, aliasedVars);
        foreach (var node in cfg.Nodes)
        {
            if (node is Node.BasicBlock basic)
            {
                List<Instruction> newInstructions = [];
                for (int i = 0; i < basic.Instructions.Count; i++)
                {
                    var newInst = RewriteInstruction(basic.Instructions[i], i, basic.Id);
                    if (newInst != null)
                        newInstructions.Add(newInst);
                }
                basic.Instructions.Clear();
                basic.Instructions.AddRange(newInstructions);
            }
        }
        return cfg;
    }

    private Instruction? RewriteInstruction(Instruction instruction, int instIndex, int blockId)
    {
        List<Instruction.Copy> reachingCopies = GetInstructionCopyAnnotation(blockId, instIndex, instruction);
        switch (instruction)
        {
            case Instruction.Copy copyInst:
                {
                    foreach (var copy in reachingCopies)
                    {
                        if (copy == copyInst || (copy.Src == copyInst.Dst && copy.Dst == copyInst.Src))
                            return null;
                    }
                    Val newSrc = ReplaceOperand(copyInst.Src, reachingCopies);
                    return new Instruction.Copy(newSrc, copyInst.Dst);
                }
            case Instruction.Unary unary:
                {
                    Val newSrc = ReplaceOperand(unary.Src, reachingCopies);
                    return new Instruction.Unary(unary.UnaryOperator, newSrc, unary.Dst);
                }
            case Instruction.Binary binary:
                {
                    Val newSrc1 = ReplaceOperand(binary.Src1, reachingCopies);
                    Val newSrc2 = ReplaceOperand(binary.Src2, reachingCopies);
                    return new Instruction.Binary(binary.Operator, newSrc1, newSrc2, binary.Dst);
                }
            case Instruction.Return ret:
                {
                    if (ret.Value != null)
                    {
                        Val newSrc = ReplaceOperand(ret.Value, reachingCopies);
                        return new Instruction.Return(newSrc);
                    }
                    return ret;
                }
            case Instruction.JumpIfZero jz:
                {
                    Val newSrc = ReplaceOperand(jz.Condition, reachingCopies);
                    return new Instruction.JumpIfZero(newSrc, jz.Target);
                }
            case Instruction.JumpIfNotZero jnz:
                {
                    Val newSrc = ReplaceOperand(jnz.Condition, reachingCopies);
                    return new Instruction.JumpIfNotZero(newSrc, jnz.Target);
                }
            case Instruction.FunctionCall funCall:
                {
                    List<Val> newArgs = [];
                    foreach (var arg in funCall.Arguments)
                    {
                        Val newSrc = ReplaceOperand(arg, reachingCopies);
                        newArgs.Add(newSrc);
                    }
                    return new Instruction.FunctionCall(funCall.Identifier, newArgs, funCall.Dst);
                }
            case Instruction.SignExtend sext:
                {
                    Val newSrc = ReplaceOperand(sext.Src, reachingCopies);
                    return new Instruction.SignExtend(newSrc, sext.Dst);
                }
            case Instruction.Truncate trun:
                {
                    Val newSrc = ReplaceOperand(trun.Src, reachingCopies);
                    return new Instruction.Truncate(newSrc, trun.Dst);
                }
            case Instruction.ZeroExtend zext:
                {
                    Val newSrc = ReplaceOperand(zext.Src, reachingCopies);
                    return new Instruction.ZeroExtend(newSrc, zext.Dst);
                }
            case Instruction.DoubleToInt d2i:
                {
                    Val newSrc = ReplaceOperand(d2i.Src, reachingCopies);
                    return new Instruction.DoubleToInt(newSrc, d2i.Dst);
                }
            case Instruction.DoubleToUInt d2u:
                {
                    Val newSrc = ReplaceOperand(d2u.Src, reachingCopies);
                    return new Instruction.DoubleToUInt(newSrc, d2u.Dst);
                }
            case Instruction.IntToDouble i2d:
                {
                    Val newSrc = ReplaceOperand(i2d.Src, reachingCopies);
                    return new Instruction.IntToDouble(newSrc, i2d.Dst);
                }
            case Instruction.UIntToDouble u2d:
                {
                    Val newSrc = ReplaceOperand(u2d.Src, reachingCopies);
                    return new Instruction.UIntToDouble(newSrc, u2d.Dst);
                }
            case Instruction.Load load:
                {
                    Val newSrc = ReplaceOperand(load.SrcPtr, reachingCopies);
                    return new Instruction.Load(newSrc, load.Dst);
                }
            case Instruction.Store store:
                {
                    Val newSrc = ReplaceOperand(store.Src, reachingCopies);
                    return new Instruction.Store(newSrc, store.DstPtr);
                }
            case Instruction.AddPointer addp:
                {
                    Val newPtr = ReplaceOperand(addp.Pointer, reachingCopies);
                    Val newIndex = ReplaceOperand(addp.Index, reachingCopies);
                    return new Instruction.AddPointer(newPtr, newIndex, addp.Scale, addp.Dst);
                }
            case Instruction.CopyFromOffset cfo:
                {
                    Val newSrc = ReplaceOperand(new Val.Variable(cfo.Src), reachingCopies);
                    return new Instruction.CopyFromOffset(((Val.Variable)newSrc).Name, cfo.Offset, cfo.Dst);
                }
            case Instruction.CopyToOffset cto:
                {
                    Val newSrc = ReplaceOperand(cto.Src, reachingCopies);
                    return new Instruction.CopyToOffset(newSrc, cto.Dst, cto.Offset);
                }
            default:
                return instruction;
        }
    }

    private Val ReplaceOperand(Val op, List<Instruction.Copy> reachingCopies)
    {
        if (op is Val.Constant)
            return op;

        foreach (var copy in reachingCopies)
            if (copy.Dst == op)
                return copy.Src;

        return op;
    }

    private List<Instruction.Copy> GetInstructionCopyAnnotation(int blockId, int instIndex, Instruction instruction)
    {
        if (annotatedCopyInstructions.TryGetValue((blockId, instIndex), out var result))
            return result;
        throw new Exception($"Optimizer Error: Couldn't find annotated instruction");
    }

    private void FindReachingCopies(Graph graph, List<Val.Variable> aliasedVars)
    {
        List<Instruction.Copy> allCopies = graph.Nodes.FindAll(a => a is Node.BasicBlock)
                                                      .SelectMany(a => ((Node.BasicBlock)a).Instructions)
                                                      .Where(a => a is Instruction.Copy)
                                                      .Select(a => (Instruction.Copy)a)
                                                      .ToList();
        Queue<Node.BasicBlock> workList = [];

        foreach (var node in graph.Nodes)
        {
            if (node is Node.EntryNode or Node.ExitNode)
                continue;

            workList.Enqueue((Node.BasicBlock)node);
            AnnotateCopyBlock(node.Id, allCopies);
        }

        while (workList.Count > 0)
        {
            var block = workList.Dequeue();
            var oldAnnotation = GetCopyBlockAnnotation(block.Id);
            var incomingCopies = Meet(graph, block, allCopies);
            Transfer(block, incomingCopies, aliasedVars);
            if (!oldAnnotation.SequenceEqual(GetCopyBlockAnnotation(block.Id)))
            {
                foreach (var succId in block.Successors)
                {
                    switch (FindNode(graph, succId))
                    {
                        case Node.ExitNode:
                            continue;
                        case Node.EntryNode:
                            throw new Exception("Optimizer Error: Malformed control-flow graph");
                        case Node.BasicBlock basic:
                            var successor = FindNode(graph, succId);
                            if (successor is Node.BasicBlock succ && !workList.Contains(succ))
                                workList.Enqueue(succ);
                            break;
                    }
                }
            }
        }
    }

    private List<Instruction.Copy> Meet(Graph graph, Node.BasicBlock block, List<Instruction.Copy> allCopies)
    {
        List<Instruction.Copy> incomingCopies = new(allCopies);
        foreach (var predId in block.Predecessors)
        {
            switch (FindNode(graph, predId))
            {
                case Node.EntryNode entry:
                    return [];
                case Node.BasicBlock basic:
                    {
                        var predOutCopies = GetCopyBlockAnnotation(predId);
                        incomingCopies = incomingCopies.Intersect(predOutCopies).ToList();
                    }
                    break;
                case Node.ExitNode:
                    throw new Exception("Optimizer Error: Malformed control-flow graph");
            }
        }
        return incomingCopies;
    }

    private List<Instruction.Copy> GetCopyBlockAnnotation(int predId)
    {
        if (annotatedCopyBlocks.TryGetValue(predId, out var result))
            return result;
        throw new Exception($"Optimizer Error: Couldn't find annotated block id {predId}");
    }

    private void Transfer(Node.BasicBlock block, List<Instruction.Copy> initialReachingCopies, List<Val.Variable> aliasedVars)
    {
        List<Instruction.Copy> currentReachingCopies = new(initialReachingCopies);

        for (int i = 0; i < block.Instructions.Count; i++)
        {
            Instruction? inst = block.Instructions[i];
            AnnotateCopyInstruction(block.Id, i, currentReachingCopies);

            switch (inst)
            {
                case Instruction.Copy copyInst:
                    {
                        if (currentReachingCopies.Contains(new Instruction.Copy(copyInst.Dst, copyInst.Src)))
                            continue;

                        KillCopy(currentReachingCopies, copyInst.Dst);

                        if ((GetType(copyInst.Src) == GetType(copyInst.Dst)) || (TypeChecker.IsSignedType(GetType(copyInst.Src)) == TypeChecker.IsSignedType(GetType(copyInst.Dst))))
                            currentReachingCopies.Add(copyInst);
                    }
                    break;
                case Instruction.FunctionCall funCall:
                    {
                        for (int i1 = 0; i1 < currentReachingCopies.Count; i1++)
                        {
                            Instruction.Copy? copy = currentReachingCopies[i1];
                            if (IsAliased(aliasedVars, copy.Src) ||
                                IsAliased(aliasedVars, copy.Dst) ||
                                (copy.Dst != null &&
                                (copy.Src == funCall.Dst ||
                                copy.Dst == funCall.Dst)))
                            {
                                currentReachingCopies.Remove(copy);
                                i1--;
                            }
                        }
                    }
                    break;
                case Instruction.Store store:
                    for (int i1 = 0; i1 < currentReachingCopies.Count; i1++)
                    {
                        Instruction.Copy? copy = currentReachingCopies[i1];
                        if (IsAliased(aliasedVars, copy.Src) ||
                            IsAliased(aliasedVars, copy.Dst))
                        {
                            currentReachingCopies.Remove(copy);
                            i1--;
                        }
                    }
                    break;
                case Instruction.Unary unary:
                    KillCopy(currentReachingCopies, unary.Dst);
                    break;
                case Instruction.Binary binary:
                    KillCopy(currentReachingCopies, binary.Dst);
                    break;
                case Instruction.SignExtend sext:
                    KillCopy(currentReachingCopies, sext.Dst);
                    break;
                case Instruction.Truncate trun:
                    KillCopy(currentReachingCopies, trun.Dst);
                    break;
                case Instruction.ZeroExtend zext:
                    KillCopy(currentReachingCopies, zext.Dst);
                    break;
                case Instruction.DoubleToInt d2i:
                    KillCopy(currentReachingCopies, d2i.Dst);
                    break;
                case Instruction.DoubleToUInt d2u:
                    KillCopy(currentReachingCopies, d2u.Dst);
                    break;
                case Instruction.IntToDouble i2d:
                    KillCopy(currentReachingCopies, i2d.Dst);
                    break;
                case Instruction.UIntToDouble u2d:
                    KillCopy(currentReachingCopies, u2d.Dst);
                    break;
                case Instruction.GetAddress addr:
                    KillCopy(currentReachingCopies, addr.Dst);
                    break;
                case Instruction.Load load:
                    KillCopy(currentReachingCopies, load.Dst);
                    break;
                case Instruction.AddPointer addp:
                    KillCopy(currentReachingCopies, addp.Dst);
                    break;
                case Instruction.CopyFromOffset cfo:
                    KillCopy(currentReachingCopies, cfo.Dst);
                    break;
                case Instruction.CopyToOffset cto:
                    KillCopy(currentReachingCopies, new Val.Variable(cto.Dst));
                    break;
                default:
                    continue;
            }
        }

        AnnotateCopyBlock(block.Id, currentReachingCopies);
    }

    private bool IsAliased(List<Val.Variable> aliasedVars, Val val)
    {
        return aliasedVars.Contains(val) || IsStatic(val);
    }

    private void KillCopy(List<Instruction.Copy> currentReachingCopies, Val Dst)
    {
        for (int i = 0; i < currentReachingCopies.Count; i++)
        {
            Instruction.Copy? copy = currentReachingCopies[i];
            if (copy.Src == Dst || copy.Dst == Dst)
            {
                currentReachingCopies.Remove(copy);
                i--;
            }
        }
    }

    private void AnnotateCopyBlock(int id, List<Instruction.Copy> currentReachingCopies)
    {
        annotatedCopyBlocks[id] = new(currentReachingCopies);
    }

    private bool IsStatic(Val src)
    {
        return src is Val.Variable var && SemanticAnalyzer.SymbolTable.TryGetValue(var.Name, out var entry) && entry.IdentifierAttributes is IdentifierAttributes.Static;
    }

    private void AnnotateCopyInstruction(int blockId, int instIndex, List<Instruction.Copy> reachingCopies)
    {
        annotatedCopyInstructions[(blockId, instIndex)] = new(reachingCopies);
    }

    private Dictionary<(int blockId, int instIndex), List<Val.Variable>> annotatedLiveInstructions = [];
    private Dictionary<int, List<Val.Variable>> annotatedLiveBlocks = [];

    private Graph DeadStoreElimination(Graph cfg, List<Val.Variable> aliasedVars)
    {
        var staticVars = FindAllStaticVariables();
        annotatedLiveBlocks.Clear();
        annotatedLiveInstructions.Clear();

        LivenessAnalysis(cfg, staticVars, aliasedVars);
        foreach (var node in cfg.Nodes)
        {
            if (node is Node.BasicBlock basic)
            {
                List<Instruction> newInstructions = [];
                for (int i = 0; i < basic.Instructions.Count; i++)
                {
                    if (!IsDeadStore(basic.Instructions[i], basic.Id, i))
                        newInstructions.Add(basic.Instructions[i]);
                }
                basic.Instructions.Clear();
                basic.Instructions.AddRange(newInstructions);
            }
        }
        return cfg;
    }

    private bool IsDeadStore(Instruction instruction, int blockId, int instIndex)
    {
        if (instruction is Instruction.FunctionCall or Instruction.Store)
            return false;

        List<Val.Variable> liveVars = GetInstructionLiveAnnotation(blockId, instIndex);
        switch (instruction)
        {
            case Instruction.Binary binary:
                {
                    if (!liveVars.Contains(binary.Dst))
                        return true;
                    break;
                }
            case Instruction.Unary unary:
                {
                    if (!liveVars.Contains(unary.Dst))
                        return true;
                    break;
                }
            case Instruction.Copy copy:
                {
                    if (copy.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.SignExtend se:
                {
                    if (se.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.Truncate trun:
                {
                    if (trun.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.ZeroExtend ze:
                {
                    if (ze.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.DoubleToInt d2i:
                {
                    if (d2i.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.DoubleToUInt d2u:
                {
                    if (d2u.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.IntToDouble i2d:
                {
                    if (i2d.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.UIntToDouble u2d:
                {
                    if (u2d.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.GetAddress getA:
                {
                    if (getA.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.Load load:
                {
                    if (load.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.AddPointer addP:
                {
                    if (addP.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.CopyToOffset cto:
                {
                    if (!liveVars.Contains(new Val.Variable(cto.Dst)))
                        return true;
                    break;
                }
            case Instruction.CopyFromOffset cfo:
                {
                    if (cfo.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
        }

        return false;
    }

    private List<Val.Variable> GetInstructionLiveAnnotation(int blockId, int instIndex)
    {
        if (annotatedLiveInstructions.TryGetValue((blockId, instIndex), out var result))
            return result;
        throw new Exception($"Optimizer Error: Couldn't find annotated instruction");
    }

    private void LivenessAnalysis(Graph graph, List<Val.Variable> allStaticVariables, List<Val.Variable> aliasedVars)
    {
        List<Val.Variable> staticAndAliasedVars = allStaticVariables.Union(aliasedVars).ToList();
        List<Val.Variable> allLive = [];
        Queue<Node.BasicBlock> workList = [];

        foreach (var node in graph.Nodes)
        {
            if (node is Node.EntryNode or Node.ExitNode)
                continue;

            workList.Enqueue((Node.BasicBlock)node);
            AnnotateLiveBlock(node.Id, allLive);
        }

        while (workList.Count > 0)
        {
            var block = workList.Dequeue();
            var oldAnnotation = GetLiveBlockAnnotation(block.Id);
            var incomingCopies = Meet(graph, block, allStaticVariables);
            Transfer(block, incomingCopies, staticAndAliasedVars);
            if (!new HashSet<Val.Variable>(oldAnnotation).SetEquals(GetLiveBlockAnnotation(block.Id)))
            {
                foreach (var predId in block.Predecessors)
                {
                    switch (FindNode(graph, predId))
                    {
                        case Node.ExitNode:
                            throw new Exception("Optimizer Error: Malformed control-flow graph");
                        case Node.EntryNode:
                            continue;
                        case Node.BasicBlock basic:
                            var predecessor = FindNode(graph, predId);
                            if (predecessor is Node.BasicBlock pred && !workList.Contains(pred))
                                workList.Enqueue(pred);
                            break;
                    }
                }
            }
        }
    }

    private void AnnotateLiveBlock(int id, List<Val.Variable> allLive)
    {
        annotatedLiveBlocks[id] = new(allLive);
    }

    private List<Val.Variable> Meet(Graph graph, Node.BasicBlock block, List<Val.Variable> allStaticVariables)
    {
        List<Val.Variable> liveVars = [];
        foreach (var succId in block.Successors)
        {
            switch (FindNode(graph, succId))
            {
                case Node.ExitNode:
                    liveVars = liveVars.Union(allStaticVariables).ToList();
                    break;
                case Node.EntryNode:
                    throw new Exception("Optimizer Error: Malformed control-flow graph");
                case Node.BasicBlock basic:
                    var succLiveVars = GetLiveBlockAnnotation(succId);
                    liveVars = liveVars.Union(succLiveVars).ToList();
                    break;
            }
        }
        return liveVars;
    }

    private List<Val.Variable> GetLiveBlockAnnotation(int succId)
    {
        if (annotatedLiveBlocks.TryGetValue(succId, out var result))
            return result;
        throw new Exception($"Optimizer Error: Couldn't find annotated block id {succId}");
    }

    private void Transfer(Node.BasicBlock block, List<Val.Variable> endLiveVariables, List<Val.Variable> allStaticVariables)
    {
        List<Val.Variable> currentLiveVariables = new(endLiveVariables);

        for (int i = block.Instructions.Count - 1; i >= 0; i--)
        {
            AnnotateLiveInstruction(block.Id, i, currentLiveVariables);

            switch (block.Instructions[i])
            {
                case Instruction.Binary binary:
                    {
                        currentLiveVariables.Remove(binary.Dst);
                        if (binary.Src1 is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        if (binary.Src2 is Val.Variable var2)
                            currentLiveVariables.Add(var2);
                        break;
                    }
                case Instruction.Unary unary:
                    {
                        currentLiveVariables.Remove(unary.Dst);
                        if (unary.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.JumpIfZero jz:
                    {
                        if (jz.Condition is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.JumpIfNotZero jnz:
                    {
                        if (jnz.Condition is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.Copy copy:
                    {
                        if (copy.Dst is Val.Variable var)
                            currentLiveVariables.Remove(var);
                        if (copy.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.FunctionCall funCall:
                    {
                        if (funCall.Dst != null && funCall.Dst is Val.Variable dst)
                            currentLiveVariables.Remove(dst);
                        foreach (var arg in funCall.Arguments)
                        {
                            if (arg is Val.Variable var)
                                currentLiveVariables.Add(var);
                        }
                        currentLiveVariables = currentLiveVariables.Union(allStaticVariables).ToList();
                        break;
                    }
                case Instruction.Return ret:
                    {
                        if (ret.Value != null && ret.Value is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.SignExtend se:
                    {
                        if (se.Dst is Val.Variable var)
                            currentLiveVariables.Remove(var);
                        if (se.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.ZeroExtend ze:
                    {
                        if (ze.Dst is Val.Variable var)
                            currentLiveVariables.Remove(var);
                        if (ze.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.Truncate trun:
                    {
                        if (trun.Dst is Val.Variable var)
                            currentLiveVariables.Remove(var);
                        if (trun.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.DoubleToInt d2i:
                    {
                        if (d2i.Dst is Val.Variable var)
                            currentLiveVariables.Remove(var);
                        if (d2i.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.DoubleToUInt d2u:
                    {
                        if (d2u.Dst is Val.Variable var)
                            currentLiveVariables.Remove(var);
                        if (d2u.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.IntToDouble i2d:
                    {
                        if (i2d.Dst is Val.Variable var)
                            currentLiveVariables.Remove(var);
                        if (i2d.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.UIntToDouble u2d:
                    {
                        if (u2d.Dst is Val.Variable var)
                            currentLiveVariables.Remove(var);
                        if (u2d.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.AddPointer addP:
                    {
                        if (addP.Dst is Val.Variable var)
                            currentLiveVariables.Remove(var);
                        if (addP.Pointer is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        if (addP.Index is Val.Variable var2)
                            currentLiveVariables.Add(var2);
                        break;
                    }
                case Instruction.GetAddress getA:
                    {
                        if (getA.Dst is Val.Variable var)
                            currentLiveVariables.Remove(var);
                        break;
                    }
                case Instruction.Load load:
                    {
                        if (load.Dst is Val.Variable dst)
                            currentLiveVariables.Remove(dst);
                        if (load.SrcPtr is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        currentLiveVariables = currentLiveVariables.Union(allStaticVariables).ToList();
                        break;
                    }
                case Instruction.Store store:
                    {
                        if (store.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        if (store.DstPtr is Val.Variable var2)
                            currentLiveVariables.Add(var2);
                        break;
                    }
                case Instruction.CopyToOffset cto:
                    {
                        if (cto.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.CopyFromOffset cfo:
                    {
                        if (cfo.Dst is Val.Variable dst)
                            currentLiveVariables.Remove(dst);
                        currentLiveVariables.Add(new Val.Variable(cfo.Src));
                        break;
                    }
            }
        }

        AnnotateLiveBlock(block.Id, currentLiveVariables);
    }

    private void AnnotateLiveInstruction(int id, int i, List<Val.Variable> currentLiveVariables)
    {
        annotatedLiveInstructions[(id, i)] = new(currentLiveVariables);
    }

    private List<Val.Variable> FindAllStaticVariables()
    {
        //note: we add too many static variables that may not even be used in the block
        //      we could instead scan every instruction in a block and add them
        List<Val.Variable> result = [];
        foreach (var entry in SemanticAnalyzer.SymbolTable)
        {
            if (entry.Value.IdentifierAttributes is IdentifierAttributes.Static)
                result.Add(new Val.Variable(entry.Key));
        }
        return result;
    }

    private List<Instruction> ControlFlowGraphToInstructions(Graph cfg)
    {
        return cfg.Nodes[(int)Node.BlockId.START..].SelectMany(a => ((Node.BasicBlock)a).Instructions).ToList();
    }
}