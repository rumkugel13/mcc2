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
        foreach (var inst in instructions)
        {
            switch (inst)
            {
                case Mov mov:
                    {
                        if (mov.src is Pseudo pseudoSrc)
                            mov.src = ReplacePseudo(pseudoSrc.Identifier);

                        if (mov.dst is Pseudo pseudoDst)
                            mov.dst = ReplacePseudo(pseudoDst.Identifier);
                        break;
                    }
                case Unary unary:
                    {
                        if (unary.Operand is Pseudo pseudoOp)
                            unary.Operand = ReplacePseudo(pseudoOp.Identifier);
                        break;
                    }
                case Binary binary:
                    {
                        if (binary.SrcOperand is Pseudo pseudoSrc)
                            binary.SrcOperand = ReplacePseudo(pseudoSrc.Identifier);

                        if (binary.DstOperand is Pseudo pseudoDst)
                            binary.DstOperand = ReplacePseudo(pseudoDst.Identifier);
                        break;
                    }
                case Idiv idiv:
                    {
                        if (idiv.Operand is Pseudo pseudo)
                            idiv.Operand = ReplacePseudo(pseudo.Identifier);
                        break;
                    }
                case Cmp cmp:
                    {
                        if (cmp.OperandA is Pseudo pseudoA)
                            cmp.OperandA = ReplacePseudo(pseudoA.Identifier);

                        if (cmp.OperandB is Pseudo pseudoB)
                            cmp.OperandB = ReplacePseudo(pseudoB.Identifier);
                        break;
                    }
                case SetCC setCC:
                    {
                        if (setCC.Operand is Pseudo pseudo)
                            setCC.Operand = ReplacePseudo(pseudo.Identifier);
                        break;
                    }
                case Push push:
                    {
                        if (push.Operand is Pseudo pseudo)
                            push.Operand = ReplacePseudo(pseudo.Identifier);
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
            return new Stack(val);
        }

        if (symbolTable.TryGetValue(name, out SemanticAnalyzer.SymbolEntry symbolEntry) &&
            symbolEntry.IdentifierAttributes is IdentifierAttributes.Static)
        { 
            return new Data(name); 
        }

        currentOffset -= 4;
        OffsetMap[name] = currentOffset;
        return new Stack(currentOffset);
    }
}