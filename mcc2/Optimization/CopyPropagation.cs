using mcc2.CFG;
using mcc2.TAC;

namespace mcc2;

public class CopyPropagation
{
    private Dictionary<(int blockId, int instIndex), List<Instruction.Copy>> annotatedInstructions = [];
    private Dictionary<int, List<Instruction.Copy>> annotatedBlocks = [];

    public Graph Propagate(Graph graph, List<Val.Variable> aliasedVars)
    {
        FindReachingCopies(graph, aliasedVars);
        foreach (var node in graph.Nodes)
        {
            if (node is Node.BasicBlock basic)
            {
                List<Instruction> newInstructions = [];
                for (int i = 0; i < basic.Instructions.Count; i++)
                {
                    var newInst = RewriteInstruction(basic.Instructions[i], i, basic.Id);
                    if (newInst != null)
                        newInstructions.Add(newInst);
                }
                basic.Instructions.Clear();
                basic.Instructions.AddRange(newInstructions);
            }
        }
        return graph;
    }

    private void FindReachingCopies(Graph graph, List<Val.Variable> aliasedVars)
    {
        List<Instruction.Copy> allCopies = graph.Nodes.FindAll(a => a is Node.BasicBlock)
                                                      .SelectMany(a => ((Node.BasicBlock)a).Instructions)
                                                      .Where(a => a is Instruction.Copy)
                                                      .Select(a => (Instruction.Copy)a)
                                                      .ToList();
        Queue<Node.BasicBlock> workList = [];

        foreach (var node in graph.Nodes)
        {
            if (node is Node.EntryNode or Node.ExitNode)
                continue;

            workList.Enqueue((Node.BasicBlock)node);
            AnnotateBlock(node.Id, allCopies);
        }

        while (workList.Count > 0)
        {
            var block = workList.Dequeue();
            var oldAnnotation = GetBlockAnnotation(block.Id);
            var incomingCopies = Meet(graph, block, allCopies);
            Transfer(block, incomingCopies, aliasedVars);
            if (!oldAnnotation.SequenceEqual(GetBlockAnnotation(block.Id)))
            {
                foreach (var succId in block.Successors)
                {
                    switch (graph.FindNode(succId))
                    {
                        case Node.ExitNode:
                            continue;
                        case Node.EntryNode:
                            throw new Exception("Optimizer Error: Malformed control-flow graph");
                        case Node.BasicBlock basic:
                            var successor = graph.FindNode(succId);
                            if (successor is Node.BasicBlock succ && !workList.Contains(succ))
                                workList.Enqueue(succ);
                            break;
                    }
                }
            }
        }
    }

    private Instruction? RewriteInstruction(Instruction instruction, int instIndex, int blockId)
    {
        List<Instruction.Copy> reachingCopies = GetInstructionAnnotation(blockId, instIndex, instruction);
        switch (instruction)
        {
            case Instruction.Copy copyInst:
                {
                    foreach (var copy in reachingCopies)
                    {
                        if (copy == copyInst || (copy.Src == copyInst.Dst && copy.Dst == copyInst.Src))
                            return null;
                    }
                    Val newSrc = ReplaceOperand(copyInst.Src, reachingCopies);
                    return new Instruction.Copy(newSrc, copyInst.Dst);
                }
            case Instruction.Unary unary:
                {
                    Val newSrc = ReplaceOperand(unary.Src, reachingCopies);
                    return new Instruction.Unary(unary.UnaryOperator, newSrc, unary.Dst);
                }
            case Instruction.Binary binary:
                {
                    Val newSrc1 = ReplaceOperand(binary.Src1, reachingCopies);
                    Val newSrc2 = ReplaceOperand(binary.Src2, reachingCopies);
                    return new Instruction.Binary(binary.Operator, newSrc1, newSrc2, binary.Dst);
                }
            case Instruction.Return ret:
                {
                    if (ret.Value != null)
                    {
                        Val newSrc = ReplaceOperand(ret.Value, reachingCopies);
                        return new Instruction.Return(newSrc);
                    }
                    return ret;
                }
            case Instruction.JumpIfZero jz:
                {
                    Val newSrc = ReplaceOperand(jz.Condition, reachingCopies);
                    return new Instruction.JumpIfZero(newSrc, jz.Target);
                }
            case Instruction.JumpIfNotZero jnz:
                {
                    Val newSrc = ReplaceOperand(jnz.Condition, reachingCopies);
                    return new Instruction.JumpIfNotZero(newSrc, jnz.Target);
                }
            case Instruction.FunctionCall funCall:
                {
                    List<Val> newArgs = [];
                    foreach (var arg in funCall.Arguments)
                    {
                        Val newSrc = ReplaceOperand(arg, reachingCopies);
                        newArgs.Add(newSrc);
                    }
                    return new Instruction.FunctionCall(funCall.Identifier, newArgs, funCall.Dst);
                }
            case Instruction.SignExtend sext:
                {
                    Val newSrc = ReplaceOperand(sext.Src, reachingCopies);
                    return new Instruction.SignExtend(newSrc, sext.Dst);
                }
            case Instruction.Truncate trun:
                {
                    Val newSrc = ReplaceOperand(trun.Src, reachingCopies);
                    return new Instruction.Truncate(newSrc, trun.Dst);
                }
            case Instruction.ZeroExtend zext:
                {
                    Val newSrc = ReplaceOperand(zext.Src, reachingCopies);
                    return new Instruction.ZeroExtend(newSrc, zext.Dst);
                }
            case Instruction.DoubleToInt d2i:
                {
                    Val newSrc = ReplaceOperand(d2i.Src, reachingCopies);
                    return new Instruction.DoubleToInt(newSrc, d2i.Dst);
                }
            case Instruction.DoubleToUInt d2u:
                {
                    Val newSrc = ReplaceOperand(d2u.Src, reachingCopies);
                    return new Instruction.DoubleToUInt(newSrc, d2u.Dst);
                }
            case Instruction.IntToDouble i2d:
                {
                    Val newSrc = ReplaceOperand(i2d.Src, reachingCopies);
                    return new Instruction.IntToDouble(newSrc, i2d.Dst);
                }
            case Instruction.UIntToDouble u2d:
                {
                    Val newSrc = ReplaceOperand(u2d.Src, reachingCopies);
                    return new Instruction.UIntToDouble(newSrc, u2d.Dst);
                }
            case Instruction.Load load:
                {
                    Val newSrc = ReplaceOperand(load.SrcPtr, reachingCopies);
                    return new Instruction.Load(newSrc, load.Dst);
                }
            case Instruction.Store store:
                {
                    Val newSrc = ReplaceOperand(store.Src, reachingCopies);
                    return new Instruction.Store(newSrc, store.DstPtr);
                }
            case Instruction.AddPointer addp:
                {
                    Val newPtr = ReplaceOperand(addp.Pointer, reachingCopies);
                    Val newIndex = ReplaceOperand(addp.Index, reachingCopies);
                    return new Instruction.AddPointer(newPtr, newIndex, addp.Scale, addp.Dst);
                }
            case Instruction.CopyFromOffset cfo:
                {
                    Val newSrc = ReplaceOperand(new Val.Variable(cfo.Src), reachingCopies);
                    return new Instruction.CopyFromOffset(((Val.Variable)newSrc).Name, cfo.Offset, cfo.Dst);
                }
            case Instruction.CopyToOffset cto:
                {
                    Val newSrc = ReplaceOperand(cto.Src, reachingCopies);
                    return new Instruction.CopyToOffset(newSrc, cto.Dst, cto.Offset);
                }
            default:
                return instruction;
        }
    }

    private Val ReplaceOperand(Val op, List<Instruction.Copy> reachingCopies)
    {
        if (op is Val.Constant)
            return op;

        foreach (var copy in reachingCopies)
            if (copy.Dst == op)
                return copy.Src;

        return op;
    }

    private List<Instruction.Copy> GetInstructionAnnotation(int blockId, int instIndex, Instruction instruction)
    {
        if (annotatedInstructions.TryGetValue((blockId, instIndex), out var result))
            return result;
        throw new Exception($"Optimizer Error: Couldn't find annotated instruction");
    }

    private List<Instruction.Copy> Meet(Graph graph, Node.BasicBlock block, List<Instruction.Copy> allCopies)
    {
        List<Instruction.Copy> incomingCopies = new(allCopies);
        foreach (var predId in block.Predecessors)
        {
            switch (graph.FindNode(predId))
            {
                case Node.EntryNode entry:
                    return [];
                case Node.BasicBlock basic:
                    {
                        var predOutCopies = GetBlockAnnotation(predId);
                        incomingCopies = incomingCopies.Intersect(predOutCopies).ToList();
                    }
                    break;
                case Node.ExitNode:
                    throw new Exception("Optimizer Error: Malformed control-flow graph");
            }
        }
        return incomingCopies;
    }

    private List<Instruction.Copy> GetBlockAnnotation(int predId)
    {
        if (annotatedBlocks.TryGetValue(predId, out var result))
            return result;
        throw new Exception($"Optimizer Error: Couldn't find annotated block id {predId}");
    }

    private void Transfer(Node.BasicBlock block, List<Instruction.Copy> initialReachingCopies, List<Val.Variable> aliasedVars)
    {
        List<Instruction.Copy> currentReachingCopies = new(initialReachingCopies);

        for (int i = 0; i < block.Instructions.Count; i++)
        {
            Instruction? inst = block.Instructions[i];
            AnnotateInstruction(block.Id, i, currentReachingCopies);

            switch (inst)
            {
                case Instruction.Copy copyInst:
                    {
                        if (currentReachingCopies.Contains(new Instruction.Copy(copyInst.Dst, copyInst.Src)))
                            continue;

                        KillCopy(currentReachingCopies, copyInst.Dst);

                        if ((ConstantFolding.GetType(copyInst.Src) == ConstantFolding.GetType(copyInst.Dst)) ||
                            (TypeChecker.IsSignedType(ConstantFolding.GetType(copyInst.Src)) == TypeChecker.IsSignedType(ConstantFolding.GetType(copyInst.Dst))))
                            currentReachingCopies.Add(copyInst);
                    }
                    break;
                case Instruction.FunctionCall funCall:
                    {
                        for (int i1 = 0; i1 < currentReachingCopies.Count; i1++)
                        {
                            Instruction.Copy? copy = currentReachingCopies[i1];
                            if (IsAliased(aliasedVars, copy.Src) ||
                                IsAliased(aliasedVars, copy.Dst) ||
                                (copy.Dst != null &&
                                (copy.Src == funCall.Dst ||
                                copy.Dst == funCall.Dst)))
                            {
                                currentReachingCopies.Remove(copy);
                                i1--;
                            }
                        }
                    }
                    break;
                case Instruction.Store store:
                    for (int i1 = 0; i1 < currentReachingCopies.Count; i1++)
                    {
                        Instruction.Copy? copy = currentReachingCopies[i1];
                        if (IsAliased(aliasedVars, copy.Src) ||
                            IsAliased(aliasedVars, copy.Dst))
                        {
                            currentReachingCopies.Remove(copy);
                            i1--;
                        }
                    }
                    break;
                case Instruction.Unary unary:
                    KillCopy(currentReachingCopies, unary.Dst);
                    break;
                case Instruction.Binary binary:
                    KillCopy(currentReachingCopies, binary.Dst);
                    break;
                case Instruction.SignExtend sext:
                    KillCopy(currentReachingCopies, sext.Dst);
                    break;
                case Instruction.Truncate trun:
                    KillCopy(currentReachingCopies, trun.Dst);
                    break;
                case Instruction.ZeroExtend zext:
                    KillCopy(currentReachingCopies, zext.Dst);
                    break;
                case Instruction.DoubleToInt d2i:
                    KillCopy(currentReachingCopies, d2i.Dst);
                    break;
                case Instruction.DoubleToUInt d2u:
                    KillCopy(currentReachingCopies, d2u.Dst);
                    break;
                case Instruction.IntToDouble i2d:
                    KillCopy(currentReachingCopies, i2d.Dst);
                    break;
                case Instruction.UIntToDouble u2d:
                    KillCopy(currentReachingCopies, u2d.Dst);
                    break;
                case Instruction.GetAddress addr:
                    KillCopy(currentReachingCopies, addr.Dst);
                    break;
                case Instruction.Load load:
                    KillCopy(currentReachingCopies, load.Dst);
                    break;
                case Instruction.AddPointer addp:
                    KillCopy(currentReachingCopies, addp.Dst);
                    break;
                case Instruction.CopyFromOffset cfo:
                    KillCopy(currentReachingCopies, cfo.Dst);
                    break;
                case Instruction.CopyToOffset cto:
                    KillCopy(currentReachingCopies, new Val.Variable(cto.Dst));
                    break;
                default:
                    continue;
            }
        }

        AnnotateBlock(block.Id, currentReachingCopies);
    }

    private bool IsAliased(List<Val.Variable> aliasedVars, Val val)
    {
        return aliasedVars.Contains(val) || IsStatic(val);
    }

    private void KillCopy(List<Instruction.Copy> currentReachingCopies, Val Dst)
    {
        for (int i = 0; i < currentReachingCopies.Count; i++)
        {
            Instruction.Copy? copy = currentReachingCopies[i];
            if (copy.Src == Dst || copy.Dst == Dst)
            {
                currentReachingCopies.Remove(copy);
                i--;
            }
        }
    }

    private void AnnotateBlock(int id, List<Instruction.Copy> currentReachingCopies)
    {
        annotatedBlocks[id] = new(currentReachingCopies);
    }

    private bool IsStatic(Val src)
    {
        return src is Val.Variable var && SemanticAnalyzer.SymbolTable.TryGetValue(var.Name, out var entry) && entry.IdentifierAttributes is IdentifierAttributes.Static;
    }

    private void AnnotateInstruction(int blockId, int instIndex, List<Instruction.Copy> reachingCopies)
    {
        annotatedInstructions[(blockId, instIndex)] = new(reachingCopies);
    }
}