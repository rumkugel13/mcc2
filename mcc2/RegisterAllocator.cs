using mcc2.Assembly;
using mcc2.CFG;

namespace mcc2;

using RegName = Operand.RegisterName;

public class RegisterAllocator
{
    public static Dictionary<string, List<RegName>> CalleeSavedRegs = [];

    private class GraphNode(Operand operand, HashSet<Operand> neighbors)
    {
        public Operand Id = operand;
        public HashSet<Operand> Neighbors = neighbors;
        public double SpillCosts = 0.0;
        public int? Color = null;
        public bool Pruned = false;
    }

    private Dictionary<(int blockId, int instIndex), List<Operand>> annotatedInstructions = [];
    private Dictionary<int, List<Operand>> annotatedBlocks = [];
    private string functionName;

    private readonly RegName[] GeneralRegs = [RegName.AX, RegName.BX, RegName.CX, RegName.DX, RegName.DI, RegName.SI,
            RegName.R8, RegName.R9, RegName.R12, RegName.R13, RegName.R14, RegName.R15];
    private readonly RegName[] GeneralCallerSavedRegs = [RegName.AX, RegName.CX, RegName.DX, RegName.DI, RegName.SI,
            RegName.R8, RegName.R9];

    private readonly RegName[] DoubleRegs = [RegName.XMM0, RegName.XMM1, RegName.XMM2, RegName.XMM3, RegName.XMM4, RegName.XMM5,
            RegName.XMM6, RegName.XMM7, RegName.XMM8, RegName.XMM9, RegName.XMM10, RegName.XMM11, RegName.XMM12, RegName.XMM13];
    private readonly RegName[] DoubleCallerSavedRegs = [RegName.XMM0, RegName.XMM1, RegName.XMM2, RegName.XMM3, RegName.XMM4, RegName.XMM5,
            RegName.XMM6, RegName.XMM7, RegName.XMM8, RegName.XMM9, RegName.XMM10, RegName.XMM11, RegName.XMM12, RegName.XMM13];

    public RegisterAllocator(string name)
    {
        this.functionName = name;
    }

    public List<Instruction> Allocate(List<Instruction> instructions, List<TAC.Val.Variable> aliasedVars)
    {
        instructions = AllocateRegs(instructions, aliasedVars, false);
        annotatedInstructions.Clear();
        annotatedBlocks.Clear();
        instructions = AllocateRegs(instructions, aliasedVars, true);
        return instructions;
    }

    private List<Instruction> AllocateRegs(List<Instruction> instructions, List<TAC.Val.Variable> aliasedVars, bool useDoubleRegs)
    {
        List<GraphNode> interferenceGraph;
        while (true)
        {
            interferenceGraph = BuildGraph(instructions, aliasedVars, useDoubleRegs); 
            DisjointSets coalescedRegs = Coalesce(interferenceGraph, instructions, useDoubleRegs);
            if (coalescedRegs.NothingWasCoalesced())
                break;
            instructions = RewriteCoalesced(instructions, coalescedRegs);
        }
        AddSpillCosts(interferenceGraph, instructions);
        ColorGraph(interferenceGraph, useDoubleRegs);
        var registerMap = CreateRegisterMap(interferenceGraph, useDoubleRegs);
        ReplacePseudoregs(instructions, registerMap);
        return instructions;
    }

    private List<Instruction> RewriteCoalesced(List<Instruction> instructions, DisjointSets coalescedRegs)
    {
        List<Instruction> result = [];
        for (int i = 0; i < instructions.Count; i++)
        {
            Instruction? inst = instructions[i];
            switch (inst)
            {
                case Instruction.Mov Mov:
                    var src = coalescedRegs.Find(Mov.Src);
                    var dst = coalescedRegs.Find(Mov.Dst);
                    if (src != dst)
                        result.Add(new Instruction.Mov(Mov.Type, src, dst));
                    break;
                case Instruction.Movsx Movsx:
                    result.Add(new Instruction.Movsx(Movsx.SrcType, Movsx.DstType, coalescedRegs.Find(Movsx.Src), coalescedRegs.Find(Movsx.Dst)));
                    break;
                case Instruction.MovZeroExtend MovZeroExtend:
                    result.Add(new Instruction.MovZeroExtend(MovZeroExtend.SrcType, MovZeroExtend.DstType, coalescedRegs.Find(MovZeroExtend.Src), coalescedRegs.Find(MovZeroExtend.Dst)));
                    break;
                case Instruction.Lea Lea:
                    result.Add(new Instruction.Lea(coalescedRegs.Find(Lea.Src), coalescedRegs.Find(Lea.Dst)));
                    break;
                case Instruction.Cvttsd2si Cvttsd2si:
                    result.Add(new Instruction.Cvttsd2si(Cvttsd2si.DstType, coalescedRegs.Find(Cvttsd2si.Src), coalescedRegs.Find(Cvttsd2si.Dst)));
                    break;
                case Instruction.Cvtsi2sd Cvtsi2sd:
                    result.Add(new Instruction.Cvtsi2sd(Cvtsi2sd.SrcType, coalescedRegs.Find(Cvtsi2sd.Src), coalescedRegs.Find(Cvtsi2sd.Dst)));
                    break;
                case Instruction.Unary Unary:
                    result.Add(new Instruction.Unary(Unary.Operator, Unary.Type, coalescedRegs.Find(Unary.Operand)));
                    break;
                case Instruction.Binary Binary:
                    result.Add(new Instruction.Binary(Binary.Operator, Binary.Type, coalescedRegs.Find(Binary.Src), coalescedRegs.Find(Binary.Dst)));
                    break;
                case Instruction.Cmp Cmp:
                    result.Add(new Instruction.Cmp(Cmp.Type, coalescedRegs.Find(Cmp.OperandA), coalescedRegs.Find(Cmp.OperandB)));
                    break;
                case Instruction.Idiv Idiv:
                    result.Add(new Instruction.Idiv(Idiv.Type, coalescedRegs.Find(Idiv.Operand)));
                    break;
                case Instruction.Div Div:
                    result.Add(new Instruction.Div(Div.Type, coalescedRegs.Find(Div.Operand)));
                    break;
                case Instruction.SetCC SetCC:
                    result.Add(new Instruction.SetCC(SetCC.Condition, coalescedRegs.Find(SetCC.Operand)));
                    break;
                case Instruction.Push Push:
                    result.Add(new Instruction.Push(coalescedRegs.Find(Push.Operand)));
                    break;
                default:
                    result.Add(inst);
                    break;
            }
        }
        return result;
    }

    private DisjointSets Coalesce(List<GraphNode> interferenceGraph, List<Instruction> instructions, bool useDoubleRegs)
    {
        DisjointSets coalescedRegs = new DisjointSets();

        foreach (var inst in instructions)
        {
            if (inst is Instruction.Mov mov)
            {
                var src = coalescedRegs.Find(mov.Src);
                var dst = coalescedRegs.Find(mov.Dst);

                if (interferenceGraph.Any(a => a.Id == src)
                    && interferenceGraph.Any(b => b.Id == dst)
                    && src != dst
                    && !interferenceGraph.Find(c => c.Id == src)!.Neighbors.Contains(dst)
                    && ConservativeCoalesce(interferenceGraph, src, dst, useDoubleRegs))
                {
                    (Operand toKeep, Operand toMerge) = src is Operand.Reg ? (src, dst) : (dst, src);
                    coalescedRegs.Union(toMerge, toKeep);
                    UpdateGraph(interferenceGraph, toMerge, toKeep);
                }
            }
        }

        return coalescedRegs;
    }

    private bool ConservativeCoalesce(List<GraphNode> interferenceGraph, Operand src, Operand dst, bool useDoubleRegs)
    {
        if (BriggsTest(interferenceGraph, src, dst, useDoubleRegs))
            return true;
        if (src is Operand.Reg)
            return GeorgeTest(interferenceGraph, src, dst, useDoubleRegs);
        if (dst is Operand.Reg)
            return GeorgeTest(interferenceGraph, dst, src, useDoubleRegs);
        return false;
    }

    private bool GeorgeTest(List<GraphNode> interferenceGraph, Operand hardReg, Operand pseudoReg, bool useDoubleRegs)
    {
        var pseudoNode = interferenceGraph.Find(node => node.Id == pseudoReg)!;
        int k = useDoubleRegs ? DoubleRegs.Length : GeneralRegs.Length;
        foreach (var neighbor in pseudoNode.Neighbors)
        {
            var neighborNode = interferenceGraph.Find(node => node.Id == neighbor)!;
            if (neighborNode.Neighbors.Contains(hardReg))
                continue;

            if (neighborNode.Neighbors.Count < k)
                continue;

            return false;
        }
        return true;
    }

    private bool BriggsTest(List<GraphNode> interferenceGraph, Operand src, Operand dst, bool useDoubleRegs)
    {
        int significandNeighbors = 0;

        var xNode = interferenceGraph.Find(node => node.Id == src)!;
        var yNode = interferenceGraph.Find(node => node.Id == dst)!;

        var combinedNeighbors = xNode.Neighbors.Union(yNode.Neighbors);
        int k = useDoubleRegs ? DoubleRegs.Length : GeneralRegs.Length;
        foreach (var n in combinedNeighbors)
        {
            var neighborNode = interferenceGraph.Find(node => node.Id == n)!;
            var degree = neighborNode.Neighbors.Count;
            if (xNode.Neighbors.Contains(n) && yNode.Neighbors.Contains(n))
                degree -= 1;
            if (degree >= k)
                significandNeighbors += 1;
        }

        return significandNeighbors < k;
    }

    private void UpdateGraph(List<GraphNode> interferenceGraph, Operand x, Operand y)
    {
        var nodeToRemove = interferenceGraph.Find(a => a.Id == x)!;
        foreach (var neighbor in nodeToRemove.Neighbors)
        {
            AddEdge(interferenceGraph, y, neighbor);
            RemoveEdge(interferenceGraph, x, neighbor);
        }
        interferenceGraph.Remove(nodeToRemove);
    }

    private void ReplacePseudoregs(List<Instruction> instructions, Dictionary<Operand.Pseudo, RegName> registerMap)
    {
        var Replace = (Operand op) => {
            return (op is Operand.Pseudo pseudoOp && registerMap.TryGetValue(pseudoOp, out RegName reg)) ? new Operand.Reg(reg) : op;
        };

        for (int i = 0; i < instructions.Count; i++)
        {
            Instruction? inst = instructions[i];
            switch (inst)
            {
                case Instruction.Mov Mov:
                    instructions[i] = new Instruction.Mov(Mov.Type, Replace(Mov.Src), Replace(Mov.Dst));
                    break;
                case Instruction.Movsx Movsx:
                    instructions[i] = new Instruction.Movsx(Movsx.SrcType, Movsx.DstType, Replace(Movsx.Src), Replace(Movsx.Dst));
                    break;
                case Instruction.MovZeroExtend MovZeroExtend:
                    instructions[i] = new Instruction.MovZeroExtend(MovZeroExtend.SrcType, MovZeroExtend.DstType, Replace(MovZeroExtend.Src), Replace(MovZeroExtend.Dst));
                    break;
                case Instruction.Lea Lea:
                    instructions[i] = new Instruction.Lea(Replace(Lea.Src), Replace(Lea.Dst));
                    break;
                case Instruction.Cvttsd2si Cvttsd2si:
                    instructions[i] = new Instruction.Cvttsd2si(Cvttsd2si.DstType, Replace(Cvttsd2si.Src), Replace(Cvttsd2si.Dst));
                    break;
                case Instruction.Cvtsi2sd Cvtsi2sd:
                    instructions[i] = new Instruction.Cvtsi2sd(Cvtsi2sd.SrcType, Replace(Cvtsi2sd.Src), Replace(Cvtsi2sd.Dst));
                    break;
                case Instruction.Unary Unary:
                    instructions[i] = new Instruction.Unary(Unary.Operator, Unary.Type, Replace(Unary.Operand));
                    break;
                case Instruction.Binary Binary:
                    instructions[i] = new Instruction.Binary(Binary.Operator, Binary.Type, Replace(Binary.Src), Replace(Binary.Dst));
                    break;
                case Instruction.Cmp Cmp:
                    instructions[i] = new Instruction.Cmp(Cmp.Type, Replace(Cmp.OperandA), Replace(Cmp.OperandB));
                    break;
                case Instruction.Idiv Idiv:
                    instructions[i] = new Instruction.Idiv(Idiv.Type, Replace(Idiv.Operand));
                    break;
                case Instruction.Div Div:
                    instructions[i] = new Instruction.Div(Div.Type, Replace(Div.Operand));
                    break;
                case Instruction.SetCC SetCC:
                    instructions[i] = new Instruction.SetCC(SetCC.Condition, Replace(SetCC.Operand));
                    break;
                case Instruction.Push Push:
                    instructions[i] = new Instruction.Push(Replace(Push.Operand));
                    break;
            }
        }

        instructions.RemoveAll(a => a is Instruction.Mov mov && mov.Src == mov.Dst);
    }

    private Dictionary<Operand.Pseudo, RegName> CreateRegisterMap(List<GraphNode> coloredGraph, bool useDoubleRegs)
    {
        Dictionary<int, RegName> colorMap = [];
        foreach (var node in coloredGraph)
        {
            if (node.Id is Operand.Reg reg)
                colorMap.Add(node.Color!.Value, reg.Register);
        }

        var callerSavedRegs = useDoubleRegs ? DoubleCallerSavedRegs : GeneralCallerSavedRegs;
        Dictionary<Operand.Pseudo, RegName> registerMap = [];
        HashSet<RegName> calleeSavedRegs = [];
        foreach (var node in coloredGraph)
        {
            if (node.Id is Operand.Pseudo pseudo)
            {
                if (node.Color != null)
                {
                    var hardReg = colorMap[node.Color.Value];
                    registerMap.Add(pseudo, hardReg);
                    if (!callerSavedRegs.Contains(hardReg))
                        calleeSavedRegs.Add(hardReg);
                }
            }
        }
        if (!CalleeSavedRegs.ContainsKey(functionName))
            CalleeSavedRegs[functionName] = [];
        CalleeSavedRegs[functionName].AddRange(calleeSavedRegs);
        return registerMap;
    }

    private void ColorGraph(List<GraphNode> interferenceGraph, bool useDoubleRegs)
    {
        var remaining = interferenceGraph.Where(a => a.Pruned == false).ToList();
        if (remaining.Count == 0)
            return;

        GraphNode? chosenNode = null;
        int k = useDoubleRegs ? DoubleRegs.Length : GeneralRegs.Length;

        foreach (var node in remaining)
        {
            var degree = node.Neighbors.Where(a => interferenceGraph.Find(b => b.Id == a)!.Pruned == false).Count();
            if (degree < k)
            {
                chosenNode = node;
                break;
            }
        }

        if (chosenNode == null)
        {
            double bestSpillMetric = double.PositiveInfinity;
            foreach (var node in remaining)
            {
                var degree = node.Neighbors.Where(a => interferenceGraph.Find(b => b.Id == a)!.Pruned == false).Count();
                var spillMetric = node.SpillCosts / degree;
                if (spillMetric < bestSpillMetric)
                {
                    chosenNode = node;
                    bestSpillMetric = spillMetric;
                }
            }
        }

        chosenNode!.Pruned = true;

        ColorGraph(interferenceGraph, useDoubleRegs);

        var colors = Enumerable.Range(1, k).ToList();
        System.Diagnostics.Debug.Assert(colors.Count == k);

        foreach (var neighborId in chosenNode.Neighbors)
        {
            var neighbor = interferenceGraph.Find(b => b.Id == neighborId)!;
            if (neighbor.Color != null)
                colors.Remove(neighbor.Color.Value);
        }

        if (colors.Count != 0)
        {
            var callerSavedRegs = useDoubleRegs ? DoubleCallerSavedRegs : GeneralCallerSavedRegs;
            if (chosenNode.Id is Operand.Reg reg && !callerSavedRegs.Contains(reg.Register))
                chosenNode.Color = colors.Max();
            else
                chosenNode.Color = colors.Min();

            chosenNode.Pruned = false;
        }
    }

    private void AddSpillCosts(List<GraphNode> interferenceGraph, List<Instruction> instructions)
    {
        var ops = GetOperands(instructions);
        var pseudos = ops.Where(a => a is Operand.Pseudo).Select(b => (Operand.Pseudo)b).ToList();
        Dictionary<Operand.Pseudo, double> spillMap = [];
        pseudos.ForEach(a =>
        {
            spillMap[a] = spillMap.TryGetValue(a, out double spill) ? spill + 1 : 1;
        });
        foreach (GraphNode v in interferenceGraph)
        {
            if (v.Id is Operand.Pseudo p)
            {
                v.SpillCosts = spillMap[p];
            }
        }
    }

    private List<GraphNode> BuildGraph(List<Instruction> instructions, List<TAC.Val.Variable> aliasedVars, bool useDoubleRegs)
    {
        List<GraphNode> interferenceGraph = MakeBaseGraph(useDoubleRegs ? DoubleRegs : GeneralRegs);
        AddPseudoregisters(interferenceGraph, instructions, aliasedVars, useDoubleRegs);
        var graph = new AssGraph(instructions);
        AnalyzeLiveness(graph, useDoubleRegs);
        AddEdges(graph, interferenceGraph, useDoubleRegs);
        return interferenceGraph;
    }

    private void AddEdges(AssGraph graph, List<GraphNode> interferenceGraph, bool useDoubleRegs)
    {
        foreach (var node in graph.Nodes)
        {
            if (node is AssNode.EntryNode or AssNode.ExitNode)
                continue;

            if (node is AssNode.BasicBlock basic)
            {
                for (int i = 0; i < basic.Instructions.Count; i++)
                {
                    Instruction? inst = basic.Instructions[i];
                    (List<Operand> used, List<Operand> updatedRegs) = FindUsedAndUpdated(inst, useDoubleRegs);
                    var liveRegisters = GetInstructionAnnotation(basic.Id, i);

                    foreach (var live in liveRegisters)
                    {
                        if (inst is Instruction.Mov mov && live == mov.Src)
                            continue;

                        foreach (var updated in updatedRegs)
                        {
                            if (interferenceGraph.Any(a => a.Id == live) && interferenceGraph.Any(a => a.Id == updated) && live != updated)
                                AddEdge(interferenceGraph, live, updated);
                        }
                    }
                }
            }
        }
    }

    private void AddEdge(List<GraphNode> interferenceGraph, Operand opA, Operand opB)
    {
        interferenceGraph.Find(a => a.Id == opA)!.Neighbors.Add(opB);
        interferenceGraph.Find(a => a.Id == opB)!.Neighbors.Add(opA);
    }

    private void RemoveEdge(List<GraphNode> interferenceGraph, Operand a, Operand b)
    {
        interferenceGraph.Find(node => node.Id == a)!.Neighbors.Remove(b);
        interferenceGraph.Find(node => node.Id == b)!.Neighbors.Remove(a);
    }

    private void AnalyzeLiveness(AssGraph graph, bool useDoubleRegs)
    {
        List<Operand> allLive = [];
        Queue<AssNode.BasicBlock> workList = [];

        foreach (var node in graph.Nodes)
        {
            if (node is AssNode.EntryNode or AssNode.ExitNode)
                continue;

            workList.Enqueue((AssNode.BasicBlock)node);
            AnnotateBlock(node.Id, allLive);
        }

        while (workList.Count > 0)
        {
            var block = workList.Dequeue();
            var oldAnnotation = GetBlockAnnotation(block.Id);
            var incomingCopies = Meet(graph, block, useDoubleRegs);
            Transfer(block, incomingCopies, useDoubleRegs);
            if (!new HashSet<Operand>(oldAnnotation).SetEquals(GetBlockAnnotation(block.Id)))
            {
                foreach (var predId in block.Predecessors)
                {
                    switch (graph.FindNode(predId))
                    {
                        case AssNode.ExitNode:
                            throw new Exception("Optimizer Error: Malformed control-flow graph");
                        case AssNode.EntryNode:
                            continue;
                        case AssNode.BasicBlock basic:
                            var predecessor = graph.FindNode(predId);
                            if (predecessor is AssNode.BasicBlock pred && !workList.Contains(pred))
                                workList.Enqueue(pred);
                            break;
                    }
                }
            }
        }
    }

    private void Transfer(AssNode.BasicBlock block, List<Operand> endLiveRegisters, bool useDoubleRegs)
    {
        HashSet<Operand> currentLiveRegisters = new(endLiveRegisters);

        for (int i = block.Instructions.Count - 1; i >= 0; i--)
        {
            AnnotateInstruction(block.Id, i, currentLiveRegisters.ToList());
            (List<Operand> used, List<Operand> updated) = FindUsedAndUpdated(block.Instructions[i], useDoubleRegs);

            foreach (var op in updated)
            {
                if (op is Operand.Reg or Operand.Pseudo)
                    currentLiveRegisters.Remove(op);
            }

            foreach (var op in used)
            {
                if (op is Operand.Reg or Operand.Pseudo)
                    currentLiveRegisters.Add(op);
            }
        }

        AnnotateBlock(block.Id, currentLiveRegisters.ToList());
    }

    private (List<Operand> used, List<Operand> updated) FindUsedAndUpdated(Instruction instruction, bool useDoubleRegs)
    {
        List<Operand> used = [];
        List<Operand> updated = [];
        switch (instruction)
        {
            case Instruction.Mov mov:
                used = [mov.Src];
                updated = [mov.Dst];
                break;
            case Instruction.Movsx movsx:
                used = [movsx.Src];
                updated = [movsx.Dst];
                break;
            case Instruction.MovZeroExtend movze:
                used = [movze.Src];
                updated = [movze.Dst];
                break;
            case Instruction.Cvtsi2sd Cvtsi2sd:
                used = [Cvtsi2sd.Src];
                updated = [Cvtsi2sd.Dst];
                break;
            case Instruction.Cvttsd2si Cvttsd2si:
                used = [Cvttsd2si.Src];
                updated = [Cvttsd2si.Dst];
                break;
            case Instruction.Binary binary:
                used = [binary.Src, binary.Dst];
                updated = [binary.Dst];
                break;
            case Instruction.Unary unary:
                used = [unary.Operand];
                updated = [unary.Operand];
                break;
            case Instruction.Cmp cmp:
                used = [cmp.OperandA, cmp.OperandB];
                updated = [];
                break;
            case Instruction.SetCC setCC:
                used = [];
                updated = [setCC.Operand];
                break;
            case Instruction.Push push:
                used = [push.Operand];
                updated = [];
                break;
            case Instruction.Idiv idiv:
                used = [idiv.Operand, new Operand.Reg(RegName.AX), new Operand.Reg(RegName.DX)];
                updated = [new Operand.Reg(RegName.AX), new Operand.Reg(RegName.DX)];
                break;
            case Instruction.Div div:
                used = [div.Operand, new Operand.Reg(RegName.AX), new Operand.Reg(RegName.DX)];
                updated = [new Operand.Reg(RegName.AX), new Operand.Reg(RegName.DX)];
                break;
            case Instruction.Cdq cdq:
                used = [new Operand.Reg(RegName.AX)];
                updated = [new Operand.Reg(RegName.DX)];
                break;
            case Instruction.Call call:
                var hardRegs = useDoubleRegs ? DoubleRegs : GeneralRegs;
                used = ((AsmSymbolTableEntry.FunctionEntry)AssemblyGenerator.AsmSymbolTable[call.Identifier]).ParamRegisters
                    .Where(a => hardRegs.Contains(a))
                    .Select(a => new Operand.Reg(a))
                    .ToList<Operand>();

                var callerSavedRegs = useDoubleRegs ? DoubleCallerSavedRegs : GeneralCallerSavedRegs;
                updated = callerSavedRegs.Select(a => new Operand.Reg(a)).ToList<Operand>();
                break;
            case Instruction.Lea lea:
                used = [lea.Src];
                updated = [lea.Dst];
                break;
            default:
                used = [];
                updated = [];
                break;
        }
        
        var regsRead1 = used.SelectMany<Operand, Operand>(a => a switch
        {
            Operand.Pseudo or Operand.Reg => [a],
            Operand.Memory mem => [new Operand.Reg(mem.Register)],
            Operand.Indexed ind => [new Operand.Reg(ind.Base), new Operand.Reg(ind.Index)],
            _ => []
        });

        var regsRead2 = updated.SelectMany<Operand, Operand>(a => a switch
        {
            Operand.Pseudo or Operand.Reg => [],
            Operand.Memory mem => [new Operand.Reg(mem.Register)],
            Operand.Indexed ind => [new Operand.Reg(ind.Base), new Operand.Reg(ind.Index)],
            _ => []
        });

        var regsWritten = updated.SelectMany<Operand, Operand>(a => a switch
        {
            Operand.Pseudo or Operand.Reg => [a],
            Operand.Memory mem => [],
            Operand.Indexed ind => [],
            _ => []
        });

        return (regsRead1.Concat(regsRead2).ToList(), regsWritten.ToList());
    }

    private List<Operand> Meet(AssGraph graph, AssNode.BasicBlock block, bool useDoubleRegs)
    {
        List<Operand> liveRegs = [];
        foreach (var succId in block.Successors)
        {
            switch (graph.FindNode(succId))
            {
                case AssNode.ExitNode:
                    var hardRegs = useDoubleRegs ? DoubleRegs : GeneralRegs;
                    liveRegs.AddRange(((AsmSymbolTableEntry.FunctionEntry)AssemblyGenerator.AsmSymbolTable[functionName]).ReturnRegisters
                        .Where(a => hardRegs.Contains(a))
                        .Select(a => new Operand.Reg(a))
                        .ToList());
                    break;
                case AssNode.EntryNode:
                    throw new Exception("Optimizer Error: Malformed control-flow graph");
                case AssNode.BasicBlock basic:
                    var succLiveVars = GetBlockAnnotation(succId);
                    liveRegs = liveRegs.Union(succLiveVars).ToList();
                    break;
            }
        }
        return liveRegs;
    }

    private void AnnotateInstruction(int id, int i, List<Operand> currentLiveRegisters)
    {
        annotatedInstructions[(id, i)] = new(currentLiveRegisters);
    }

    private List<Operand> GetInstructionAnnotation(int blockId, int instIndex)
    {
        if (annotatedInstructions.TryGetValue((blockId, instIndex), out var result))
            return result;
        throw new Exception($"Optimizer Error: Couldn't find annotated instruction");
    }

    private void AnnotateBlock(int id, List<Operand> allLive)
    {
        annotatedBlocks[id] = new(allLive);
    }

    private List<Operand> GetBlockAnnotation(int succId)
    {
        if (annotatedBlocks.TryGetValue(succId, out var result))
            return result;
        throw new Exception($"Optimizer Error: Couldn't find annotated block id {succId}");
    }

    private void AddPseudoregisters(List<GraphNode> interferenceGraph, List<Instruction> instructions, List<TAC.Val.Variable> aliasedVars, bool useDoubleRegs)
    {
        var operands = GetOperands(instructions).Distinct();
        foreach (var op in operands)
        {
            if (op is Operand.Pseudo pseudo &&
                ((((AsmSymbolTableEntry.ObjectEntry)AssemblyGenerator.AsmSymbolTable[pseudo.Identifier]).AssemblyType is AssemblyType.Double) == useDoubleRegs) &&
                !(((AsmSymbolTableEntry.ObjectEntry)AssemblyGenerator.AsmSymbolTable[pseudo.Identifier]).IsStatic || aliasedVars.Any(a => a.Name == pseudo.Identifier)))
                interferenceGraph.Add(new GraphNode(op, []));
        }
    }

    private List<Operand> GetOperands(List<Instruction> instructions)
    {
        List<Operand> operands = [];
        foreach (Instruction inst in instructions)
        {
            switch (inst)
            {
                case Instruction.Mov Mov:
                    operands.Add(Mov.Src);
                    operands.Add(Mov.Dst);
                    break;
                case Instruction.Movsx Movsx:
                    operands.Add(Movsx.Src);
                    operands.Add(Movsx.Dst);
                    break;
                case Instruction.MovZeroExtend MovZeroExtend:
                    operands.Add(MovZeroExtend.Src);
                    operands.Add(MovZeroExtend.Dst);
                    break;
                case Instruction.Lea Lea:
                    operands.Add(Lea.Src);
                    operands.Add(Lea.Dst);
                    break;
                case Instruction.Cvttsd2si Cvttsd2si:
                    operands.Add(Cvttsd2si.Src);
                    operands.Add(Cvttsd2si.Dst);
                    break;
                case Instruction.Cvtsi2sd Cvtsi2sd:
                    operands.Add(Cvtsi2sd.Src);
                    operands.Add(Cvtsi2sd.Dst);
                    break;
                case Instruction.Unary Unary:
                    operands.Add(Unary.Operand);
                    break;
                case Instruction.Binary Binary:
                    operands.Add(Binary.Src);
                    operands.Add(Binary.Dst);
                    break;
                case Instruction.Cmp Cmp:
                    operands.Add(Cmp.OperandA);
                    operands.Add(Cmp.OperandB);
                    break;
                case Instruction.Idiv Idiv:
                    operands.Add(Idiv.Operand);
                    break;
                case Instruction.Div Div:
                    operands.Add(Div.Operand);
                    break;
                case Instruction.SetCC SetCC:
                    operands.Add(SetCC.Operand);
                    break;
                case Instruction.Push Push:
                    operands.Add(Push.Operand);
                    break;
            }
        }

        return operands.ToList();
    }

    private List<GraphNode> MakeBaseGraph(RegName[] registerNames)
    {
        List<GraphNode> graphNodes = [];
        foreach (var reg in registerNames)
        {
            graphNodes.Add(new GraphNode(new Operand.Reg(reg), []) { SpillCosts = double.PositiveInfinity });
        }
        graphNodes.ForEach(a => a.Neighbors.UnionWith(graphNodes.Where(b => b.Id != a.Id).Select(b => b.Id)));
        return graphNodes;
    }

    private class DisjointSets
    {
        private  Dictionary<Operand, Operand> regMap;

        public DisjointSets()
        {
            regMap = [];
        }

        public void Union(Operand x, Operand y)
        {
            regMap.Add(x, y);
        }

        public Operand Find(Operand r)
        {
            if (regMap.TryGetValue(r, out Operand? result))
            {
                return Find(result);
            }
            return r;
        }

        public bool NothingWasCoalesced()
        {
            return regMap.Count == 0;
        }
    }
}