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
                    Mov second = new Mov(reg, mov.dst);
                    mov.dst = reg;
                    instructions.Insert(i + 1, second);
                }
            }
        }
    }
}