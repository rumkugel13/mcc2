using mcc2.CFG;
using mcc2.TAC;

namespace mcc2;

public class UnreachableCodeElimination
{
    public Graph Eliminate(Graph graph)
    {
        HashSet<int> visited = [];
        FindReachableBlocks(graph, (int)Node.BlockId.ENTRY, visited);
        var toRemove = graph.Nodes.Where(a => a is Node.BasicBlock basic && !visited.Contains(basic.Id)).ToList();
        foreach (var node in toRemove)
        {
            if (node is Node.BasicBlock basic)
                while (basic.Successors.Count > 0)
                {
                    graph.RemoveEdge(basic.Id, basic.Successors[0]);
                }
        }

        graph.Nodes = graph.Nodes.Where(a => (a is Node.BasicBlock basic && visited.Contains(basic.Id)) || a is Node.EntryNode or Node.ExitNode).ToList();

        RemoveRedundantJumps(graph);
        RemoveUselessLabels(graph);

        return graph;
    }

    private void FindReachableBlocks(Graph graph, int start, HashSet<int> visited)
    {
        if (!visited.Add(start) || start == (int)Node.BlockId.EXIT)
            return;

        switch (graph.Nodes[start])
        {
            case Node.EntryNode entry:
                foreach (var successor in entry.Successors)
                    FindReachableBlocks(graph, successor, visited);
                break;
            case Node.BasicBlock basic:
                foreach (var successor in basic.Successors)
                    FindReachableBlocks(graph, successor, visited);
                break;
        }
    }

    private void RemoveRedundantJumps(Graph graph)
    {
        //note: make sure nodes are ordered by id
        int i = (int)Node.BlockId.START;
        while (i < graph.Nodes.Count - 1)
        {
            var block = graph.Nodes[i];
            if (block is Node.BasicBlock basic && basic.Instructions[^1] is Instruction.Jump or Instruction.JumpIfZero or Instruction.JumpIfNotZero)
            {
                bool keepJump = false;
                var defaultSuccessor = graph.Nodes[i + 1];
                foreach (var successorId in basic.Successors)
                {
                    if (successorId != defaultSuccessor.Id)
                    {
                        keepJump = true;
                        break;
                    }
                }

                if (!keepJump)
                    basic.Instructions.RemoveAt(basic.Instructions.Count - 1);
            }

            i += 1;
        }
    }

    private void RemoveUselessLabels(Graph graph)
    {
        //note: make sure nodes are ordered by id
        int i = (int)Node.BlockId.START;
        while (i < graph.Nodes.Count)
        {
            var block = graph.Nodes[i];
            if (block is Node.BasicBlock basic && basic.Instructions.Count > 0 && basic.Instructions[0] is Instruction.Label)
            {
                bool keepLabel = false;
                var defaultPredecessor = graph.Nodes[i - 1];
                foreach (var predecessorId in basic.Predecessors)
                {
                    if (predecessorId != defaultPredecessor.Id && predecessorId != (int)Node.BlockId.ENTRY)
                    {
                        keepLabel = true;
                        break;
                    }
                }

                if (!keepLabel)
                    basic.Instructions.RemoveAt(0);
            }

            i += 1;
        }
    }
}