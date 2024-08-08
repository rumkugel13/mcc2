using mcc2.Assembly;

namespace mcc2;

public class PseudoReplacer
{
    public Dictionary<string, int> OffsetMap = [];
    private int currentOffset;

    public int Replace(List<Instruction> instructions)
    {
        foreach(var inst in instructions)
        {
            switch (inst)
            {
                case Mov mov:
                    if (mov.src is Pseudo pseudoSrc)
                        mov.src = ReplacePseudo(pseudoSrc.Identifier);
                    
                    if (mov.dst is Pseudo pseudoDst)
                        mov.dst = ReplacePseudo(pseudoDst.Identifier);
                    
                    break;
                case Unary unary:
                    if (unary.Operand is Pseudo pseudoOp)
                        unary.Operand = ReplacePseudo(pseudoOp.Identifier);
                    break;
            }
        }

        // note: we sub the offset from rsp, so we need to negate it
        return -currentOffset;
    }

    private Stack ReplacePseudo(string name)
    {
        if (OffsetMap.TryGetValue(name, out int val))
        {
            return new Stack(val);
        }

        currentOffset -= 4;
        OffsetMap[name] = currentOffset;
        return new Stack(currentOffset);
    }
}