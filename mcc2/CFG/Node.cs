using mcc2.TAC;

namespace mcc2.CFG;

public abstract record Node
{
    public enum BlockId
    {
        ENTRY,
        EXIT,
        START,
    }

    public int Id;

    public record BasicBlock(int Id, List<Instruction> Instructions, List<int> Predecessors, List<int> Successors) : Node(Id);
    public record EntryNode(List<int> Successors) : Node((int)BlockId.ENTRY);
    public record ExitNode(List<int> Predecessors) : Node((int)BlockId.EXIT);

    private Node(int id) { Id = id; }
}