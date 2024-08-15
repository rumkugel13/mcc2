using mcc2.Assembly;

namespace mcc2;

public class PseudoReplacer
{
    public Dictionary<string, int> OffsetMap = [];
    private int currentOffset;

    public int Replace(List<Instruction> instructions)
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
                        if (src is Operand.Pseudo pseudoSrc)
                            src = ReplacePseudo(pseudoSrc.Identifier);

                        if (dst is Operand.Pseudo pseudoDst)
                            dst = ReplacePseudo(pseudoDst.Identifier);

                        instructions[i] = new Instruction.Mov(mov.Type, src, dst);
                        break;
                    }
                case Instruction.Unary unary:
                    {
                        var op = unary.Operand;
                        if (op is Operand.Pseudo pseudoOp)
                            op = ReplacePseudo(pseudoOp.Identifier);

                        instructions[i] = new Instruction.Unary(unary.Operator, unary.Type, op);
                        break;
                    }
                case Instruction.Binary binary:
                    {
                        var src = binary.SrcOperand;
                        var dst = binary.DstOperand;
                        if (src is Operand.Pseudo pseudoSrc)
                            src = ReplacePseudo(pseudoSrc.Identifier);

                        if (dst is Operand.Pseudo pseudoDst)
                            dst = ReplacePseudo(pseudoDst.Identifier);

                        instructions[i] = new Instruction.Binary(binary.Operator, binary.Type, src, dst);
                        break;
                    }
                case Instruction.Idiv idiv:
                    {
                        var op = idiv.Operand;
                        if (op is Operand.Pseudo pseudo)
                            op = ReplacePseudo(pseudo.Identifier);

                        instructions[i] = new Instruction.Idiv(idiv.Type, op);
                        break;
                    }
                case Instruction.Div div:
                    {
                        var op = div.Operand;
                        if (op is Operand.Pseudo pseudo)
                            op = ReplacePseudo(pseudo.Identifier);

                        instructions[i] = new Instruction.Div(div.Type, op);
                        break;
                    }
                case Instruction.Cmp cmp:
                    {
                        var opA = cmp.OperandA;
                        var opB = cmp.OperandB;
                        if (opA is Operand.Pseudo pseudoA)
                            opA = ReplacePseudo(pseudoA.Identifier);

                        if (opB is Operand.Pseudo pseudoB)
                            opB = ReplacePseudo(pseudoB.Identifier);

                        instructions[i] = new Instruction.Cmp(cmp.Type, opA, opB);
                        break;
                    }
                case Instruction.SetCC setCC:
                    {
                        var op = setCC.Operand;
                        if (op is Operand.Pseudo pseudo)
                            op = ReplacePseudo(pseudo.Identifier);
                        
                        instructions[i] = new Instruction.SetCC(setCC.Condition, op);
                        break;
                    }
                case Instruction.Push push:
                    {
                        var op = push.Operand;
                        if (op is Operand.Pseudo pseudo)
                            op = ReplacePseudo(pseudo.Identifier);

                        instructions[i] = new Instruction.Push(op);
                        break;
                    }
                case Instruction.Movsx movsx:
                    {
                        var src = movsx.Src;
                        var dst = movsx.Dst;
                        if (src is Operand.Pseudo pseudoSrc)
                            src = ReplacePseudo(pseudoSrc.Identifier);

                        if (dst is Operand.Pseudo pseudoDst)
                            dst = ReplacePseudo(pseudoDst.Identifier);

                        instructions[i] = new Instruction.Movsx(src, dst);
                        break;
                    }
                case Instruction.MovZeroExtend movzx:
                    {
                        var src = movzx.Src;
                        var dst = movzx.Dst;
                        if (src is Operand.Pseudo pseudoSrc)
                            src = ReplacePseudo(pseudoSrc.Identifier);

                        if (dst is Operand.Pseudo pseudoDst)
                            dst = ReplacePseudo(pseudoDst.Identifier);

                        instructions[i] = new Instruction.MovZeroExtend(src, dst);
                        break;
                    }
                case Instruction.Cvttsd2si cvttsd2si:
                    {
                        var src = cvttsd2si.Src;
                        var dst = cvttsd2si.Dst;
                        if (src is Operand.Pseudo pseudoSrc)
                            src = ReplacePseudo(pseudoSrc.Identifier);

                        if (dst is Operand.Pseudo pseudoDst)
                            dst = ReplacePseudo(pseudoDst.Identifier);

                        instructions[i] = new Instruction.Cvttsd2si(cvttsd2si.DstType, src, dst);
                    }
                    break;
                case Instruction.Cvtsi2sd cvtsi2sd:
                    {
                        var src = cvtsi2sd.Src;
                        var dst = cvtsi2sd.Dst;
                        if (src is Operand.Pseudo pseudoSrc)
                            src = ReplacePseudo(pseudoSrc.Identifier);

                        if (dst is Operand.Pseudo pseudoDst)
                            dst = ReplacePseudo(pseudoDst.Identifier);

                        instructions[i] = new Instruction.Cvtsi2sd(cvtsi2sd.SrcType, src, dst);
                    }
                    break;
            }
        }

        return currentOffset;
    }

    private Operand ReplacePseudo(string name)
    {
        if (OffsetMap.TryGetValue(name, out int val))
        {
            return new Operand.Stack(val);
        }

        if ((AsmSymbolTableEntry.ObjectEntry)AssemblyGenerator.AsmSymbolTable[name] is AsmSymbolTableEntry.ObjectEntry objEntry && objEntry.IsStatic)
        { 
            return new Operand.Data(name); 
        }

        var align = ((AsmSymbolTableEntry.ObjectEntry)AssemblyGenerator.AsmSymbolTable[name]).AssemblyType switch {
            Instruction.AssemblyType.Longword => 4,
            Instruction.AssemblyType.Quadword => 8,
            Instruction.AssemblyType.Double => 8,
            _ => throw new NotImplementedException()
        };

        currentOffset = AssemblyGenerator.AlignTo(currentOffset + align, align);
        OffsetMap[name] = -currentOffset;
        return new Operand.Stack(-currentOffset);
    }
}