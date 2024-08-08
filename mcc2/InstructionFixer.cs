using mcc2.Assembly;

namespace mcc2;

public class InstructionFixer
{
    public void Fix(List<Instruction> instructions)
    {
        for (int i = 0; i < instructions.Count; i++)
        {
            var inst = instructions[i];
            if (inst is Mov mov)
            {
                if (mov.src is Stack && mov.dst is Stack)
                {
                    Reg reg = new Reg(Reg.RegisterName.R10);
                    Mov moveBefore = new Mov(mov.src, reg);
                    instructions.Insert(i, moveBefore);
                    mov.src = reg;
                }
            }
            else if (inst is Cmp cmp)
            {
                if (cmp.OperandA is Stack && cmp.OperandB is Stack)
                {
                    Reg reg = new Reg(Reg.RegisterName.R10);
                    Mov moveBefore = new Mov(cmp.OperandA, reg);
                    instructions.Insert(i, moveBefore);
                    cmp.OperandA = reg;
                }
                else if (cmp.OperandB is Imm imm)
                {
                    Reg reg = new Reg(Reg.RegisterName.R11);
                    Mov moveBefore = new Mov(cmp.OperandB, reg);
                    instructions.Insert(i, moveBefore);
                    cmp.OperandB = reg;
                }
            }
            else if (inst is Idiv idiv)
            {
                if (idiv.Operand is Imm imm)
                {
                    Reg reg = new Reg(Reg.RegisterName.R10);
                    Mov moveBefore = new Mov(imm, reg);
                    instructions.Insert(i, moveBefore);
                    idiv.Operand = reg;
                }
            }
            else if (inst is Binary binary)
            {
                if (binary.Operator == Binary.BinaryOperator.Add && binary.SrcOperand is Stack && binary.DstOperand is Stack)
                {
                    Reg reg = new Reg(Reg.RegisterName.R10);
                    Mov moveBefore = new Mov(binary.SrcOperand, reg);
                    instructions.Insert(i, moveBefore);
                    binary.SrcOperand = reg;
                }
                else if (binary.Operator == Binary.BinaryOperator.Sub && binary.SrcOperand is Stack && binary.DstOperand is Stack)
                {
                    Reg reg = new Reg(Reg.RegisterName.R10);
                    Mov moveBefore = new Mov(binary.SrcOperand, reg);
                    instructions.Insert(i, moveBefore);
                    binary.SrcOperand = reg;
                }
                else if (binary.Operator == Binary.BinaryOperator.Mult && binary.DstOperand is Stack)
                {
                    Reg reg = new Reg(Reg.RegisterName.R11);
                    Mov moveBefore = new Mov(binary.DstOperand, reg);
                    Mov moveAfter = new Mov(reg, binary.DstOperand);
                    binary.DstOperand = reg;
                    instructions.Insert(i + 1, moveAfter);
                    instructions.Insert(i, moveBefore);
                }
            }
        }
    }
}