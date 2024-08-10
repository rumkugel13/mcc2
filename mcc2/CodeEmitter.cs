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
        foreach (var fun in program.Functions)
            EmitFunction(fun, builder);
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
            case Cmp cmp:
                builder.AppendLine($"\tcmpl {EmitOperand(cmp.OperandA)}, {EmitOperand(cmp.OperandB)}");
                break;
            case Jmp jmp:
                builder.AppendLine($"\tjmp .L{jmp.Identifier}");
                break;
            case JmpCC jmpCC:
                builder.AppendLine($"\tj{EmitConditionCode(jmpCC.Condition)} .L{jmpCC.Identifier}");
                break;
            case SetCC setCC:
                builder.AppendLine($"\tset{EmitConditionCode(setCC.Condition)} {EmitOperand(setCC.Operand, 1)}");
                break;
            case Label label:
                builder.AppendLine($".L{label.Identifier}:");
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private string EmitConditionCode(JmpCC.ConditionCode condition)
    {
        return condition switch
        {
            JmpCC.ConditionCode.E => "e",
            JmpCC.ConditionCode.NE => "ne",
            JmpCC.ConditionCode.G => "g",
            JmpCC.ConditionCode.GE => "ge",
            JmpCC.ConditionCode.L => "l",
            JmpCC.ConditionCode.LE => "le",
            _ => throw new NotImplementedException()
        };
    }

    private string EmitBinaryOperator(Binary.BinaryOperator binaryOperator)
    {
        return binaryOperator switch
        {
            Binary.BinaryOperator.Add => "addl",
            Binary.BinaryOperator.Sub => "subl",
            Binary.BinaryOperator.Mult => "imull",
            _ => throw new NotImplementedException()
        };
    }

    private string EmitUnaryOperator(Unary.UnaryOperator unaryOperator)
    {
        return unaryOperator switch
        {
            Unary.UnaryOperator.Neg => "negl",
            Unary.UnaryOperator.Not => "notl",
            _ => throw new NotImplementedException()
        };
    }

    private string EmitOperand(Operand operand, int bytes = 4)
    {
        return operand switch
        {
            Reg reg => reg.Register switch
            {
                Reg.RegisterName.AX => bytes == 4 ? "%eax" : "%al",
                Reg.RegisterName.DX => bytes == 4 ? "%edx" : "%dl",
                Reg.RegisterName.R10 => bytes == 4 ? "%r10d" : "%r10b",
                Reg.RegisterName.R11 => bytes == 4 ? "%r11d" : "%r11b",
                _ => throw new NotImplementedException()
            },
            Imm imm => $"${imm.Value}",
            Stack stack => $"{stack.Offset}(%rbp)",
            _ => throw new NotImplementedException()
        };
    }
}