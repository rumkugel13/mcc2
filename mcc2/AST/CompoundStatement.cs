namespace mcc2.AST;

public class CompoundStatement : Statement
{
    public Block Block;

    public CompoundStatement(Block block)
    {
        this.Block = block;
    }
}