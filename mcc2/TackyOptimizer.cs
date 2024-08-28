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
            List<Instruction> postConstantFolding = optimizations.HasFlag(CompilerDriver.Optimizations.FoldConstants) ? ConstantFolding(instructions) : instructions;

            Graph cfg = MakeControlFlowGraph(postConstantFolding);

            if (optimizations.HasFlag(CompilerDriver.Optimizations.EliminateUnreachableCode))
                cfg = UnreachableCodeElimination(cfg);

            if (optimizations.HasFlag(CompilerDriver.Optimizations.PropagateCopies))
                cfg = CopyPropagation(cfg);

            if (optimizations.HasFlag(CompilerDriver.Optimizations.EliminateDeadStores))
                cfg = DeadStoreElimination(cfg);

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
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstDouble((double)c.Value)), i2d.Dst));
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
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstDouble((double)c.Value)), u2d.Dst));
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
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstChar((char)dval.Value)), d2i.Dst));
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
                                    result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstInt((int)smallC.Value)), se.Dst));
                                    break;
                                case Type.UInt:
                                    result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstUInt((uint)(int)smallC.Value)), se.Dst));
                                    break;
                                case Type.Long:
                                    result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstLong((long)smallC.Value)), se.Dst));
                                    break;
                                case Type.ULong:
                                    result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstULong((ulong)(long)smallC.Value)), se.Dst));
                                    break;
                            }
                        }
                        else if (seConst.Value is Const.ConstInt smallI)
                        {
                            if (GetType(se.Dst) is Type.Long)
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstLong((long)smallI.Value)), se.Dst));
                            else
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstULong((ulong)(long)smallI.Value)), se.Dst));
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
                                    result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstInt((int)(uint)smallC.Value)), ze.Dst));
                                    break;
                                case Type.UInt:
                                    result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstUInt((uint)smallC.Value)), ze.Dst));
                                    break;
                                case Type.Long:
                                    result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstLong((long)(ulong)smallC.Value)), ze.Dst));
                                    break;
                                case Type.ULong:
                                    result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstULong((ulong)smallC.Value)), ze.Dst));
                                    break;
                            }
                        }
                        else if (zeConst.Value is Const.ConstUInt smallI)
                        {
                            if (GetType(ze.Dst) is Type.Long)
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstLong((long)(ulong)smallI.Value)), ze.Dst));
                            else
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstULong((ulong)smallI.Value)), ze.Dst));
                        }
                        else
                            result.Add(inst);
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
                if (GetValue(c2.Value) == 0)
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
                if (GetValue(c2.Value) == 0)
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
            AST.Const.ConstDouble constDouble => (ulong)constDouble.Value,
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

    private Dictionary<(int blockId, int instIndex), List<Instruction.Copy>> annotatedInstructions = [];
    private Dictionary<int, List<Instruction.Copy>> annotatedBlocks = [];

    private Graph CopyPropagation(Graph cfg)
    {
        annotatedBlocks.Clear();
        annotatedInstructions.Clear();
        FindReachingCopies(cfg);
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
        List<Instruction.Copy> reachingCopies = GetInstructionAnnotation(blockId, instIndex, instruction);
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
            //todo: other instructions
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

    private List<Instruction.Copy> GetInstructionAnnotation(int blockId, int instIndex, Instruction instruction)
    {
        if (annotatedInstructions.TryGetValue((blockId, instIndex), out var result))
            return result;
        throw new Exception($"Optimizer Error: Couldn't find annotated instruction");
    }

    private void FindReachingCopies(Graph graph)
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
            AnnotateBlock(node.Id, allCopies);
        }

        while (workList.Count > 0)
        {
            var block = workList.Dequeue();
            var oldAnnotation = GetBlockAnnotation(block.Id);
            var incomingCopies = Meet(graph, block, allCopies);
            Transfer(block, incomingCopies);
            if (!oldAnnotation.SequenceEqual(GetBlockAnnotation(block.Id)))
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
                        var predOutCopies = GetBlockAnnotation(predId);
                        incomingCopies = incomingCopies.Intersect(predOutCopies).ToList();
                    }
                    break;
                case Node.ExitNode:
                    throw new Exception("Optimizer Error: Malformed control-flow graph");
            }
        }
        return incomingCopies;
    }

    private List<Instruction.Copy> GetBlockAnnotation(int predId)
    {
        if (annotatedBlocks.TryGetValue(predId, out var result))
            return result;
        throw new Exception($"Optimizer Error: Couldn't find annotated block id {predId}");
    }

    private void Transfer(Node.BasicBlock block, List<Instruction.Copy> initialReachingCopies)
    {
        List<Instruction.Copy> currentReachingCopies = new(initialReachingCopies);

        for (int i = 0; i < block.Instructions.Count; i++)
        {
            Instruction? inst = block.Instructions[i];
            AnnotateInstruction(block.Id, i, currentReachingCopies);

            switch (inst)
            {
                case Instruction.Copy copyInst:
                    {
                        if (currentReachingCopies.Contains(new Instruction.Copy(copyInst.Dst, copyInst.Src)))
                            continue;

                        for (int i1 = 0; i1 < currentReachingCopies.Count; i1++)
                        {
                            Instruction.Copy? copy = currentReachingCopies[i1];
                            if (copy.Src == copyInst.Dst || copy.Dst == copyInst.Dst)
                            {
                                currentReachingCopies.Remove(copy);
                                i1--;
                            }
                        }

                        currentReachingCopies.Add(copyInst);
                    }
                    break;
                case Instruction.FunctionCall funCall:
                    {
                        for (int i1 = 0; i1 < currentReachingCopies.Count; i1++)
                        {
                            Instruction.Copy? copy = currentReachingCopies[i1];
                            if (IsStatic(copy.Src) || IsStatic(copy.Dst) ||
                                copy.Src == funCall.Dst || copy.Dst == funCall.Dst)
                            {
                                currentReachingCopies.Remove(copy);
                                i1--;
                            }
                        }
                    }
                    break;
                case Instruction.Unary unary:
                    {
                        for (int i1 = 0; i1 < currentReachingCopies.Count; i1++)
                        {
                            Instruction.Copy? copy = currentReachingCopies[i1];
                            if (copy.Src == unary.Dst || copy.Dst == unary.Dst)
                            {
                                currentReachingCopies.Remove(copy);
                                i1--;
                            }
                        }
                    }
                    break;
                case Instruction.Binary binary:
                    {
                        for (int i1 = 0; i1 < currentReachingCopies.Count; i1++)
                        {
                            Instruction.Copy? copy = currentReachingCopies[i1];
                            if (copy.Src == binary.Dst || copy.Dst == binary.Dst)
                            {
                                currentReachingCopies.Remove(copy);
                                i1--;
                            }
                        }
                    }
                    break;
                default:
                    continue;
            }
        }

        AnnotateBlock(block.Id, currentReachingCopies);
    }

    private void AnnotateBlock(int id, List<Instruction.Copy> currentReachingCopies)
    {
        annotatedBlocks[id] = new(currentReachingCopies);
    }

    private bool IsStatic(Val src)
    {
        return src is Val.Variable var && SemanticAnalyzer.SymbolTable.TryGetValue(var.Name, out var entry) && entry.IdentifierAttributes is IdentifierAttributes.Static;
    }

    private void AnnotateInstruction(int blockId, int instIndex, List<Instruction.Copy> reachingCopies)
    {
        annotatedInstructions[(blockId, instIndex)] = new(reachingCopies);
    }

    private Graph DeadStoreElimination(Graph cfg)
    {
        return cfg;
    }

    private List<Instruction> ControlFlowGraphToInstructions(Graph cfg)
    {
        return cfg.Nodes[(int)Node.BlockId.START..].SelectMany(a => ((Node.BasicBlock)a).Instructions).ToList();
    }
}