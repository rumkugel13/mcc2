namespace mcc2.Tests;

[TestClass]
public class TestChapter20
{
    private const string chapter = "chapter_20/";

    [TestMethod]
    public void TestCompileAllTypesNoCoalescing()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "all_types/no_coalescing").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteAllTypesNoCoalescing()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "all_types/no_coalescing").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileAllTypesWithCoalescing()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "all_types/with_coalescing").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteAllTypesWithCoalescing()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "all_types/with_coalescing").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileIntOnlyNoCoalescing()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "int_only/no_coalescing").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteIntOnlyNoCoalescing()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "int_only/no_coalescing").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestCompileIntOnlyWithCoalescing()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "int_only/with_coalescing").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteIntOnlyWithCoalescing()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "int_only/with_coalescing").Where(a => a.EndsWith(".c"));
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
}