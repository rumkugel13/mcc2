using System.Text;
using mcc2.Assembly;

namespace mcc2;

public class CodeEmitter
{
    public StringBuilder Emit(AssemblyProgram program)
    {
        StringBuilder builder = new();
        EmitProgram(program, builder);
        builder.AppendLine("\t.section .note.GNU-stack,\"\",@progbits");
        return builder;
    }

    private void EmitProgram(AssemblyProgram program, StringBuilder builder)
    {
        EmitFunction(program.Function, builder);
    }

    private void EmitFunction(Function function, StringBuilder builder)
    {
        builder.AppendLine($"\t.globl {function.Name}");
        builder.AppendLine($"{function.Name}:");
        foreach (var inst in function.Instructions)
        {
            EmitInstruction(inst, builder);
        }
    }

    private void EmitInstruction(Instruction instruction, StringBuilder builder)
    {
        builder.AppendLine($"\t{instruction switch {
            Mov mov => $"movl {EmitOperand(mov.src)},{EmitOperand(mov.dst)}",
            Ret ret => "ret",
            _ => ""
        }
        }");
    }

    private string EmitOperand(Operand operand)
    {
        return operand switch {
            Reg reg => "%eax",
            Imm imm => $"${imm.Value}",
            _ => ""
        };
    }
}