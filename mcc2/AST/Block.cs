namespace mcc2.AST;

public class Block
{
    public List<BlockItem> BlockItems;

    public Block(List<BlockItem> blockItems)
    {
        this.BlockItems = blockItems;
    }
}