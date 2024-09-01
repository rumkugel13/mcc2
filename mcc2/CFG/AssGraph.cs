using mcc2.Assembly;

namespace mcc2.CFG;

public class AssGraph
{
    public List<AssNode> Nodes = [];

    public AssGraph(List<Instruction> instructions)
    {
        var partitions = PartitionIntoBasicBlocks(instructions);
        List<AssNode> blocks = [new AssNode.EntryNode([]), new AssNode.ExitNode([])];
        int counter = (int)AssNode.BlockId.START;
        foreach (var part in partitions)
        {
            blocks.Add(new AssNode.BasicBlock(counter++, part, [], []));
        }
        Nodes = blocks;
        AddAllEdges();
    }

    private List<List<Instruction>> PartitionIntoBasicBlocks(List<Instruction> instructions)
    {
        List<List<Instruction>> finishedBlocks = [];
        List<Instruction> currentBlock = [];
        foreach (var inst in instructions)
        {
            if (inst is Instruction.Label)
            {
                if (currentBlock.Count != 0)
                    finishedBlocks.Add(currentBlock);
                currentBlock = [inst];
            }
            else if (inst is Instruction.Jmp or Instruction.JmpCC or Instruction.Ret)
            {
                currentBlock.Add(inst);
                finishedBlocks.Add(currentBlock);
                currentBlock = [];
            }
            else
                currentBlock.Add(inst);
        }

        if (currentBlock.Count != 0)
            finishedBlocks.Add(currentBlock);

        return finishedBlocks;
    }

    public void AddAllEdges()
    {
        AddEdge((int)Node.BlockId.ENTRY, (int)Node.BlockId.START);

        foreach (var graphNode in this.Nodes)
        {
            if (graphNode is AssNode.EntryNode or AssNode.ExitNode)
                continue;

            var node = (AssNode.BasicBlock)graphNode;
            int nextId = (node.Id == this.Nodes.Count - 1) ? (int)Node.BlockId.EXIT : node.Id + 1;

            var inst = node.Instructions[^1];
            switch (inst)
            {
                case Instruction.Ret ret:
                    AddEdge(node.Id, (int)Node.BlockId.EXIT);
                    break;
                case Instruction.Jmp jump:
                    var targetId = GetBlockByLabel(jump.Identifier);
                    AddEdge(node.Id, targetId);
                    break;
                case Instruction.JmpCC jumpIfZero:
                    targetId = GetBlockByLabel(jumpIfZero.Identifier);
                    AddEdge(node.Id, targetId);
                    AddEdge(node.Id, nextId);
                    break;
                default:
                    AddEdge(node.Id, nextId);
                    break;
            }
        }
    }

    public int GetBlockByLabel(string label)
    {
        foreach (var node in this.Nodes)
        {
            if (node is AssNode.BasicBlock basic && basic.Instructions[0] is Instruction.Label l && l.Identifier == label)
                return basic.Id;
        }
        throw new Exception($"Optimizer Error: Couldn't find block with label {label}");
    }

    public void AddEdge(int node1, int node2)
    {
        switch (this.Nodes[node1])
        {
            case AssNode.EntryNode entry:
                entry.Successors.Add(node2);
                break;
            case AssNode.BasicBlock basic:
                basic.Successors.Add(node2);
                break;
        }

        switch (this.Nodes[node2])
        {
            case AssNode.BasicBlock basic:
                basic.Predecessors.Add(node1);
                break;
            case AssNode.ExitNode entry:
                entry.Predecessors.Add(node1);
                break;
        }
    }

    public void RemoveEdge(int node1, int node2)
    {
        switch (FindNode(node1))
        {
            case AssNode.EntryNode entry:
                entry.Successors.Remove(node2);
                break;
            case AssNode.BasicBlock basic:
                basic.Successors.Remove(node2);
                break;
        }

        switch (FindNode(node2))
        {
            case AssNode.BasicBlock basic:
                basic.Predecessors.Remove(node1);
                break;
            case AssNode.ExitNode entry:
                entry.Predecessors.Remove(node1);
                break;
        }
    }

    public AssNode FindNode(int id)
    {
        if (id == (int)Node.BlockId.ENTRY)
            return this.Nodes[(int)Node.BlockId.ENTRY];
        else if (id == (int)Node.BlockId.EXIT)
            return this.Nodes[(int)Node.BlockId.EXIT];
        else
            foreach (var node in this.Nodes)
            {
                if (node is AssNode.BasicBlock basic && basic.Id == id)
                    return basic;
            }
        throw new Exception($"Optimizer Error: Couldn't find block with id {id}");
    }

    public List<Instruction> ToInstructions()
    {
        return Nodes[(int)Node.BlockId.START..].SelectMany(a => ((AssNode.BasicBlock)a).Instructions).ToList();
    }
}