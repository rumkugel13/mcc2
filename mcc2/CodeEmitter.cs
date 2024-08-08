using System.Text;
using mcc2.Assembly;

namespace mcc2;

public class CodeEmitter
{
    public StringBuilder Emit(AssemblyProgram program)
    {
        StringBuilder builder = new();
        EmitProgram(program, builder);
        if (OperatingSystem.IsLinux())
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
        builder.AppendLine($"\tpushq %rbp");
        builder.AppendLine($"\tmovq %rsp, %rbp");
        foreach (var inst in function.Instructions)
        {
            EmitInstruction(inst, builder);
        }
    }

    private void EmitInstruction(Instruction instruction, StringBuilder builder)
    {
        switch (instruction)
        {
            case Mov mov:
                builder.AppendLine($"\tmovl {EmitOperand(mov.src)}, {EmitOperand(mov.dst)}");
                break;
            case Ret:
                builder.AppendLine($"\tmovq %rbp, %rsp");
                builder.AppendLine($"\tpopq %rbp");
                builder.AppendLine("\tret");
                break;
            case Unary unary:
                builder.AppendLine($"\t{EmitUnaryOperator(unary.Operator)} {EmitOperand(unary.Operand)}");
                break;
            case Binary binary:
                builder.AppendLine($"\t{EmitBinaryOperator(binary.Operator)} {EmitOperand(binary.SrcOperand)}, {EmitOperand(binary.DstOperand)}");
                break;
            case Idiv idiv:
                builder.AppendLine($"\tidivl {EmitOperand(idiv.Operand)}");
                break;
            case Cdq:
                builder.AppendLine("\tcdq");
                break;
            case AllocateStack allocateStack:
                builder.AppendLine($"\tsubq ${allocateStack.Bytes}, %rsp");
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private string EmitBinaryOperator(Binary.BinaryOperator binaryOperator)
    {
        return binaryOperator switch {
            Binary.BinaryOperator.Add => "addl",
            Binary.BinaryOperator.Sub => "subl",
            Binary.BinaryOperator.Mult => "imull",
            _ => throw new NotImplementedException()
        };
    }

    private string EmitUnaryOperator(Unary.UnaryOperator unaryOperator)
    {
        return unaryOperator switch {
            Unary.UnaryOperator.Neg => "negl",
            Unary.UnaryOperator.Not => "notl",
            _ => throw new NotImplementedException()
        };
    }

    private string EmitOperand(Operand operand)
    {
        return operand switch {
            Reg reg => reg.Register switch {
                Reg.RegisterName.AX => "%eax",
                Reg.RegisterName.DX => "%edx",
                Reg.RegisterName.R10 => "%r10d",
                Reg.RegisterName.R11 => "%r11d",
                _ => throw new NotImplementedException()
            },
            Imm imm => $"${imm.Value}",
            Stack stack => $"{stack.Offset}(%rbp)",
            _ => throw new NotImplementedException()
        };
    }
}