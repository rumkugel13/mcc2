using mcc2.CFG;
using mcc2.TAC;

namespace mcc2;

public class DeadStoreElimination
{
    private Dictionary<(int blockId, int instIndex), List<Val.Variable>> annotatedInstructions = [];
    private Dictionary<int, List<Val.Variable>> annotatedBlocks = [];

    public Graph Eliminate(Graph graph, List<Val.Variable> aliasedVars)
    {
        var staticVars = FindAllStaticVariables();
        LivenessAnalysis(graph, staticVars, aliasedVars);
        foreach (var node in graph.Nodes)
        {
            if (node is Node.BasicBlock basic)
            {
                List<Instruction> newInstructions = [];
                for (int i = 0; i < basic.Instructions.Count; i++)
                {
                    if (!IsDeadStore(basic.Instructions[i], basic.Id, i))
                        newInstructions.Add(basic.Instructions[i]);
                }
                basic.Instructions.Clear();
                basic.Instructions.AddRange(newInstructions);
            }
        }
        return graph;
    }

    private List<Val.Variable> FindAllStaticVariables()
    {
        //note: we add too many static variables that may not even be used in the block
        //      we could instead scan every instruction in a block and add them
        List<Val.Variable> result = [];
        foreach (var entry in SemanticAnalyzer.SymbolTable)
        {
            if (entry.Value.IdentifierAttributes is IdentifierAttributes.Static)
                result.Add(new Val.Variable(entry.Key));
        }
        return result;
    }

    private void LivenessAnalysis(Graph graph, List<Val.Variable> allStaticVariables, List<Val.Variable> aliasedVars)
    {
        List<Val.Variable> staticAndAliasedVars = allStaticVariables.Union(aliasedVars).ToList();
        List<Val.Variable> allLive = [];
        Queue<Node.BasicBlock> workList = [];

        foreach (var node in graph.Nodes)
        {
            if (node is Node.EntryNode or Node.ExitNode)
                continue;

            workList.Enqueue((Node.BasicBlock)node);
            AnnotateBlock(node.Id, allLive);
        }

        while (workList.Count > 0)
        {
            var block = workList.Dequeue();
            var oldAnnotation = GetBlockAnnotation(block.Id);
            var incomingCopies = Meet(graph, block, allStaticVariables);
            Transfer(block, incomingCopies, staticAndAliasedVars);
            if (!new HashSet<Val.Variable>(oldAnnotation).SetEquals(GetBlockAnnotation(block.Id)))
            {
                foreach (var predId in block.Predecessors)
                {
                    switch (graph.FindNode(predId))
                    {
                        case Node.ExitNode:
                            throw new Exception("Optimizer Error: Malformed control-flow graph");
                        case Node.EntryNode:
                            continue;
                        case Node.BasicBlock basic:
                            var predecessor = graph.FindNode(predId);
                            if (predecessor is Node.BasicBlock pred && !workList.Contains(pred))
                                workList.Enqueue(pred);
                            break;
                    }
                }
            }
        }
    }

    private bool IsDeadStore(Instruction instruction, int blockId, int instIndex)
    {
        if (instruction is Instruction.FunctionCall or Instruction.Store)
            return false;

        List<Val.Variable> liveVars = GetInstructionAnnotation(blockId, instIndex);
        switch (instruction)
        {
            case Instruction.Binary binary:
                {
                    if (!liveVars.Contains(binary.Dst))
                        return true;
                    break;
                }
            case Instruction.Unary unary:
                {
                    if (!liveVars.Contains(unary.Dst))
                        return true;
                    break;
                }
            case Instruction.Copy copy:
                {
                    if (copy.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.SignExtend se:
                {
                    if (se.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.Truncate trun:
                {
                    if (trun.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.ZeroExtend ze:
                {
                    if (ze.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.DoubleToInt d2i:
                {
                    if (d2i.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.DoubleToUInt d2u:
                {
                    if (d2u.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.IntToDouble i2d:
                {
                    if (i2d.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.UIntToDouble u2d:
                {
                    if (u2d.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.GetAddress getA:
                {
                    if (getA.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.Load load:
                {
                    if (load.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.AddPointer addP:
                {
                    if (addP.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
            case Instruction.CopyToOffset cto:
                {
                    if (!liveVars.Contains(new Val.Variable(cto.Dst)))
                        return true;
                    break;
                }
            case Instruction.CopyFromOffset cfo:
                {
                    if (cfo.Dst is Val.Variable var && !liveVars.Contains(var))
                        return true;
                    break;
                }
        }

        return false;
    }

    private List<Val.Variable> GetInstructionAnnotation(int blockId, int instIndex)
    {
        if (annotatedInstructions.TryGetValue((blockId, instIndex), out var result))
            return result;
        throw new Exception($"Optimizer Error: Couldn't find annotated instruction");
    }

    private void AnnotateBlock(int id, List<Val.Variable> allLive)
    {
        annotatedBlocks[id] = new(allLive);
    }

    private List<Val.Variable> Meet(Graph graph, Node.BasicBlock block, List<Val.Variable> allStaticVariables)
    {
        List<Val.Variable> liveVars = [];
        foreach (var succId in block.Successors)
        {
            switch (graph.FindNode(succId))
            {
                case Node.ExitNode:
                    liveVars = liveVars.Union(allStaticVariables).ToList();
                    break;
                case Node.EntryNode:
                    throw new Exception("Optimizer Error: Malformed control-flow graph");
                case Node.BasicBlock basic:
                    var succLiveVars = GetBlockAnnotation(succId);
                    liveVars = liveVars.Union(succLiveVars).ToList();
                    break;
            }
        }
        return liveVars;
    }

    private List<Val.Variable> GetBlockAnnotation(int succId)
    {
        if (annotatedBlocks.TryGetValue(succId, out var result))
            return result;
        throw new Exception($"Optimizer Error: Couldn't find annotated block id {succId}");
    }

    private void Transfer(Node.BasicBlock block, List<Val.Variable> endLiveVariables, List<Val.Variable> allStaticVariables)
    {
        List<Val.Variable> currentLiveVariables = new(endLiveVariables);

        for (int i = block.Instructions.Count - 1; i >= 0; i--)
        {
            AnnotateInstruction(block.Id, i, currentLiveVariables);

            switch (block.Instructions[i])
            {
                case Instruction.Binary binary:
                    {
                        currentLiveVariables.Remove(binary.Dst);
                        if (binary.Src1 is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        if (binary.Src2 is Val.Variable var2)
                            currentLiveVariables.Add(var2);
                        break;
                    }
                case Instruction.Unary unary:
                    {
                        currentLiveVariables.Remove(unary.Dst);
                        if (unary.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.JumpIfZero jz:
                    {
                        if (jz.Condition is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.JumpIfNotZero jnz:
                    {
                        if (jnz.Condition is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.Copy copy:
                    {
                        if (copy.Dst is Val.Variable var)
                            currentLiveVariables.Remove(var);
                        if (copy.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.FunctionCall funCall:
                    {
                        if (funCall.Dst != null && funCall.Dst is Val.Variable dst)
                            currentLiveVariables.Remove(dst);
                        foreach (var arg in funCall.Arguments)
                        {
                            if (arg is Val.Variable var)
                                currentLiveVariables.Add(var);
                        }
                        currentLiveVariables = currentLiveVariables.Union(allStaticVariables).ToList();
                        break;
                    }
                case Instruction.Return ret:
                    {
                        if (ret.Value != null && ret.Value is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.SignExtend se:
                    {
                        if (se.Dst is Val.Variable var)
                            currentLiveVariables.Remove(var);
                        if (se.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.ZeroExtend ze:
                    {
                        if (ze.Dst is Val.Variable var)
                            currentLiveVariables.Remove(var);
                        if (ze.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.Truncate trun:
                    {
                        if (trun.Dst is Val.Variable var)
                            currentLiveVariables.Remove(var);
                        if (trun.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.DoubleToInt d2i:
                    {
                        if (d2i.Dst is Val.Variable var)
                            currentLiveVariables.Remove(var);
                        if (d2i.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.DoubleToUInt d2u:
                    {
                        if (d2u.Dst is Val.Variable var)
                            currentLiveVariables.Remove(var);
                        if (d2u.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.IntToDouble i2d:
                    {
                        if (i2d.Dst is Val.Variable var)
                            currentLiveVariables.Remove(var);
                        if (i2d.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.UIntToDouble u2d:
                    {
                        if (u2d.Dst is Val.Variable var)
                            currentLiveVariables.Remove(var);
                        if (u2d.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.AddPointer addP:
                    {
                        if (addP.Dst is Val.Variable var)
                            currentLiveVariables.Remove(var);
                        if (addP.Pointer is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        if (addP.Index is Val.Variable var2)
                            currentLiveVariables.Add(var2);
                        break;
                    }
                case Instruction.GetAddress getA:
                    {
                        if (getA.Dst is Val.Variable var)
                            currentLiveVariables.Remove(var);
                        break;
                    }
                case Instruction.Load load:
                    {
                        if (load.Dst is Val.Variable dst)
                            currentLiveVariables.Remove(dst);
                        if (load.SrcPtr is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        currentLiveVariables = currentLiveVariables.Union(allStaticVariables).ToList();
                        break;
                    }
                case Instruction.Store store:
                    {
                        if (store.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        if (store.DstPtr is Val.Variable var2)
                            currentLiveVariables.Add(var2);
                        break;
                    }
                case Instruction.CopyToOffset cto:
                    {
                        if (cto.Src is Val.Variable var1)
                            currentLiveVariables.Add(var1);
                        break;
                    }
                case Instruction.CopyFromOffset cfo:
                    {
                        if (cfo.Dst is Val.Variable dst)
                            currentLiveVariables.Remove(dst);
                        currentLiveVariables.Add(new Val.Variable(cfo.Src));
                        break;
                    }
            }
        }

        AnnotateBlock(block.Id, currentLiveVariables);
    }

    private void AnnotateInstruction(int id, int i, List<Val.Variable> currentLiveVariables)
    {
        annotatedInstructions[(id, i)] = new(currentLiveVariables);
    }
}