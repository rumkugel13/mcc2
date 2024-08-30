using mcc2.TAC;

namespace mcc2.CFG;

public class Graph
{
    public List<Node> Nodes;

    public Graph(List<Node> nodes)
    {
        this.Nodes = nodes;
    }

    public void AddAllEdges()
    {
        AddEdge((int)Node.BlockId.ENTRY, (int)Node.BlockId.START);

        foreach (var graphNode in this.Nodes)
        {
            if (graphNode is Node.EntryNode or Node.ExitNode)
                continue;

            var node = (Node.BasicBlock)graphNode;
            int nextId = (node.Id == this.Nodes.Count - 1) ? (int)Node.BlockId.EXIT : node.Id + 1;

            var inst = node.Instructions[^1];
            switch (inst)
            {
                case Instruction.Return ret:
                    AddEdge(node.Id, (int)Node.BlockId.EXIT);
                    break;
                case Instruction.Jump jump:
                    var targetId = GetBlockByLabel(jump.Target);
                    AddEdge(node.Id, targetId);
                    break;
                case Instruction.JumpIfZero jumpIfZero:
                    targetId = GetBlockByLabel(jumpIfZero.Target);
                    AddEdge(node.Id, targetId);
                    AddEdge(node.Id, nextId);
                    break;
                case Instruction.JumpIfNotZero jumpIfNotZero:
                    targetId = GetBlockByLabel(jumpIfNotZero.Target);
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
            if (node is Node.BasicBlock basic && basic.Instructions[0] is Instruction.Label l && l.Identifier == label)
                return basic.Id;
        }
        throw new Exception($"Optimizer Error: Couldn't find block with label {label}");
    }

    public void AddEdge(int node1, int node2)
    {
        switch (this.Nodes[node1])
        {
            case Node.EntryNode entry:
                entry.Successors.Add(node2);
                break;
            case Node.BasicBlock basic:
                basic.Successors.Add(node2);
                break;
        }

        switch (this.Nodes[node2])
        {
            case Node.BasicBlock basic:
                basic.Predecessors.Add(node1);
                break;
            case Node.ExitNode entry:
                entry.Predecessors.Add(node1);
                break;
        }
    }

    public void RemoveEdge(int node1, int node2)
    {
        switch (FindNode(node1))
        {
            case Node.EntryNode entry:
                entry.Successors.Remove(node2);
                break;
            case Node.BasicBlock basic:
                basic.Successors.Remove(node2);
                break;
        }

        switch (FindNode(node2))
        {
            case Node.BasicBlock basic:
                basic.Predecessors.Remove(node1);
                break;
            case Node.ExitNode entry:
                entry.Predecessors.Remove(node1);
                break;
        }
    }

    public Node FindNode(int id)
    {
        if (id == (int)Node.BlockId.ENTRY)
            return this.Nodes[(int)Node.BlockId.ENTRY];
        else if (id == (int)Node.BlockId.EXIT)
            return this.Nodes[(int)Node.BlockId.EXIT];
        else
            foreach (var node in this.Nodes)
            {
                if (node is Node.BasicBlock basic && basic.Id == id)
                    return basic;
            }
        throw new Exception($"Optimizer Error: Couldn't find block with id {id}");
    }
}