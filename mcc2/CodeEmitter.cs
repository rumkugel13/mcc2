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

        var isZero = GetValue(staticVariable.Init) == 0;
        builder.AppendLine($"\t.{(isZero ? "bss" : "data")}");
        builder.AppendLine($"\t.{(OperatingSystem.IsLinux() ? "align" : "balign")} {staticVariable.Alignment}");
        builder.AppendLine($"{staticVariable.Identifier}:");

        if (isZero)
            builder.AppendLine($"\t.zero {staticVariable.Alignment}");
        else
            builder.AppendLine($"\t.{EmitAssemblerType(staticVariable.Init)} {GetValue(staticVariable.Init)}");
    }

    private long GetValue(StaticInit staticInit)
    {
        return staticInit switch {
            StaticInit.IntInit init => init.Value,
            StaticInit.LongInit init => init.Value,
            StaticInit.UIntInit init => init.Value,
            StaticInit.ULongInit init => (long)init.Value,
            _ => throw new NotImplementedException()
        };
    }

    private string EmitAssemblerType(StaticInit staticInit)
    {
        return staticInit switch {
            StaticInit.IntInit or StaticInit.UIntInit => "long",
            StaticInit.LongInit or StaticInit.ULongInit => "quad",
            _ => throw new NotImplementedException()
        };
    }

    private void EmitInstruction(Instruction instruction, StringBuilder builder)
    {
        switch (instruction)
        {
            case Instruction.Mov mov:
                builder.AppendLine($"\tmov{EmitTypeSuffix(mov.Type)} {EmitOperand(mov.Src, mov.Type)}, {EmitOperand(mov.Dst, mov.Type)}");
                break;
            case Instruction.Movsx movsx:
                builder.AppendLine($"\tmovslq {EmitOperand(movsx.Src, Instruction.AssemblyType.Longword)}, {EmitOperand(movsx.Dst, Instruction.AssemblyType.Quadword)}");
                break;
            case Instruction.Ret:
                builder.AppendLine($"\tmovq %rbp, %rsp");
                builder.AppendLine($"\tpopq %rbp");
                builder.AppendLine("\tret");
                break;
            case Instruction.Unary unary:
                builder.AppendLine($"\t{EmitUnaryOperator(unary.Operator)}{EmitTypeSuffix(unary.Type)} {EmitOperand(unary.Operand, unary.Type)}");
                break;
            case Instruction.Binary binary:
                builder.AppendLine($"\t{EmitBinaryOperator(binary.Operator)}{EmitTypeSuffix(binary.Type)} {EmitOperand(binary.SrcOperand, binary.Type)}, {EmitOperand(binary.DstOperand, binary.Type)}");
                break;
            case Instruction.Idiv idiv:
                builder.AppendLine($"\tidiv{EmitTypeSuffix(idiv.Type)} {EmitOperand(idiv.Operand, idiv.Type)}");
                break;
            case Instruction.Div div:
                builder.AppendLine($"\tdiv{EmitTypeSuffix(div.Type)} {EmitOperand(div.Operand, div.Type)}");
                break;
            case Instruction.Cdq cdq:
                if (cdq.Type == Instruction.AssemblyType.Longword)
                    builder.AppendLine("\tcdq");
                else
                    builder.AppendLine("\tcqo");
                break;
            case Instruction.Cmp cmp:
                builder.AppendLine($"\tcmp{EmitTypeSuffix(cmp.Type)} {EmitOperand(cmp.OperandA, cmp.Type)}, {EmitOperand(cmp.OperandB, cmp.Type)}");
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
            case Instruction.Push push:
                builder.AppendLine($"\tpushq {EmitOperand(push.Operand, Instruction.AssemblyType.Quadword)}");
                break;
            case Instruction.Call call:
                builder.AppendLine($"\tcall {call.Identifier}{(!((AsmSymbolTableEntry.FunctionEntry)AssemblyGenerator.AsmSymbolTable[call.Identifier]).Defined && OperatingSystem.IsLinux() ? "@PLT" : "")}");
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private string EmitTypeSuffix(Instruction.AssemblyType assemblyType)
    {
        return assemblyType switch
        {
            Instruction.AssemblyType.Longword => "l",
            Instruction.AssemblyType.Quadword => "q",
            _ => throw new NotImplementedException()
        };
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
            Instruction.ConditionCode.A => "a",
            Instruction.ConditionCode.AE => "ae",
            Instruction.ConditionCode.B => "b",
            Instruction.ConditionCode.BE => "be",
            _ => throw new NotImplementedException()
        };
    }

    private string EmitBinaryOperator(Instruction.BinaryOperator binaryOperator)
    {
        return binaryOperator switch
        {
            Instruction.BinaryOperator.Add => "add",
            Instruction.BinaryOperator.Sub => "sub",
            Instruction.BinaryOperator.Mult => "imul",
            _ => throw new NotImplementedException()
        };
    }

    private string EmitUnaryOperator(Instruction.UnaryOperator unaryOperator)
    {
        return unaryOperator switch
        {
            Instruction.UnaryOperator.Neg => "neg",
            Instruction.UnaryOperator.Not => "not",
            _ => throw new NotImplementedException()
        };
    }

    private string EmitOperand(Operand operand, int bytes)
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

    private string EmitOperand(Operand operand, Instruction.AssemblyType assemblyType)
    {
        return operand switch
        {
            Operand.Reg reg => EmitRegister(reg, assemblyType),
            Operand.Imm imm => $"${imm.Value}",
            Operand.Stack stack => $"{stack.Offset}(%rbp)",
            Operand.Data data => $"{data.Identifier}(%rip)",
            _ => throw new NotImplementedException()
        };
    }

    // note: need to keep this updated with Reg.RegisterName
    readonly string[] byteRegs = ["%al", "%cl", "%dl", "%dil", "%sil", "%r8b", "%r9b", "%r10b", "%r11b", "%spl"];
    readonly string[] fourByteRegs = ["%eax", "%ecx", "%edx", "%edi", "%esi", "%r8d", "%r9d", "%r10d", "%r11d", "%esp"];
    readonly string[] eightByteRegs = ["%rax", "%rcx", "%rdx", "%rdi", "%rsi", "%r8", "%r9", "%r10", "%r11", "%rsp"];

    private string EmitRegister(Operand.Reg reg, int bytes)
    {
        return bytes switch
        {
            1 => byteRegs[(int)reg.Register],
            4 => fourByteRegs[(int)reg.Register],
            8 => eightByteRegs[(int)reg.Register],
            _ => throw new NotImplementedException()
        };
    }

    private string EmitRegister(Operand.Reg reg, Instruction.AssemblyType assemblyType)
    {
        return assemblyType switch
        {
            Instruction.AssemblyType.Longword => fourByteRegs[(int)reg.Register],
            Instruction.AssemblyType.Quadword => eightByteRegs[(int)reg.Register],
            _ => throw new NotImplementedException()
        };
    }
}