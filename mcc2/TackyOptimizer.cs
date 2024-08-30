using mcc2.AST;
using mcc2.CFG;
using mcc2.TAC;

namespace mcc2;

public class TackyOptimizer()
{
    public TACProgam Optimize(TACProgam program, CompilerDriver.Optimizations optimizations)
    {
        for (int i = 0; i < program.Definitions.Count; i++)
        {
            TopLevel? topLevel = program.Definitions[i];
            if (topLevel is TopLevel.Function func)
                program.Definitions[i] = OptimizeFunction(func, optimizations);
        }

        return program;
    }

    private TopLevel.Function OptimizeFunction(TopLevel.Function function, CompilerDriver.Optimizations optimizations)
    {
        return new TopLevel.Function(function.Name, function.Global, function.Parameters, OptimizeFunctionBody(function.Instructions, optimizations));
    }

    private List<Instruction> OptimizeFunctionBody(List<Instruction> instructions, CompilerDriver.Optimizations optimizations)
    {
        if (instructions.Count == 0)
            return instructions;

        while (true)
        {
            var aliasedVars = AddressTakenAnalysis(instructions);

            List<Instruction> postConstantFolding = optimizations.HasFlag(CompilerDriver.Optimizations.FoldConstants) ?
                new ConstantFolding().Fold(instructions) :
                instructions;

            Graph graph = new Graph(postConstantFolding);

            if (optimizations.HasFlag(CompilerDriver.Optimizations.EliminateUnreachableCode))
                graph = new UnreachableCodeElimination().Eliminate(graph);

            if (optimizations.HasFlag(CompilerDriver.Optimizations.PropagateCopies))
                graph = new CopyPropagation().Propagate(graph, aliasedVars);

            if (optimizations.HasFlag(CompilerDriver.Optimizations.EliminateDeadStores))
                graph = new DeadStoreElimination().Eliminate(graph, aliasedVars);

            List<Instruction> optimized = graph.ToInstructions();

            if (AreEqual(optimized, instructions) || optimized.Count == 0)
                return optimized;

            instructions = optimized;
        }
    }

    private List<Val.Variable> AddressTakenAnalysis(List<Instruction> instructions)
    {
        List<Val.Variable> result = [];

        foreach (var inst in instructions)
        {
            if (inst is Instruction.GetAddress getAddr)
                result.Add((Val.Variable)getAddr.Src);
        }
        return result;
    }

    private bool AreEqual(List<Instruction> list1, List<Instruction> list2)
    {
        if (list1.Count != list2.Count)
            return false;

        for (int i = 0; i < list1.Count; i++)
        {
            if (list1[i] is Instruction.FunctionCall f1 && list2[i] is Instruction.FunctionCall f2)
            {
                if (f1.Identifier != f2.Identifier || f1.Dst != f2.Dst || f1.Arguments.Count != f2.Arguments.Count ||
                    !f1.Arguments.SequenceEqual(f2.Arguments))
                    return false;
            }
            else if (list1[i] != list2[i])
                return false;
        }

        return true;
    }
}