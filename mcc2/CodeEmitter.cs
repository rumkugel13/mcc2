using System.Text;
using mcc2.Assembly;

namespace mcc2;

public class CodeEmitter
{
    readonly StringBuilder builder;
    
    public CodeEmitter()
    {
        builder = new();
    }

    public StringBuilder Emit(AssemblyProgram program)
    {
        EmitProgram(program);
        if (OperatingSystem.IsLinux())
            EmitLine("\t.section .note.GNU-stack,\"\",@progbits");
        return builder;
    }

    private void EmitProgram(AssemblyProgram program)
    {
        foreach (var topLevel in program.TopLevel)
            switch (topLevel)
            {
                case TopLevel.Function fun:
                    EmitFunction(fun);
                    break;
                case TopLevel.StaticVariable stat:
                    EmitStaticVariable(stat);
                    break;
                case TopLevel.StaticConstant statConst:
                    EmitStaticConstant(statConst);
                    break;
            }
    }

    private void EmitFunction(TopLevel.Function function)
    {
        if (function.Global)
            EmitLine($"\t.globl {function.Name}");

        EmitLine($"\t.text");
        EmitLine($"{function.Name}:");
        EmitLine($"\tpushq %rbp");
        EmitLine($"\tmovq %rsp, %rbp");
        foreach (var inst in function.Instructions)
        {
            EmitInstruction(inst);
        }
    }

    private void EmitStaticVariable(TopLevel.StaticVariable staticVariable)
    {
        if (staticVariable.Global)
            EmitLine($"\t.globl {staticVariable.Identifier}");

        var isZero = IsZero(staticVariable.Inits);
        EmitLine($"\t.{(isZero ? "bss" : "data")}");
        EmitLine($"\t.{(OperatingSystem.IsLinux() ? "align" : "balign")} {staticVariable.Alignment}");
        EmitLine($"{staticVariable.Identifier}:");

        if (isZero && staticVariable.Inits[0] is not StaticInit.ZeroInit)
            EmitLine($"\t.zero {staticVariable.Alignment}");
        else
        {
            foreach (var init in staticVariable.Inits)
            {
                switch (init)
                {
                    case StaticInit.PointerInit pointer:
                        EmitLine($"\t.quad {pointer.Name}");
                        break;
                    case StaticInit.StringInit stringInit:
                        EmitStringConstant(stringInit);
                        break;
                    default:
                        EmitLine($"\t.{EmitAssemblerType(init)} {GetValue(init)}");
                        break;
                }
            }   
        }
    }

    private bool IsZero(List<StaticInit> inits)
    {
        if (inits.Count != 1)
            return false;
        if (inits[0] is StaticInit.ZeroInit)
            return true;
        if (inits[0] is not StaticInit.DoubleInit and not StaticInit.StringInit and not StaticInit.PointerInit && GetValue(inits[0]) == 0)
            return true;
        return false;
    }

    private void EmitStringConstant(StaticInit.StringInit stringInit)
    {
        EmitLine($"\t.asci{(stringInit.NullTerminated ? "z" : "i")} \"{System.Text.RegularExpressions.Regex.Escape(stringInit.Value).
                        Replace("\'", "\\'").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\a", "\\007").Replace("\b", "\\b").Replace("\f", "\\f")
                        .Replace("\r", "\\r").Replace("\t", "\\t").Replace("\v", "\\v")}\"");
    }

    private void EmitStaticConstant(TopLevel.StaticConstant statConst)
    {
        if (OperatingSystem.IsLinux())
            EmitLine($"\t.section .rodata");
        if (OperatingSystem.IsMacOS())
            EmitLine($"\t.{(statConst.Init is StaticInit.StringInit ? "cstring" : $"literal{statConst.Alignment}")}");

        EmitLine($"\t.{(OperatingSystem.IsLinux() ? "align" : "balign")} {statConst.Alignment}");
        EmitLine($"{statConst.Identifier}:");
        switch (statConst.Init)
        {
            case StaticInit.PointerInit pointer:
                EmitLine($"\t.quad {pointer.Name}");
                break;
            case StaticInit.StringInit stringInit:
                EmitStringConstant(stringInit);
                break;
            default:
                EmitLine($"\t.{EmitAssemblerType(statConst.Init)} {GetValue(statConst.Init)}");
                if (OperatingSystem.IsMacOS() && statConst.Alignment == 16)
                    EmitLine($"\t.quad 0");
                break;
        }
    }

    private long GetValue(StaticInit staticInit)
    {
        return staticInit switch {
            StaticInit.IntInit init => (long)init.Value,
            StaticInit.LongInit init => (long)init.Value,
            StaticInit.UIntInit init => (long)(ulong)init.Value,
            StaticInit.ULongInit init => (long)init.Value,
            StaticInit.DoubleInit init => (long)BitConverter.DoubleToInt64Bits(init.Value),
            StaticInit.ZeroInit init => (long)init.Bytes,
            StaticInit.CharInit init => (long)init.Value,
            StaticInit.UCharInit init => (long)(ulong)(byte)init.Value,
            _ => throw new NotImplementedException()
        };
    }

    private string EmitAssemblerType(StaticInit staticInit)
    {
        return staticInit switch {
            StaticInit.IntInit or StaticInit.UIntInit => "long",
            StaticInit.LongInit or StaticInit.ULongInit or StaticInit.DoubleInit or StaticInit.PointerInit => "quad",
            StaticInit.ZeroInit => "zero",
            StaticInit.CharInit or StaticInit.UCharInit => "byte",
            _ => throw new NotImplementedException()
        };
    }

    private void EmitInstruction(Instruction instruction)
    {
        switch (instruction)
        {
            case Instruction.Mov mov:
                EmitLine($"\tmov{EmitTypeSuffix(mov.Type)} {EmitOperand(mov.Src, mov.Type)}, {EmitOperand(mov.Dst, mov.Type)}");
                break;
            case Instruction.Movsx movsx:
                EmitLine($"\tmovs{EmitTypeSuffix(movsx.SrcType)}{EmitTypeSuffix(movsx.DstType)} {EmitOperand(movsx.Src, movsx.SrcType)}, {EmitOperand(movsx.Dst, movsx.DstType)}");
                break;
            case Instruction.MovZeroExtend movzx:
                EmitLine($"\tmovz{EmitTypeSuffix(movzx.SrcType)}{EmitTypeSuffix(movzx.DstType)} {EmitOperand(movzx.Src, movzx.SrcType)}, {EmitOperand(movzx.Dst, movzx.DstType)}");
                break;
            case Instruction.Ret:
                EmitLine($"\tmovq %rbp, %rsp");
                EmitLine($"\tpopq %rbp");
                EmitLine("\tret");
                break;
            case Instruction.Unary unary:
                EmitLine($"\t{EmitUnaryOperator(unary.Operator)}{EmitTypeSuffix(unary.Type)} {EmitOperand(unary.Operand, unary.Type)}");
                break;
            case Instruction.Binary binary:
                if (binary.Type is AssemblyType.Double && binary.Operator is Instruction.BinaryOperator.Xor or Instruction.BinaryOperator.Mult)
                {
                    if (binary.Operator == Instruction.BinaryOperator.Xor)
                        EmitLine($"\txorpd {EmitOperand(binary.Src, binary.Type)}, {EmitOperand(binary.Dst, binary.Type)}");
                    else if (binary.Operator == Instruction.BinaryOperator.Mult)
                        EmitLine($"\tmulsd {EmitOperand(binary.Src, binary.Type)}, {EmitOperand(binary.Dst, binary.Type)}");
                }
                else if (binary.Operator is Instruction.BinaryOperator.Shl or Instruction.BinaryOperator.ShrTwoOp or Instruction.BinaryOperator.Sar)
                {
                    EmitLine($"\t{EmitBinaryOperator(binary.Operator)}{EmitTypeSuffix(binary.Type)} {EmitOperand(binary.Src, new AssemblyType.Byte())}, {EmitOperand(binary.Dst, binary.Type)}");
                }
                else
                    EmitLine($"\t{EmitBinaryOperator(binary.Operator)}{EmitTypeSuffix(binary.Type)} {EmitOperand(binary.Src, binary.Type)}, {EmitOperand(binary.Dst, binary.Type)}");
                break;
            case Instruction.Idiv idiv:
                EmitLine($"\tidiv{EmitTypeSuffix(idiv.Type)} {EmitOperand(idiv.Operand, idiv.Type)}");
                break;
            case Instruction.Div div:
                EmitLine($"\tdiv{EmitTypeSuffix(div.Type)} {EmitOperand(div.Operand, div.Type)}");
                break;
            case Instruction.Cdq cdq:
                if (cdq.Type is AssemblyType.Longword)
                    EmitLine("\tcdq");
                else
                    EmitLine("\tcqo");
                break;
            case Instruction.Cmp cmp:
                if (cmp.Type is AssemblyType.Double)
                    EmitLine($"\tcomisd {EmitOperand(cmp.OperandA, cmp.Type)}, {EmitOperand(cmp.OperandB, cmp.Type)}");
                else
                    EmitLine($"\tcmp{EmitTypeSuffix(cmp.Type)} {EmitOperand(cmp.OperandA, cmp.Type)}, {EmitOperand(cmp.OperandB, cmp.Type)}");
                break;
            case Instruction.Jmp jmp:
                EmitLine($"\tjmp .L{jmp.Identifier}");
                break;
            case Instruction.JmpCC jmpCC:
                EmitLine($"\tj{EmitConditionCode(jmpCC.Condition)} .L{jmpCC.Identifier}");
                break;
            case Instruction.SetCC setCC:
                EmitLine($"\tset{EmitConditionCode(setCC.Condition)} {EmitOperand(setCC.Operand, new AssemblyType.Byte())}");
                break;
            case Instruction.Label label:
                EmitLine($".L{label.Identifier}:");
                break;
            case Instruction.Push push:
                EmitLine($"\tpushq {EmitOperand(push.Operand, new AssemblyType.Quadword())}");
                break;
            case Instruction.Pop pop:
                EmitLine($"\tpopq {EmitRegister(pop.Register, new AssemblyType.Quadword())}");
                break;
            case Instruction.Call call:
                EmitLine($"\tcall {call.Identifier}{(!((AsmSymbolTableEntry.FunctionEntry)AssemblyGenerator.AsmSymbolTable[call.Identifier]).Defined && OperatingSystem.IsLinux() ? "@PLT" : "")}");
                break;
            case Instruction.Cvtsi2sd cvtsi2sd:
                EmitLine($"\tcvtsi2sd{EmitTypeSuffix(cvtsi2sd.SrcType)} {EmitOperand(cvtsi2sd.Src, cvtsi2sd.SrcType)}, {EmitOperand(cvtsi2sd.Dst, new AssemblyType.Double())}");
                break;
            case Instruction.Cvttsd2si cvttsd2si:
                EmitLine($"\tcvttsd2si{EmitTypeSuffix(cvttsd2si.DstType)} {EmitOperand(cvttsd2si.Src, new AssemblyType.Double())}, {EmitOperand(cvttsd2si.Dst, cvttsd2si.DstType)}");
                break;
            case Instruction.Lea lea:
                EmitLine($"\tleaq {EmitOperand(lea.Src, new AssemblyType.Quadword())}, {EmitOperand(lea.Dst, new AssemblyType.Quadword())}");
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private string EmitTypeSuffix(AssemblyType assemblyType)
    {
        return assemblyType switch
        {
            AssemblyType.Byte => "b",
            AssemblyType.Longword => "l",
            AssemblyType.Quadword => "q",
            AssemblyType.Double => "sd",
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
            Instruction.ConditionCode.P => "p",
            Instruction.ConditionCode.NP => "np",
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
            Instruction.BinaryOperator.DivDouble => "div",
            Instruction.BinaryOperator.And => "and",
            Instruction.BinaryOperator.Or => "or",
            Instruction.BinaryOperator.Xor => "xor",
            Instruction.BinaryOperator.Shl => "shl",
            Instruction.BinaryOperator.ShrTwoOp => "shr",
            Instruction.BinaryOperator.Sar => "sar",
            _ => throw new NotImplementedException()
        };
    }

    private string EmitUnaryOperator(Instruction.UnaryOperator unaryOperator)
    {
        return unaryOperator switch
        {
            Instruction.UnaryOperator.Neg => "neg",
            Instruction.UnaryOperator.Not => "not",
            Instruction.UnaryOperator.Shr => "shr",
            _ => throw new NotImplementedException()
        };
    }

    private string EmitOperand(Operand operand, AssemblyType assemblyType)
    {
        return operand switch
        {
            Operand.Reg reg => EmitRegister(reg.Register, assemblyType),
            Operand.Imm imm => $"${(long)imm.Value}",
            Operand.Memory memory => $"{(memory.Offset != 0 ? memory.Offset : "")}({EmitRegister(memory.Register, new AssemblyType.Quadword())})",
            Operand.Data data => $"{data.Identifier}{(data.Offset != 0 ? $"+{data.Offset}" : "")}(%rip)",
            Operand.Indexed indexed => $"({EmitRegister(indexed.Base, new AssemblyType.Quadword())}, {EmitRegister(indexed.Index, new AssemblyType.Quadword())}, {indexed.Scale})",
            _ => throw new NotImplementedException()
        };
    }

    // note: need to keep this updated with Reg.RegisterName
    readonly string[] byteRegs = ["%al", "%bl", "%cl", "%dl", "%dil", "%sil", "%r8b", "%r9b", "%r10b", "%r11b", "%r12b", "%r13b", "%r14b", "%r15b", "%spl", "%bpl"];
    readonly string[] fourByteRegs = ["%eax", "%ebx", "%ecx", "%edx", "%edi", "%esi", "%r8d", "%r9d", "%r10d", "%r11d", "%r12d", "%r13d", "%r14d", "%r15d", "%esp", "%ebp"];
    readonly string[] eightByteRegs = ["%rax", "%rbx", "%rcx", "%rdx", "%rdi", "%rsi", "%r8", "%r9", "%r10", "%r11", "%r12", "%r13", "%r14", "%r15", "%rsp", "%rbp"];
    readonly string[] floatRegs = ["%xmm0", "%xmm1", "%xmm2", "%xmm3", "%xmm4", "%xmm5", "%xmm6", "%xmm7", "%xmm8", "%xmm9", "%xmm10", "%xmm11", "%xmm12", "%xmm13", "%xmm14", "%xmm15"];

    private string EmitRegister(Operand.RegisterName reg, AssemblyType assemblyType)
    {
        return assemblyType switch
        {
            AssemblyType.Byte => byteRegs[(int)reg],
            AssemblyType.Longword => fourByteRegs[(int)reg],
            AssemblyType.Quadword => eightByteRegs[(int)reg],
            AssemblyType.Double => floatRegs[(int)(reg - byteRegs.Length)],
            _ => throw new NotImplementedException()
        };
    }

    private void EmitLine(string line)
    {
        builder.AppendLine(line);
    }
}