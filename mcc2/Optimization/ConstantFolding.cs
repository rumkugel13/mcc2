using mcc2.AST;
using mcc2.TAC;

namespace mcc2;

public class ConstantFolding
{
    public List<Instruction> Fold(List<Instruction> instructions)
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
                                case Type.ULong or Type.Pointer:
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
                                case Type.ULong or Type.Pointer:
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
                case Instruction.Copy copy:
                    if (copy.Src is Val.Constant copyConst)
                    {
                        switch (copyConst.Value)
                        {
                            case Const.ConstChar constChar when GetType(copy.Dst) is Type.UChar:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstUChar(constChar.Value)), copy.Dst));
                                break;
                            case Const.ConstUChar constUChar when GetType(copy.Dst) is Type.Char or Type.SChar:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstChar(constUChar.Value)), copy.Dst));
                                break;
                            case Const.ConstInt constInt when GetType(copy.Dst) is Type.UInt:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstUInt((uint)constInt.Value)), copy.Dst));
                                break;
                            case Const.ConstUInt constUInt when GetType(copy.Dst) is Type.Int:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstInt((int)constUInt.Value)), copy.Dst));
                                break;
                            case Const.ConstLong constLong when GetType(copy.Dst) is Type.ULong:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstULong((ulong)constLong.Value)), copy.Dst));
                                break;
                            case Const.ConstULong constULong when GetType(copy.Dst) is Type.Long:
                                result.Add(new Instruction.Copy(new Val.Constant(new Const.ConstLong((long)constULong.Value)), copy.Dst));
                                break;
                            default:
                                result.Add(inst);
                                break;
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

    public static Type GetType(TAC.Val val)
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
}