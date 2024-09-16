using mcc2.Assembly;

namespace mcc2;

public class InstructionFixer
{
    public List<Instruction> Fix(IReadOnlyList<Instruction> instructions, string functionName, long bytesForLocals)
    {
        List<Instruction> result = new(instructions.Count);
        var calleeSavedRegs = RegisterAllocator.CalleeSavedRegs[functionName];
        var allocateBytes = CalculateStackAdjustment(bytesForLocals, calleeSavedRegs.Count);
        result.Add(new Instruction.Binary(Instruction.BinaryOperator.Sub, new AssemblyType.Quadword(),
            new Operand.Imm((ulong)allocateBytes), new Operand.Reg(Operand.RegisterName.SP)));

        for (int i = 0; i < calleeSavedRegs.Count; i++)
        {
            result.Add(new Instruction.Push(new Operand.Reg(calleeSavedRegs[i])));
        }

        for (int i = 0; i < instructions.Count; i++)
        {
            Operand.Reg srcReg = new Operand.Reg(Operand.RegisterName.R10);
            Operand.Reg dstReg = new Operand.Reg(Operand.RegisterName.R11);
            Operand.Reg srcFloatReg = new Operand.Reg(Operand.RegisterName.XMM14);
            Operand.Reg dstFloatReg = new Operand.Reg(Operand.RegisterName.XMM15);

            var inst = instructions[i];
            switch (inst)
            {
                case Instruction.Mov mov:
                    {
                        if (IsMemory(mov.Src) && IsMemory(mov.Dst))
                        {
                            var regUsed = srcReg;
                            if (mov.Type is AssemblyType.Double)
                                regUsed = srcFloatReg;
                            result.Add(new Instruction.Mov(mov.Type, mov.Src, regUsed));
                            result.Add(new Instruction.Mov(mov.Type, regUsed, mov.Dst));
                        }
                        else if (mov.Type is AssemblyType.Quadword && mov.Src is Operand.Imm immSrc && IsMemory(mov.Dst) && IsLargeInt(immSrc))
                        {
                            result.Add(new Instruction.Mov(new AssemblyType.Quadword(), mov.Src, srcReg));
                            result.Add(new Instruction.Mov(new AssemblyType.Quadword(), srcReg, mov.Dst));
                        }
                        else if (mov.Type is AssemblyType.Longword && mov.Src is Operand.Imm immSrc2 &&
                            (immSrc2.Value > uint.MaxValue || (long)immSrc2.Value < int.MinValue))
                        {
                            result.Add(new Instruction.Mov(new AssemblyType.Longword(), new Operand.Imm((uint)immSrc2.Value), mov.Dst));
                        }
                        else if (mov.Type is AssemblyType.Byte && mov.Src is Operand.Imm immSrcb && IsLargeByte(immSrcb))
                        {
                            result.Add(new Instruction.Mov(new AssemblyType.Byte(), new Operand.Imm((byte)immSrcb.Value), mov.Dst));
                        }
                        else
                            result.Add(inst);

                        break;
                    }

                case Instruction.Cmp cmp:
                    {
                        if (cmp.Type is AssemblyType.Double && cmp.OperandB is not Operand.Reg)
                        {
                            result.Add(new Instruction.Mov(new AssemblyType.Double(), cmp.OperandB, dstFloatReg));
                            result.Add(new Instruction.Cmp(new AssemblyType.Double(), cmp.OperandA, dstFloatReg));
                        }
                        else if (IsMemory(cmp.OperandA) && IsMemory(cmp.OperandB))
                        {
                            result.Add(new Instruction.Mov(cmp.Type, cmp.OperandA, srcReg));
                            result.Add(new Instruction.Cmp(cmp.Type, srcReg, cmp.OperandB));
                        }
                        else if (cmp.Type is AssemblyType.Quadword && cmp.OperandA is Operand.Imm immA && cmp.OperandB is not Operand.Imm && IsLargeInt(immA))
                        {
                            result.Add(new Instruction.Mov(new AssemblyType.Quadword(), cmp.OperandA, srcReg));
                            result.Add(new Instruction.Cmp(new AssemblyType.Quadword(), srcReg, cmp.OperandB));
                        }
                        else if (cmp.Type is AssemblyType.Quadword && cmp.OperandA is Operand.Imm immA2 && cmp.OperandB is Operand.Imm && IsLargeInt(immA2))
                        {
                            result.Add(new Instruction.Mov(new AssemblyType.Quadword(), cmp.OperandA, srcReg));
                            result.Add(new Instruction.Mov(new AssemblyType.Quadword(), cmp.OperandB, dstReg));
                            result.Add(new Instruction.Cmp(new AssemblyType.Quadword(), srcReg, dstReg));
                        }
                        else if (cmp.OperandB is Operand.Imm imm)
                        {
                            result.Add(new Instruction.Mov(cmp.Type, cmp.OperandB, dstReg));
                            result.Add(new Instruction.Cmp(cmp.Type, cmp.OperandA, dstReg));
                        }
                        else
                            result.Add(inst);

                        break;
                    }

                case Instruction.Idiv idiv:
                    {
                        if (idiv.Operand is Operand.Imm imm)
                        {
                            result.Add(new Instruction.Mov(idiv.Type, imm, srcReg));
                            result.Add(new Instruction.Idiv(idiv.Type, srcReg));
                        }
                        else
                            result.Add(inst);

                        break;
                    }

                case Instruction.Div div:
                    {
                        if (div.Operand is Operand.Imm imm)
                        {
                            result.Add(new Instruction.Mov(div.Type, imm, srcReg));
                            result.Add(new Instruction.Div(div.Type, srcReg));
                        }
                        else
                            result.Add(inst);

                        break;
                    }

                case Instruction.Binary binary:
                    {
                        if (binary.Type is AssemblyType.Double && binary.Dst is not Operand.Reg)
                        {
                            result.Add(new Instruction.Mov(new AssemblyType.Double(), binary.Dst, dstFloatReg));
                            result.Add(new Instruction.Binary(binary.Operator, new AssemblyType.Double(), binary.Src, dstFloatReg));
                            result.Add(new Instruction.Mov(new AssemblyType.Double(), dstFloatReg, binary.Dst));
                        }
                        else if (binary.Operator == Instruction.BinaryOperator.Add || binary.Operator == Instruction.BinaryOperator.Sub ||
                            binary.Operator == Instruction.BinaryOperator.And || binary.Operator == Instruction.BinaryOperator.Or ||
                            binary.Operator == Instruction.BinaryOperator.Xor || binary.Operator == Instruction.BinaryOperator.Shl ||
                            binary.Operator == Instruction.BinaryOperator.ShrTwoOp)
                        {
                            if (IsMemory(binary.Src) && IsMemory(binary.Dst))
                            {
                                result.Add(new Instruction.Mov(binary.Type, binary.Src, srcReg));
                                result.Add(new Instruction.Binary(binary.Operator, binary.Type, srcReg, binary.Dst));
                            }
                            else if (binary.Src is Operand.Imm immSrc && IsLargeInt(immSrc))
                            {
                                result.Add(new Instruction.Mov(new AssemblyType.Quadword(), binary.Src, srcReg));
                                result.Add(new Instruction.Binary(binary.Operator, new AssemblyType.Quadword(), srcReg, binary.Dst));
                            }
                            else
                                result.Add(inst);
                        }
                        else if (binary.Operator == Instruction.BinaryOperator.Mult)
                        {
                            if (binary.Src is Operand.Imm immSrc && IsMemory(binary.Dst) && IsLargeInt(immSrc))
                            {
                                result.Add(new Instruction.Mov(new AssemblyType.Quadword(), binary.Src, srcReg));
                                result.Add(new Instruction.Mov(new AssemblyType.Quadword(), binary.Dst, dstReg));
                                result.Add(new Instruction.Binary(binary.Operator, new AssemblyType.Quadword(), srcReg, dstReg));
                                result.Add(new Instruction.Mov(new AssemblyType.Quadword(), dstReg, binary.Dst));
                            }
                            else if (binary.Src is Operand.Imm immSrc2 && IsLargeInt(immSrc2))
                            {
                                result.Add(new Instruction.Mov(new AssemblyType.Quadword(), binary.Src, srcReg));
                                result.Add(new Instruction.Binary(binary.Operator, new AssemblyType.Quadword(), srcReg, binary.Dst));
                            }
                            else if (IsMemory(binary.Dst))
                            {
                                result.Add(new Instruction.Mov(binary.Type, binary.Dst, dstReg));
                                result.Add(new Instruction.Binary(binary.Operator, binary.Type, binary.Src, dstReg));
                                result.Add(new Instruction.Mov(binary.Type, dstReg, binary.Dst));
                            }
                            else
                                result.Add(inst);
                        }
                        else
                            result.Add(inst);

                        break;
                    }

                case Instruction.Movsx movsx:
                    {
                        if (movsx.Src is Operand.Imm && IsMemory(movsx.Dst))
                        {
                            result.Add(new Instruction.Mov(movsx.SrcType, movsx.Src, srcReg));
                            result.Add(new Instruction.Movsx(movsx.SrcType, movsx.DstType, srcReg, dstReg));
                            result.Add(new Instruction.Mov(movsx.DstType, dstReg, movsx.Dst));
                        }
                        else if (movsx.Src is Operand.Imm)
                        {
                            result.Add(new Instruction.Mov(movsx.SrcType, movsx.Src, srcReg));
                            result.Add(new Instruction.Movsx(movsx.SrcType, movsx.DstType, srcReg, movsx.Dst));
                        }
                        else if (IsMemory(movsx.Dst))
                        {
                            result.Add(new Instruction.Movsx(movsx.SrcType, movsx.DstType, movsx.Src, dstReg));
                            result.Add(new Instruction.Mov(movsx.DstType, dstReg, movsx.Dst));
                        }
                        else
                            result.Add(inst);

                        break;
                    }

                case Instruction.Push push:
                    {
                        if (push.Operand is Operand.Imm immA && IsLargeInt(immA))
                        {
                            result.Add(new Instruction.Mov(new AssemblyType.Quadword(), push.Operand, srcReg));
                            result.Add(new Instruction.Push(srcReg));
                        }
                        else if (push.Operand is Operand.Reg reg && reg.Register >= Operand.RegisterName.XMM0)
                        {
                            result.Add(new Instruction.Binary(Instruction.BinaryOperator.Sub, new AssemblyType.Quadword(), new Operand.Imm(8), new Operand.Reg(Operand.RegisterName.SP)));
                            result.Add(new Instruction.Mov(new AssemblyType.Double(), reg, new Operand.Memory(Operand.RegisterName.SP, 0)));
                        }
                        else
                            result.Add(inst);

                        break;
                    }

                case Instruction.MovZeroExtend movzx:
                    {
                        if (movzx.Src is Operand.Imm imm && movzx.SrcType is AssemblyType.Byte)
                        {
                            if (IsMemory(movzx.Dst))
                            {
                                result.Add(new Instruction.Mov(new AssemblyType.Byte(), imm, srcReg));
                                result.Add(new Instruction.MovZeroExtend(new AssemblyType.Byte(), movzx.DstType, srcReg, dstReg));
                                result.Add(new Instruction.Mov(movzx.DstType, dstReg, movzx.Dst));
                            }
                            else
                            {
                                result.Add(new Instruction.Mov(new AssemblyType.Byte(), imm, srcReg));
                                result.Add(new Instruction.MovZeroExtend(new AssemblyType.Byte(), movzx.DstType, srcReg, movzx.Dst));
                            }
                        }
                        else if (IsMemory(movzx.Dst) && movzx.SrcType is AssemblyType.Byte)
                        {
                            result.Add(new Instruction.MovZeroExtend(new AssemblyType.Byte(), movzx.DstType, movzx.Src, dstReg));
                            result.Add(new Instruction.Mov(movzx.DstType, dstReg, movzx.Dst));
                        }
                        else if (movzx.Dst is Operand.Reg reg && movzx.SrcType is AssemblyType.Longword)
                        {
                            result.Add(new Instruction.Mov(new AssemblyType.Longword(), movzx.Src, movzx.Dst));
                        }
                        else if (IsMemory(movzx.Dst) && movzx.SrcType is AssemblyType.Longword)
                        {
                            result.Add(new Instruction.Mov(new AssemblyType.Longword(), movzx.Src, dstReg));
                            result.Add(new Instruction.Mov(movzx.DstType, dstReg, movzx.Dst));
                        }
                        else
                            result.Add(inst);

                        break;
                    }

                case Instruction.Cvttsd2si cvttsd2si:
                    {
                        if (cvttsd2si.Dst is not Operand.Reg reg)
                        {
                            result.Add(new Instruction.Cvttsd2si(cvttsd2si.DstType, cvttsd2si.Src, dstReg));
                            result.Add(new Instruction.Mov(cvttsd2si.DstType, dstReg, cvttsd2si.Dst));
                        }
                        else
                            result.Add(inst);
                        break;
                    }
                case Instruction.Cvtsi2sd cvtsi2sd:
                    {
                        if (cvtsi2sd.Src is Operand.Imm && cvtsi2sd.Dst is not Operand.Reg)
                        {
                            result.Add(new Instruction.Mov(cvtsi2sd.SrcType, cvtsi2sd.Src, srcReg));
                            result.Add(new Instruction.Cvtsi2sd(cvtsi2sd.SrcType, srcReg, dstFloatReg));
                            result.Add(new Instruction.Mov(new AssemblyType.Double(), dstFloatReg, cvtsi2sd.Dst));
                        }
                        else if (cvtsi2sd.Src is Operand.Imm)
                        {
                            result.Add(new Instruction.Mov(cvtsi2sd.SrcType, cvtsi2sd.Src, srcReg));
                            result.Add(new Instruction.Cvtsi2sd(cvtsi2sd.SrcType, srcReg, cvtsi2sd.Dst));
                        }
                        else if (cvtsi2sd.Dst is not Operand.Reg)
                        {
                            result.Add(new Instruction.Cvtsi2sd(cvtsi2sd.SrcType, cvtsi2sd.Src, dstFloatReg));
                            result.Add(new Instruction.Mov(new AssemblyType.Double(), dstFloatReg, cvtsi2sd.Dst));
                        }
                        else
                            result.Add(inst);
                        break;
                    }
                case Instruction.Lea lea:
                    {
                        if (lea.Dst is not Operand.Reg)
                        {
                            result.Add(new Instruction.Lea(lea.Src, dstReg));
                            result.Add(new Instruction.Mov(new AssemblyType.Quadword(), dstReg, lea.Dst));
                        }
                        else
                            result.Add(inst);
                        break;
                    }

                case Instruction.Ret ret:
                    {
                        for (int j = calleeSavedRegs.Count - 1; j >= 0; j--)
                        {
                            result.Add(new Instruction.Pop(calleeSavedRegs[j]));
                        }

                        result.Add(inst);
                        break;
                    }

                default:
                    result.Add(inst);
                    break;
            }
        }

        return result;
    }

    private long CalculateStackAdjustment(long bytesForLocals, long calleeSavedCount)
    {
        var calleeSavedBytes = 8 * calleeSavedCount;
        var totalStackBytes = calleeSavedBytes + bytesForLocals;
        var adjustedStackBytes = AssemblyGenerator.AlignTo(totalStackBytes, 16);
        var stackAdjustment = adjustedStackBytes - calleeSavedBytes;
        return stackAdjustment;
    }

    private bool IsMemory(Operand operand)
    {
        return operand is Operand.Memory or Operand.Data or Operand.Indexed;
    }

    private bool IsLargeInt(Operand.Imm imm)
    {
        return imm.Value > int.MaxValue || (long)imm.Value < int.MinValue;
    }

    private bool IsLargeByte(Operand.Imm imm)
    {
        return imm.Value > byte.MaxValue || (long)imm.Value < -128L;
    }
}