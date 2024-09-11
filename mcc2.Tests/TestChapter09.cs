namespace mcc2.Tests;

[TestClass]
public class TestChapter09
{
    [TestMethod]
    public void TestInvalidParse()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_9/invalid_parse").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidParse(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsDeclarations()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_9/invalid_declarations").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsDeclarationsExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_9/invalid_declarations/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsLabelsExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_9/invalid_labels/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTypes()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_9/invalid_types").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTypesExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_9/invalid_types/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestCompileValidArguments()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_9/valid/arguments_in_registers").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_9/valid/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidLibraries()
    {
        // todo: properly use multiple files
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_9/valid/libraries").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidLibrariesNoFunctionCalls()
    {
        // todo: properly use multiple files
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_9/valid/libraries/no_function_calls").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidNoArguments()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_9/valid/no_arguments").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidStackArguments()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_9/valid/stack_arguments").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteValidArguments()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_9/valid/arguments_in_registers").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_9/valid/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_9/valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("addition")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + "chapter_9/valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("many")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + "chapter_9/valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("system")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);
    }

    [TestMethod]
    public void TestExecuteValidLibrariesNoFunctionCalls()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_9/valid/libraries/no_function_calls")
            .Where(a => a.EndsWith(".c") && a.Contains("division")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + "chapter_9/valid/libraries/no_function_calls")
            .Where(a => a.EndsWith(".c") && a.Contains("local")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);
    }

    [TestMethod]
    public void TestExecuteValidNoArguments()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_9/valid/no_arguments").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidStackArguments()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_9/valid/stack_arguments")
            .Where(a => a.EndsWith(".c") && !a.Contains("stack_alignment"));
        TestUtils.TestExecuteValid(files);

        var special = TestUtils.TestsPath + "chapter_9/valid/stack_arguments/stack_alignment.c";
        var extra = TestUtils.TestsPath + "chapter_9/valid/stack_arguments/stack_alignment_check_" + (OperatingSystem.IsMacOS() ? "osx.s" : "linux.s");
        TestUtils.TestExecuteValidSpecial(special, extra);
    }
}