using mcc2.Assembly;

namespace mcc2;

public class InstructionFixer
{
    public void Fix(List<Instruction> instructions)
    {
        for (int i = 0; i < instructions.Count; i++)
        {
            var inst = instructions[i];
            if (inst is Instruction.Mov mov)
            {
                if (mov.Src is Operand.Stack or Operand.Data && mov.Dst is Operand.Stack or Operand.Data)
                {
                    Operand.Reg reg = new Operand.Reg(Operand.RegisterName.R10);
                    Instruction.Mov moveBefore = new Instruction.Mov(mov.Src, reg);
                    // mov.Src = reg;
                    instructions[i] = new Instruction.Mov(reg, mov.Dst);
                    instructions.Insert(i, moveBefore);
                }
            }
            else if (inst is Instruction.Cmp cmp)
            {
                if (cmp.OperandA is Operand.Stack or Operand.Data && cmp.OperandB is Operand.Stack or Operand.Data)
                {
                    Operand.Reg reg = new Operand.Reg(Operand.RegisterName.R10);
                    Instruction.Mov moveBefore = new Instruction.Mov(cmp.OperandA, reg);
                    // cmp.OperandA = reg;
                    instructions[i] = new Instruction.Cmp(reg, cmp.OperandB);
                    instructions.Insert(i, moveBefore);
                }
                else if (cmp.OperandB is Operand.Imm imm)
                {
                    Operand.Reg reg = new Operand.Reg(Operand.RegisterName.R11);
                    Instruction.Mov moveBefore = new Instruction.Mov(cmp.OperandB, reg);
                    // cmp.OperandB = reg;
                    instructions[i] = new Instruction.Cmp(cmp.OperandA, reg);
                    instructions.Insert(i, moveBefore);
                }
            }
            else if (inst is Instruction.Idiv idiv)
            {
                if (idiv.Operand is Operand.Imm imm)
                {
                    Operand.Reg reg = new Operand.Reg(Operand.RegisterName.R10);
                    Instruction.Mov moveBefore = new Instruction.Mov(imm, reg);
                    // idiv.Operand = reg;
                    instructions[i] = new Instruction.Idiv(reg);
                    instructions.Insert(i, moveBefore);
                }
            }
            else if (inst is Instruction.Binary binary)
            {
                if (binary.Operator == Instruction.BinaryOperator.Add && binary.SrcOperand is Operand.Stack or Operand.Data && binary.DstOperand is Operand.Stack or Operand.Data)
                {
                    Operand.Reg reg = new Operand.Reg(Operand.RegisterName.R10);
                    Instruction.Mov moveBefore = new Instruction.Mov(binary.SrcOperand, reg);
                    // binary.SrcOperand = reg;
                    instructions[i] = new Instruction.Binary(binary.Operator, reg, binary.DstOperand);
                    instructions.Insert(i, moveBefore);
                }
                else if (binary.Operator == Instruction.BinaryOperator.Sub && binary.SrcOperand is Operand.Stack or Operand.Data && binary.DstOperand is Operand.Stack or Operand.Data)
                {
                    Operand.Reg reg = new Operand.Reg(Operand.RegisterName.R10);
                    Instruction.Mov moveBefore = new Instruction.Mov(binary.SrcOperand, reg);
                    // binary.SrcOperand = reg;
                    instructions[i] = new Instruction.Binary(binary.Operator, reg, binary.DstOperand);
                    instructions.Insert(i, moveBefore);
                }
                else if (binary.Operator == Instruction.BinaryOperator.Mult && binary.DstOperand is Operand.Stack or Operand.Data)
                {
                    Operand.Reg reg = new Operand.Reg(Operand.RegisterName.R11);
                    Instruction.Mov moveBefore = new Instruction.Mov(binary.DstOperand, reg);
                    Instruction.Mov moveAfter = new Instruction.Mov(reg, binary.DstOperand);
                    // binary.DstOperand = reg;
                    instructions[i] = new Instruction.Binary(binary.Operator, binary.SrcOperand, reg);
                    instructions.Insert(i + 1, moveAfter);
                    instructions.Insert(i, moveBefore);
                }
            }
        }
    }
}