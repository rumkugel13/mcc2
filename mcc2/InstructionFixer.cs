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

            var inst = instructions[i];
            if (inst is Instruction.Mov mov)
            {
                if (mov.Src is Operand.Stack or Operand.Data && mov.Dst is Operand.Stack or Operand.Data)
                {
                    Instruction.Mov moveBefore = new Instruction.Mov(mov.Type, mov.Src, srcReg);
                    instructions[i] = new Instruction.Mov(mov.Type, srcReg, mov.Dst);
                    instructions.Insert(i, moveBefore);
                }
                else if (mov.Type == Instruction.AssemblyType.Quadword && mov.Src is Operand.Imm immSrc && mov.Dst is Operand.Stack or Operand.Data && 
                    (immSrc.Value > int.MaxValue || immSrc.Value < int.MinValue))
                {
                    Instruction.Mov moveBefore = new Instruction.Mov(Instruction.AssemblyType.Quadword, mov.Src, srcReg);
                    instructions[i] = new Instruction.Mov(Instruction.AssemblyType.Quadword, srcReg, mov.Dst);
                    instructions.Insert(i, moveBefore);
                }
                else if (mov.Type == Instruction.AssemblyType.Longword && mov.Src is Operand.Imm immSrc2 && 
                    (immSrc2.Value > uint.MaxValue || immSrc2.Value < int.MinValue))
                {
                    instructions[i] = new Instruction.Mov(Instruction.AssemblyType.Longword, new Operand.Imm((int)immSrc2.Value), mov.Dst);
                }
            }
            else if (inst is Instruction.Cmp cmp)
            {
                if (cmp.OperandA is Operand.Stack or Operand.Data && cmp.OperandB is Operand.Stack or Operand.Data)
                {
                    Instruction.Mov moveBefore = new Instruction.Mov(cmp.Type, cmp.OperandA, srcReg);
                    instructions[i] = new Instruction.Cmp(cmp.Type, srcReg, cmp.OperandB);
                    instructions.Insert(i, moveBefore);
                }
                else if (cmp.Type == Instruction.AssemblyType.Quadword && cmp.OperandA is Operand.Imm immA && cmp.OperandB is not Operand.Imm &&
                    (immA.Value > int.MaxValue || immA.Value < int.MinValue))
                {
                    Instruction.Mov moveBefore = new Instruction.Mov(Instruction.AssemblyType.Quadword, cmp.OperandA, srcReg);
                    instructions[i] = new Instruction.Cmp(Instruction.AssemblyType.Quadword, srcReg, cmp.OperandB);
                    instructions.Insert(i, moveBefore);
                }
                else if (cmp.Type == Instruction.AssemblyType.Quadword && cmp.OperandA is Operand.Imm immA2 && cmp.OperandB is Operand.Imm &&
                    (immA2.Value > int.MaxValue || immA2.Value < int.MinValue))
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
            }
            else if (inst is Instruction.Idiv idiv)
            {
                if (idiv.Operand is Operand.Imm imm)
                {
                    Instruction.Mov moveBefore = new Instruction.Mov(idiv.Type, imm, srcReg);
                    instructions[i] = new Instruction.Idiv(idiv.Type, srcReg);
                    instructions.Insert(i, moveBefore);
                }
            }
            else if (inst is Instruction.Div div)
            {
                if (div.Operand is Operand.Imm imm)
                {
                    Instruction.Mov moveBefore = new Instruction.Mov(div.Type, imm, srcReg);
                    instructions[i] = new Instruction.Div(div.Type, srcReg);
                    instructions.Insert(i, moveBefore);
                }
            }
            else if (inst is Instruction.Binary binary)
            {
                if (binary.Operator == Instruction.BinaryOperator.Add || binary.Operator == Instruction.BinaryOperator.Sub)
                {
                    if (binary.SrcOperand is Operand.Stack or Operand.Data && binary.DstOperand is Operand.Stack or Operand.Data)
                    {
                        Instruction.Mov moveBefore = new Instruction.Mov(binary.Type, binary.SrcOperand, srcReg);
                        instructions[i] = new Instruction.Binary(binary.Operator, binary.Type, srcReg, binary.DstOperand);
                        instructions.Insert(i, moveBefore);
                    }
                    else if (binary.SrcOperand is Operand.Imm immSrc &&
                        (immSrc.Value > int.MaxValue || immSrc.Value < int.MinValue))
                    {
                        Instruction.Mov moveBefore = new Instruction.Mov(Instruction.AssemblyType.Quadword, binary.SrcOperand, srcReg);
                        instructions[i] = new Instruction.Binary(binary.Operator, Instruction.AssemblyType.Quadword, srcReg, binary.DstOperand);
                        instructions.Insert(i, moveBefore);
                    }
                }
                else if (binary.Operator == Instruction.BinaryOperator.Mult)
                {
                    if (binary.SrcOperand is Operand.Imm immSrc && binary.DstOperand is Operand.Stack or Operand.Data && 
                        (immSrc.Value > int.MaxValue || immSrc.Value < int.MinValue))
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
                        (immSrc2.Value > int.MaxValue || immSrc2.Value < int.MinValue))
                    {
                        Instruction.Mov moveBefore = new Instruction.Mov(Instruction.AssemblyType.Quadword, binary.SrcOperand, srcReg);
                        instructions[i] = new Instruction.Binary(binary.Operator, Instruction.AssemblyType.Quadword, srcReg, binary.DstOperand);
                        instructions.Insert(i, moveBefore);
                    }
                    else if (binary.DstOperand is Operand.Stack or Operand.Data)
                    {
                        Instruction.Mov moveBefore = new Instruction.Mov(binary.Type, binary.DstOperand, dstReg);
                        instructions[i] = new Instruction.Binary(binary.Operator, binary.Type, binary.SrcOperand, dstReg);
                        Instruction.Mov moveAfter = new Instruction.Mov(binary.Type, dstReg, binary.DstOperand);
                        instructions.Insert(i + 1, moveAfter);
                        instructions.Insert(i, moveBefore);
                    }
                }
            }
            else if (inst is Instruction.Movsx movsx)
            {
                if (movsx.Src is Operand.Imm && movsx.Dst is Operand.Stack or Operand.Data)
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
                else if (movsx.Dst is Operand.Stack or Operand.Data)
                {
                    Instruction.Mov moveAfter = new Instruction.Mov(Instruction.AssemblyType.Quadword, dstReg, movsx.Dst);
                    instructions[i] = new Instruction.Movsx(movsx.Src, dstReg);
                    instructions.Insert(i + 1, moveAfter);
                }
            }
            else if (inst is Instruction.Push push)
            {
                if (push.Operand is Operand.Imm immA && (immA.Value > int.MaxValue || immA.Value < int.MinValue))
                {
                    Instruction.Mov moveBefore = new Instruction.Mov(Instruction.AssemblyType.Quadword, push.Operand, srcReg);
                    instructions[i] = new Instruction.Push(srcReg);
                    instructions.Insert(i, moveBefore);
                }
            }
            else if (inst is Instruction.MovZeroExtend movzx)
            {
                if (movzx.Dst is Operand.Reg reg)
                {
                    instructions[i] = new Instruction.Mov(Instruction.AssemblyType.Longword, movzx.Src, movzx.Dst);
                }
                else if (movzx.Dst is Operand.Stack or Operand.Data)
                {
                    Instruction.Mov moveBefore = new Instruction.Mov(Instruction.AssemblyType.Longword, movzx.Src, dstReg);
                    instructions[i] = new Instruction.Mov(Instruction.AssemblyType.Quadword, dstReg, movzx.Dst);
                    instructions.Insert(i, moveBefore);
                }
            }
        }
    }
}