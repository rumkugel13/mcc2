using mcc2.Assembly;

namespace mcc2.CFG;

public abstract record AssNode
{
    public enum BlockId
    {
        ENTRY,
        EXIT,
        START,
    }

    public int Id;

    public record BasicBlock(int Id, List<Instruction> Instructions, List<int> Predecessors, List<int> Successors) : AssNode(Id);
    public record EntryNode(List<int> Successors) : AssNode((int)BlockId.ENTRY);
    public record ExitNode(List<int> Predecessors) : AssNode((int)BlockId.EXIT);

    private AssNode(int id) { Id = id; }
}