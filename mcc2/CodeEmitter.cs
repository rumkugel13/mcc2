using System.Text;
using mcc2.Assembly;

namespace mcc2;

public class CodeEmitter
{
    private Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable;

    public CodeEmitter(Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable)
    {
        this.symbolTable = symbolTable;
    }

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
        foreach (var topLevel in program.TopLevel)
            if (topLevel is TopLevel.Function fun)
                EmitFunction(fun, builder);
            else if (topLevel is TopLevel.StaticVariable stat)
                EmitStaticVariable(stat, builder);
    }

    private void EmitFunction(TopLevel.Function function, StringBuilder builder)
    {
        if (function.Global)
            builder.AppendLine($"\t.globl {function.Name}");
            
        builder.AppendLine($"\t.text");
        builder.AppendLine($"{function.Name}:");
        builder.AppendLine($"\tpushq %rbp");
        builder.AppendLine($"\tmovq %rsp, %rbp");
        foreach (var inst in function.Instructions)
        {
            EmitInstruction(inst, builder);
        }
    }

    private void EmitStaticVariable(TopLevel.StaticVariable staticVariable, StringBuilder builder)
    {
        if (staticVariable.Global)
            builder.AppendLine($"\t.globl {staticVariable.Identifier}");

        if (staticVariable.Init == 0)
        {
            builder.AppendLine($"\t.bss");
            if (OperatingSystem.IsLinux())
                builder.AppendLine($"\t.align 4");
            else
                builder.AppendLine($"\t.balign 4");
            builder.AppendLine($"{staticVariable.Identifier}:");
            builder.AppendLine($"\t.zero 4");
        }
        else
        {
            builder.AppendLine($"\t.data");
            if (OperatingSystem.IsLinux())
                builder.AppendLine($"\t.align 4");
            else
                builder.AppendLine($"\t.balign 4");
            builder.AppendLine($"{staticVariable.Identifier}:");
            builder.AppendLine($"\t.long {staticVariable.Init}");
        }
    }

    private void EmitInstruction(Instruction instruction, StringBuilder builder)
    {
        switch (instruction)
        {
            case Instruction.Mov mov:
                builder.AppendLine($"\tmovl {EmitOperand(mov.Src)}, {EmitOperand(mov.Dst)}");
                break;
            case Instruction.Ret:
                builder.AppendLine($"\tmovq %rbp, %rsp");
                builder.AppendLine($"\tpopq %rbp");
                builder.AppendLine("\tret");
                break;
            case Instruction.Unary unary:
                builder.AppendLine($"\t{EmitUnaryOperator(unary.Operator)} {EmitOperand(unary.Operand)}");
                break;
            case Instruction.Binary binary:
                builder.AppendLine($"\t{EmitBinaryOperator(binary.Operator)} {EmitOperand(binary.SrcOperand)}, {EmitOperand(binary.DstOperand)}");
                break;
            case Instruction.Idiv idiv:
                builder.AppendLine($"\tidivl {EmitOperand(idiv.Operand)}");
                break;
            case Instruction.Cdq:
                builder.AppendLine("\tcdq");
                break;
            case Instruction.AllocateStack allocateStack:
                builder.AppendLine($"\tsubq ${allocateStack.Bytes}, %rsp");
                break;
            case Instruction.Cmp cmp:
                builder.AppendLine($"\tcmpl {EmitOperand(cmp.OperandA)}, {EmitOperand(cmp.OperandB)}");
                break;
            case Instruction.Jmp jmp:
                builder.AppendLine($"\tjmp .L{jmp.Identifier}");
                break;
            case Instruction.JmpCC jmpCC:
                builder.AppendLine($"\tj{EmitConditionCode(jmpCC.Condition)} .L{jmpCC.Identifier}");
                break;
            case Instruction.SetCC setCC:
                builder.AppendLine($"\tset{EmitConditionCode(setCC.Condition)} {EmitOperand(setCC.Operand, 1)}");
                break;
            case Instruction.Label label:
                builder.AppendLine($".L{label.Identifier}:");
                break;
            case Instruction.DeallocateStack deallocateStack:
                builder.AppendLine($"\taddq ${deallocateStack.Bytes}, %rsp");
                break;
            case Instruction.Push push:
                builder.AppendLine($"\tpushq {EmitOperand(push.Operand, 8)}");
                break;
            case Instruction.Call call:
                builder.AppendLine($"\tcall {call.Identifier}{(!((IdentifierAttributes.Function)symbolTable[call.Identifier].IdentifierAttributes).Defined && OperatingSystem.IsLinux() ? "@PLT" : "")}");
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private string EmitConditionCode(Instruction.ConditionCode condition)
    {
        return condition switch
        {
            Instruction.ConditionCode.E => "e",
            Instruction.ConditionCode.NE => "ne",
            Instruction.ConditionCode.G => "g",
            Instruction.ConditionCode.GE => "ge",
            Instruction.ConditionCode.L => "l",
            Instruction.ConditionCode.LE => "le",
            _ => throw new NotImplementedException()
        };
    }

    private string EmitBinaryOperator(Instruction.BinaryOperator binaryOperator)
    {
        return binaryOperator switch
        {
            Instruction.BinaryOperator.Add => "addl",
            Instruction.BinaryOperator.Sub => "subl",
            Instruction.BinaryOperator.Mult => "imull",
            _ => throw new NotImplementedException()
        };
    }

    private string EmitUnaryOperator(Instruction.UnaryOperator unaryOperator)
    {
        return unaryOperator switch
        {
            Instruction.UnaryOperator.Neg => "negl",
            Instruction.UnaryOperator.Not => "notl",
            _ => throw new NotImplementedException()
        };
    }

    private string EmitOperand(Operand operand, int bytes = 4)
    {
        return operand switch
        {
            Operand.Reg reg => EmitRegister(reg, bytes),
            Operand.Imm imm => $"${imm.Value}",
            Operand.Stack stack => $"{stack.Offset}(%rbp)",
            Operand.Data data => $"{data.Identifier}(%rip)",
            _ => throw new NotImplementedException()
        };
    }

    private string EmitRegister(Operand.Reg reg, int bytes = 4)
    {
        // note: need to keep this updated with Reg.RegisterName
        string[] byteRegs = ["%al", "%cl", "%dl", "%dil", "%sil", "%r8b", "%r9b", "%r10b", "%r11b"];
        string[] fourByteRegs = ["%eax", "%ecx", "%edx", "%edi", "%esi", "%r8d", "%r9d", "%r10d", "%r11d"];
        string[] eightByteRegs = ["%rax", "%rcx", "%rdx", "%rdi", "%rsi", "%r8", "%r9", "%r10", "%r11"];

        return bytes switch
        {
            1 => byteRegs[(int)reg.Register],
            4 => fourByteRegs[(int)reg.Register],
            8 => eightByteRegs[(int)reg.Register],
            _ => throw new NotImplementedException()
        };
    }
}