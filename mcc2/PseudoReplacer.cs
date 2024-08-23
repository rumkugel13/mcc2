using mcc2.Assembly;

namespace mcc2;

public class PseudoReplacer
{
    public Dictionary<string, long> OffsetMap = [];
    private long currentOffset;

    public PseudoReplacer(string functionName)
    {
        if (AssemblyGenerator.AsmSymbolTable[functionName] is AsmSymbolTableEntry.FunctionEntry funcEntry && funcEntry.ReturnOnStack)
            currentOffset = 8;
    }

    public long Replace(List<Instruction> instructions)
    {
        for (int i = 0; i < instructions.Count; i++)
        {
            Instruction? inst = instructions[i];
            switch (inst)
            {
                case Instruction.Mov mov:
                    {
                        var src = mov.Src;
                        var dst = mov.Dst;
                        if (src is Operand.Pseudo or Operand.PseudoMemory)
                            src = ReplacePseudo(src);

                        if (dst is Operand.Pseudo or Operand.PseudoMemory)
                            dst = ReplacePseudo(dst);

                        instructions[i] = new Instruction.Mov(mov.Type, src, dst);
                        break;
                    }
                case Instruction.Unary unary:
                    {
                        var op = unary.Operand;
                        if (op is Operand.Pseudo or Operand.PseudoMemory)
                            op = ReplacePseudo(op);

                        instructions[i] = new Instruction.Unary(unary.Operator, unary.Type, op);
                        break;
                    }
                case Instruction.Binary binary:
                    {
                        var src = binary.SrcOperand;
                        var dst = binary.DstOperand;
                        if (src is Operand.Pseudo or Operand.PseudoMemory)
                            src = ReplacePseudo(src);

                        if (dst is Operand.Pseudo or Operand.PseudoMemory)
                            dst = ReplacePseudo(dst);

                        instructions[i] = new Instruction.Binary(binary.Operator, binary.Type, src, dst);
                        break;
                    }
                case Instruction.Idiv idiv:
                    {
                        var op = idiv.Operand;
                        if (op is Operand.Pseudo or Operand.PseudoMemory)
                            op = ReplacePseudo(op);

                        instructions[i] = new Instruction.Idiv(idiv.Type, op);
                        break;
                    }
                case Instruction.Div div:
                    {
                        var op = div.Operand;
                        if (op is Operand.Pseudo or Operand.PseudoMemory)
                            op = ReplacePseudo(op);

                        instructions[i] = new Instruction.Div(div.Type, op);
                        break;
                    }
                case Instruction.Cmp cmp:
                    {
                        var opA = cmp.OperandA;
                        var opB = cmp.OperandB;
                        if (opA is Operand.Pseudo or Operand.PseudoMemory)
                            opA = ReplacePseudo(opA);

                        if (opB is Operand.Pseudo or Operand.PseudoMemory)
                            opB = ReplacePseudo(opB);

                        instructions[i] = new Instruction.Cmp(cmp.Type, opA, opB);
                        break;
                    }
                case Instruction.SetCC setCC:
                    {
                        var op = setCC.Operand;
                        if (op is Operand.Pseudo or Operand.PseudoMemory)
                            op = ReplacePseudo(op);

                        instructions[i] = new Instruction.SetCC(setCC.Condition, op);
                        break;
                    }
                case Instruction.Push push:
                    {
                        var op = push.Operand;
                        if (op is Operand.Pseudo or Operand.PseudoMemory)
                            op = ReplacePseudo(op);

                        instructions[i] = new Instruction.Push(op);
                        break;
                    }
                case Instruction.Movsx movsx:
                    {
                        var src = movsx.Src;
                        var dst = movsx.Dst;
                        if (src is Operand.Pseudo or Operand.PseudoMemory)
                            src = ReplacePseudo(src);

                        if (dst is Operand.Pseudo or Operand.PseudoMemory)
                            dst = ReplacePseudo(dst);

                        instructions[i] = new Instruction.Movsx(movsx.SrcType, movsx.DstType, src, dst);
                        break;
                    }
                case Instruction.MovZeroExtend movzx:
                    {
                        var src = movzx.Src;
                        var dst = movzx.Dst;
                        if (src is Operand.Pseudo or Operand.PseudoMemory)
                            src = ReplacePseudo(src);

                        if (dst is Operand.Pseudo or Operand.PseudoMemory)
                            dst = ReplacePseudo(dst);

                        instructions[i] = new Instruction.MovZeroExtend(movzx.SrcType, movzx.DstType, src, dst);
                        break;
                    }
                case Instruction.Cvttsd2si cvttsd2si:
                    {
                        var src = cvttsd2si.Src;
                        var dst = cvttsd2si.Dst;
                        if (src is Operand.Pseudo or Operand.PseudoMemory)
                            src = ReplacePseudo(src);

                        if (dst is Operand.Pseudo or Operand.PseudoMemory)
                            dst = ReplacePseudo(dst);

                        instructions[i] = new Instruction.Cvttsd2si(cvttsd2si.DstType, src, dst);
                    }
                    break;
                case Instruction.Cvtsi2sd cvtsi2sd:
                    {
                        var src = cvtsi2sd.Src;
                        var dst = cvtsi2sd.Dst;
                        if (src is Operand.Pseudo or Operand.PseudoMemory)
                            src = ReplacePseudo(src);

                        if (dst is Operand.Pseudo or Operand.PseudoMemory)
                            dst = ReplacePseudo(dst);

                        instructions[i] = new Instruction.Cvtsi2sd(cvtsi2sd.SrcType, src, dst);
                    }
                    break;
                case Instruction.Lea lea:
                    {
                        var src = lea.Src;
                        var dst = lea.Dst;
                        if (src is Operand.Pseudo or Operand.PseudoMemory)
                            src = ReplacePseudo(src);

                        if (dst is Operand.Pseudo or Operand.PseudoMemory)
                            dst = ReplacePseudo(dst);

                        instructions[i] = new Instruction.Lea(src, dst);
                    }
                    break;
            }
        }

        return currentOffset;
    }

    private Operand ReplacePseudo(Operand operand)
    {
        string name;
        long offset = 0;
        bool isMemory = false;
        if (operand is Operand.Pseudo pseudo)
            name = pseudo.Identifier;
        else if (operand is Operand.PseudoMemory pseudoMemory)
        {
            name = pseudoMemory.Identifier;
            offset = pseudoMemory.Offset;
            isMemory = true;
        }
        else
            return operand;

        if (OffsetMap.TryGetValue(name, out long val))
        {
            if (isMemory)
                val += offset;
            return new Operand.Memory(Operand.RegisterName.BP, val);
        }

        if ((AsmSymbolTableEntry.ObjectEntry)AssemblyGenerator.AsmSymbolTable[name] is AsmSymbolTableEntry.ObjectEntry objEntry && objEntry.IsStatic)
        {
            return new Operand.Data(name, offset);
        }

        var (size, align) = ((AsmSymbolTableEntry.ObjectEntry)AssemblyGenerator.AsmSymbolTable[name]).AssemblyType switch
        {
            AssemblyType.Longword => (4, 4),
            AssemblyType.Quadword => (8, 8),
            AssemblyType.Double => (8, 8),
            AssemblyType.ByteArray array => (array.Size, array.Alignment),
            AssemblyType.Byte => (1, 1),
            _ => throw new NotImplementedException()
        };

        currentOffset = AssemblyGenerator.AlignTo(currentOffset + size, align);
        OffsetMap[name] = -currentOffset;
        return new Operand.Memory(Operand.RegisterName.BP, -currentOffset);
    }
}