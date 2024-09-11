namespace mcc2.Tests;

[TestClass]
public class TestChapter19
{
    private const string chapter = "chapter_19/";

    [TestMethod]
    public void TestCompileConstantFoldingAllTypes()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "constant_folding/all_types").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteConstantFoldingAllTypes()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "constant_folding/all_types").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileConstantFoldingAllTypesExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "constant_folding/all_types/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteConstantFoldingAllTypesExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "constant_folding/all_types/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileConstantFoldingIntOnly()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "constant_folding/int_only").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteConstantFoldingIntOnly()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "constant_folding/int_only").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileConstantFoldingIntOnlyExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "constant_folding/int_only/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteConstantFoldingIntOnlyExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "constant_folding/int_only/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileCopyPropagationAllTypes()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "copy_propagation/all_types").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteCopyPropagationAllTypes()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "copy_propagation/all_types").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileCopyPropagationAllTypesDontPropagate()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "copy_propagation/all_types/dont_propagate").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteCopyPropagationAllTypesDontPropagate()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "copy_propagation/all_types/dont_propagate").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileCopyPropagationAllTypesExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "copy_propagation/all_types/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteCopyPropagationAllTypesExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "copy_propagation/all_types/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileCopyPropagationAllTypesExtraCreditDontPropagate()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "copy_propagation/all_types/extra_credit/dont_propagate").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteCopyPropagationAllTypesExtraCreditDontPropagate()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "copy_propagation/all_types/extra_credit/dont_propagate").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileCopyPropagationIntOnly()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "copy_propagation/int_only").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteCopyPropagationIntOnly()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "copy_propagation/int_only").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileCopyPropagationIntOnlyDontPropagate()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "copy_propagation/int_only/dont_propagate").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteCopyPropagationIntOnlyDontPropagate()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "copy_propagation/int_only/dont_propagate").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileCopyPropagationIntOnlyExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "copy_propagation/int_only/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteCopyPropagationIntOnlyExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "copy_propagation/int_only/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileCopyPropagationIntOnlyExtraCreditDontPropagate()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "copy_propagation/int_only/extra_credit/dont_propagate").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteCopyPropagationIntOnlyExtraCreditDontPropagate()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "copy_propagation/int_only/extra_credit/dont_propagate").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileDeadStoreEliminationAllTypes()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "dead_store_elimination/all_types").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteDeadStoreEliminationAllTypes()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "dead_store_elimination/all_types").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileDeadStoreEliminationAllTypesDontElim()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "dead_store_elimination/all_types/dont_elim").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteDeadStoreEliminationAllTypesDontElim()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "dead_store_elimination/all_types/dont_elim").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileDeadStoreEliminationAllTypesExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "dead_store_elimination/all_types/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteDeadStoreEliminationAllTypesExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "dead_store_elimination/all_types/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileDeadStoreEliminationAllTypesExtraCreditDontElim()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "dead_store_elimination/all_types/extra_credit/dont_elim").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteDeadStoreEliminationAllTypesExtraCreditDontElim()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "dead_store_elimination/all_types/extra_credit/dont_elim").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileDeadStoreEliminationIntOnly()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "dead_store_elimination/int_only").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteDeadStoreEliminationIntOnly()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "dead_store_elimination/int_only")
            .Where(a => a.EndsWith(".c") && !a.Contains("not_always_live"));
        TestUtils.TestExecuteValid(files);

        // todo: add test case for static_not_always_live
    }

    [TestMethod]
    public void TestCompileDeadStoreEliminationIntOnlyDontElim()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "dead_store_elimination/int_only/dont_elim").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteDeadStoreEliminationIntOnlyDontElim()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "dead_store_elimination/int_only/dont_elim").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileDeadStoreEliminationIntOnlyExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "dead_store_elimination/int_only/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteDeadStoreEliminationIntOnlyExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "dead_store_elimination/int_only/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileDeadStoreEliminationIntOnlyExtraCreditDontElim()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "dead_store_elimination/int_only/extra_credit/dont_elim").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteDeadStoreEliminationIntOnlyExtraCreditDontElim()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "dead_store_elimination/int_only/extra_credit/dont_elim").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileHelperLibs()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "helper_libs").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteHelperLibs()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "helper_libs").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileUnreachableCodeElimination()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "unreachable_code_elimination").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteUnreachableCodeElimination()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "unreachable_code_elimination")
            .Where(a => a.EndsWith(".c") && !a.Contains("infinite_loop"));
        TestUtils.TestExecuteValid(files);

        // todo: add test case for infinite_loop
    }

    [TestMethod]
    public void TestCompileUnreachableCodeEliminationExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "unreachable_code_elimination/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteUnreachableCodeEliminationExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "unreachable_code_elimination/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileWholePipelineAllTypes()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "whole_pipeline/all_types").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteWholePipelineAllTypes()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "whole_pipeline/all_types").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileWholePipelineAllTypesExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "whole_pipeline/all_types/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteWholePipelineAllTypesExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "whole_pipeline/all_types/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileWholePipelineIntOnly()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "whole_pipeline/int_only").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteWholePipelineIntOnly()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "whole_pipeline/int_only").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileWholePipelineIntOnlyExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "whole_pipeline/int_only/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteWholePipelineIntOnlyExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "whole_pipeline/int_only/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }
}