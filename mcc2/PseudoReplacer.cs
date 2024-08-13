using mcc2.Assembly;

namespace mcc2;

public class PseudoReplacer
{
    public Dictionary<string, int> OffsetMap = [];
    private int currentOffset;
    private Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable;

    public PseudoReplacer(Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable)
    {
        this.symbolTable = symbolTable;
    }

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

                        instructions[i] = new Instruction.Mov(src, dst);
                        break;
                    }
                case Instruction.Unary unary:
                    {
                        var op = unary.Operand;
                        if (op is Operand.Pseudo pseudoOp)
                            op = ReplacePseudo(pseudoOp.Identifier);

                        instructions[i] = new Instruction.Unary(unary.Operator, op);
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

                        instructions[i] = new Instruction.Binary(binary.Operator, src, dst);
                        break;
                    }
                case Instruction.Idiv idiv:
                    {
                        var op = idiv.Operand;
                        if (op is Operand.Pseudo pseudo)
                            op = ReplacePseudo(pseudo.Identifier);

                        instructions[i] = new Instruction.Idiv(op);
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

                        instructions[i] = new Instruction.Cmp(opA, opB);
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
            }
        }

        // note: we sub the offset from rsp, so we need to negate it
        return -currentOffset;
    }

    private Operand ReplacePseudo(string name)
    {
        if (OffsetMap.TryGetValue(name, out int val))
        {
            return new Operand.Stack(val);
        }

        if (symbolTable.TryGetValue(name, out SemanticAnalyzer.SymbolEntry symbolEntry) &&
            symbolEntry.IdentifierAttributes is IdentifierAttributes.Static)
        { 
            return new Operand.Data(name); 
        }

        currentOffset -= 4;
        OffsetMap[name] = currentOffset;
        return new Operand.Stack(currentOffset);
    }
}