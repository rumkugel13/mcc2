using mcc2.Assembly;

namespace mcc2;

public class InstructionFixer
{
    public void Fix(List<Instruction> instructions)
    {
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
                        if (mov.Src is Operand.Memory or Operand.Data && mov.Dst is Operand.Memory or Operand.Data)
                        {
                            var regUsed = srcReg;
                            if (mov.Type is Instruction.AssemblyType.Double)
                                regUsed = srcFloatReg;
                            Instruction.Mov moveBefore = new Instruction.Mov(mov.Type, mov.Src, regUsed);
                            instructions[i] = new Instruction.Mov(mov.Type, regUsed, mov.Dst);
                            instructions.Insert(i, moveBefore);
                        }
                        else if (mov.Type == Instruction.AssemblyType.Quadword && mov.Src is Operand.Imm immSrc && mov.Dst is Operand.Memory or Operand.Data &&
                            (immSrc.Value > int.MaxValue || (long)immSrc.Value < int.MinValue))
                        {
                            Instruction.Mov moveBefore = new Instruction.Mov(Instruction.AssemblyType.Quadword, mov.Src, srcReg);
                            instructions[i] = new Instruction.Mov(Instruction.AssemblyType.Quadword, srcReg, mov.Dst);
                            instructions.Insert(i, moveBefore);
                        }
                        else if (mov.Type == Instruction.AssemblyType.Longword && mov.Src is Operand.Imm immSrc2 &&
                            (immSrc2.Value > uint.MaxValue || (long)immSrc2.Value < int.MinValue))
                        {
                            instructions[i] = new Instruction.Mov(Instruction.AssemblyType.Longword, new Operand.Imm((uint)immSrc2.Value), mov.Dst);
                        }

                        break;
                    }

                case Instruction.Cmp cmp:
                    {
                        if (cmp.Type is Instruction.AssemblyType.Double && cmp.OperandB is not Operand.Reg)
                        {
                            Instruction.Mov moveDst = new Instruction.Mov(Instruction.AssemblyType.Double, cmp.OperandB, dstFloatReg);
                            instructions[i] = new Instruction.Cmp(Instruction.AssemblyType.Double, cmp.OperandA, dstFloatReg);
                            instructions.Insert(i, moveDst);
                        }
                        else if (cmp.OperandA is Operand.Memory or Operand.Data && cmp.OperandB is Operand.Memory or Operand.Data)
                        {
                            Instruction.Mov moveBefore = new Instruction.Mov(cmp.Type, cmp.OperandA, srcReg);
                            instructions[i] = new Instruction.Cmp(cmp.Type, srcReg, cmp.OperandB);
                            instructions.Insert(i, moveBefore);
                        }
                        else if (cmp.Type == Instruction.AssemblyType.Quadword && cmp.OperandA is Operand.Imm immA && cmp.OperandB is not Operand.Imm &&
                            (immA.Value > int.MaxValue || (long)immA.Value < int.MinValue))
                        {
                            Instruction.Mov moveBefore = new Instruction.Mov(Instruction.AssemblyType.Quadword, cmp.OperandA, srcReg);
                            instructions[i] = new Instruction.Cmp(Instruction.AssemblyType.Quadword, srcReg, cmp.OperandB);
                            instructions.Insert(i, moveBefore);
                        }
                        else if (cmp.Type == Instruction.AssemblyType.Quadword && cmp.OperandA is Operand.Imm immA2 && cmp.OperandB is Operand.Imm &&
                            (immA2.Value > int.MaxValue || (long)immA2.Value < int.MinValue))
                        {
                            Instruction.Mov moveSrc = new Instruction.Mov(Instruction.AssemblyType.Quadword, cmp.OperandA, srcReg);
                            Instruction.Mov moveDst = new Instruction.Mov(Instruction.AssemblyType.Quadword, cmp.OperandB, dstReg);
                            instructions[i] = new Instruction.Cmp(Instruction.AssemblyType.Quadword, srcReg, dstReg);
                            instructions.Insert(i, moveDst);
                            instructions.Insert(i, moveSrc);
                        }
                        else if (cmp.OperandB is Operand.Imm imm)
                        {
                            Instruction.Mov moveBefore = new Instruction.Mov(cmp.Type, cmp.OperandB, dstReg);
                            instructions[i] = new Instruction.Cmp(cmp.Type, cmp.OperandA, dstReg);
                            instructions.Insert(i, moveBefore);
                        }

                        break;
                    }

                case Instruction.Idiv idiv:
                    {
                        if (idiv.Operand is Operand.Imm imm)
                        {
                            Instruction.Mov moveBefore = new Instruction.Mov(idiv.Type, imm, srcReg);
                            instructions[i] = new Instruction.Idiv(idiv.Type, srcReg);
                            instructions.Insert(i, moveBefore);
                        }

                        break;
                    }

                case Instruction.Div div:
                    {
                        if (div.Operand is Operand.Imm imm)
                        {
                            Instruction.Mov moveBefore = new Instruction.Mov(div.Type, imm, srcReg);
                            instructions[i] = new Instruction.Div(div.Type, srcReg);
                            instructions.Insert(i, moveBefore);
                        }

                        break;
                    }

                case Instruction.Binary binary:
                    {
                        if (binary.Type is Instruction.AssemblyType.Double && binary.DstOperand is not Operand.Reg)
                        {
                            Instruction.Mov moveDst = new Instruction.Mov(Instruction.AssemblyType.Double, binary.DstOperand, dstFloatReg);
                            instructions[i] = new Instruction.Binary(binary.Operator, Instruction.AssemblyType.Double, binary.SrcOperand, dstFloatReg);
                            Instruction.Mov moveAfter = new Instruction.Mov(Instruction.AssemblyType.Double, dstFloatReg, binary.DstOperand);
                            instructions.Insert(i + 1, moveAfter);
                            instructions.Insert(i, moveDst);
                        }
                        else if (binary.Operator == Instruction.BinaryOperator.Add || binary.Operator == Instruction.BinaryOperator.Sub ||
                            binary.Operator == Instruction.BinaryOperator.And || binary.Operator == Instruction.BinaryOperator.Or)
                        {
                            if (binary.SrcOperand is Operand.Memory or Operand.Data && binary.DstOperand is Operand.Memory or Operand.Data)
                            {
                                Instruction.Mov moveBefore = new Instruction.Mov(binary.Type, binary.SrcOperand, srcReg);
                                instructions[i] = new Instruction.Binary(binary.Operator, binary.Type, srcReg, binary.DstOperand);
                                instructions.Insert(i, moveBefore);
                            }
                            else if (binary.SrcOperand is Operand.Imm immSrc &&
                                (immSrc.Value > int.MaxValue || (long)immSrc.Value < int.MinValue))
                            {
                                Instruction.Mov moveBefore = new Instruction.Mov(Instruction.AssemblyType.Quadword, binary.SrcOperand, srcReg);
                                instructions[i] = new Instruction.Binary(binary.Operator, Instruction.AssemblyType.Quadword, srcReg, binary.DstOperand);
                                instructions.Insert(i, moveBefore);
                            }
                        }
                        else if (binary.Operator == Instruction.BinaryOperator.Mult)
                        {
                            if (binary.SrcOperand is Operand.Imm immSrc && binary.DstOperand is Operand.Memory or Operand.Data &&
                                (immSrc.Value > int.MaxValue || (long)immSrc.Value < int.MinValue))
                            {
                                Instruction.Mov moveSrc = new Instruction.Mov(Instruction.AssemblyType.Quadword, binary.SrcOperand, srcReg);
                                Instruction.Mov moveDst = new Instruction.Mov(Instruction.AssemblyType.Quadword, binary.DstOperand, dstReg);
                                instructions[i] = new Instruction.Binary(binary.Operator, Instruction.AssemblyType.Quadword, srcReg, dstReg);
                                Instruction.Mov moveAfter = new Instruction.Mov(Instruction.AssemblyType.Quadword, dstReg, binary.DstOperand);
                                instructions.Insert(i + 1, moveAfter);
                                instructions.Insert(i, moveDst);
                                instructions.Insert(i, moveSrc);
                            }
                            else if (binary.SrcOperand is Operand.Imm immSrc2 &&
                                (immSrc2.Value > int.MaxValue || (long)immSrc2.Value < int.MinValue))
                            {
                                Instruction.Mov moveBefore = new Instruction.Mov(Instruction.AssemblyType.Quadword, binary.SrcOperand, srcReg);
                                instructions[i] = new Instruction.Binary(binary.Operator, Instruction.AssemblyType.Quadword, srcReg, binary.DstOperand);
                                instructions.Insert(i, moveBefore);
                            }
                            else if (binary.DstOperand is Operand.Memory or Operand.Data)
                            {
                                Instruction.Mov moveBefore = new Instruction.Mov(binary.Type, binary.DstOperand, dstReg);
                                instructions[i] = new Instruction.Binary(binary.Operator, binary.Type, binary.SrcOperand, dstReg);
                                Instruction.Mov moveAfter = new Instruction.Mov(binary.Type, dstReg, binary.DstOperand);
                                instructions.Insert(i + 1, moveAfter);
                                instructions.Insert(i, moveBefore);
                            }
                        }

                        break;
                    }

                case Instruction.Movsx movsx:
                    {
                        if (movsx.Src is Operand.Imm && movsx.Dst is Operand.Memory or Operand.Data)
                        {
                            Instruction.Mov moveBefore = new Instruction.Mov(Instruction.AssemblyType.Longword, movsx.Src, srcReg);
                            instructions[i] = new Instruction.Movsx(srcReg, dstReg);
                            Instruction.Mov moveAfter = new Instruction.Mov(Instruction.AssemblyType.Quadword, dstReg, movsx.Dst);
                            instructions.Insert(i + 1, moveAfter);
                            instructions.Insert(i, moveBefore);
                        }
                        else if (movsx.Src is Operand.Imm)
                        {
                            Instruction.Mov moveBefore = new Instruction.Mov(Instruction.AssemblyType.Longword, movsx.Src, srcReg);
                            instructions[i] = new Instruction.Movsx(srcReg, movsx.Dst);
                            instructions.Insert(i, moveBefore);
                        }
                        else if (movsx.Dst is Operand.Memory or Operand.Data)
                        {
                            Instruction.Mov moveAfter = new Instruction.Mov(Instruction.AssemblyType.Quadword, dstReg, movsx.Dst);
                            instructions[i] = new Instruction.Movsx(movsx.Src, dstReg);
                            instructions.Insert(i + 1, moveAfter);
                        }

                        break;
                    }

                case Instruction.Push push:
                    {
                        if (push.Operand is Operand.Imm immA && (immA.Value > int.MaxValue || (long)immA.Value < int.MinValue))
                        {
                            Instruction.Mov moveBefore = new Instruction.Mov(Instruction.AssemblyType.Quadword, push.Operand, srcReg);
                            instructions[i] = new Instruction.Push(srcReg);
                            instructions.Insert(i, moveBefore);
                        }
                        else if (push.Operand is Operand.Reg reg && reg.Register >= Operand.RegisterName.XMM0)
                        {
                            var subStackPointer = new Instruction.Binary(Instruction.BinaryOperator.Sub, Instruction.AssemblyType.Quadword, new Operand.Imm(8), new Operand.Reg(Operand.RegisterName.SP));
                            instructions[i] = new Instruction.Mov(Instruction.AssemblyType.Double, reg, new Operand.Memory(Operand.RegisterName.SP, 0));
                            instructions.Insert(i, subStackPointer);
                        }

                        break;
                    }

                case Instruction.MovZeroExtend movzx:
                    {
                        if (movzx.Dst is Operand.Reg reg)
                        {
                            instructions[i] = new Instruction.Mov(Instruction.AssemblyType.Longword, movzx.Src, movzx.Dst);
                        }
                        else if (movzx.Dst is Operand.Memory or Operand.Data)
                        {
                            Instruction.Mov moveBefore = new Instruction.Mov(Instruction.AssemblyType.Longword, movzx.Src, dstReg);
                            instructions[i] = new Instruction.Mov(Instruction.AssemblyType.Quadword, dstReg, movzx.Dst);
                            instructions.Insert(i, moveBefore);
                        }

                        break;
                    }

                case Instruction.Cvttsd2si cvttsd2si:
                    {
                        if (cvttsd2si.Dst is not Operand.Reg reg)
                        {
                            Instruction.Mov moveAfter = new Instruction.Mov(cvttsd2si.DstType, dstReg, cvttsd2si.Dst);
                            instructions[i] = new Instruction.Cvttsd2si(cvttsd2si.DstType, cvttsd2si.Src, dstReg);
                            instructions.Insert(i + 1, moveAfter);
                        }
                        break;
                    }
                case Instruction.Cvtsi2sd cvtsi2sd:
                    {
                        if (cvtsi2sd.Src is Operand.Imm && cvtsi2sd.Dst is not Operand.Reg)
                        {
                            Instruction.Mov moveBefore = new Instruction.Mov(cvtsi2sd.SrcType, cvtsi2sd.Src, srcReg);
                            instructions[i] = new Instruction.Cvtsi2sd(cvtsi2sd.SrcType, srcReg, dstFloatReg);
                            Instruction.Mov moveAfter = new Instruction.Mov(Instruction.AssemblyType.Double, dstFloatReg, cvtsi2sd.Dst);
                            instructions.Insert(i + 1, moveAfter);
                            instructions.Insert(i, moveBefore);
                        }
                        else if (cvtsi2sd.Src is Operand.Imm)
                        {
                            Instruction.Mov moveBefore = new Instruction.Mov(cvtsi2sd.SrcType, cvtsi2sd.Src, srcReg);
                            instructions[i] = new Instruction.Cvtsi2sd(cvtsi2sd.SrcType, srcReg, cvtsi2sd.Dst);
                            instructions.Insert(i, moveBefore);
                        }
                        else if (cvtsi2sd.Dst is not Operand.Reg)
                        {
                            Instruction.Mov moveAfter = new Instruction.Mov(Instruction.AssemblyType.Double, dstFloatReg, cvtsi2sd.Dst);
                            instructions[i] = new Instruction.Cvtsi2sd(cvtsi2sd.SrcType, cvtsi2sd.Src, dstFloatReg);
                            instructions.Insert(i + 1, moveAfter);
                        }
                        break;
                    }
                case Instruction.Lea lea:
                {
                    if (lea.Dst is not Operand.Reg)
                    {
                        Instruction.Mov moveAfter = new Instruction.Mov(Instruction.AssemblyType.Quadword, dstReg, lea.Dst);
                        instructions[i] = new Instruction.Lea(lea.Src, dstReg);
                        instructions.Insert(i + 1, moveAfter);
                    }
                    break;
                }
            }
        }
    }
}