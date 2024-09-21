namespace mcc2.Tests;

[TestClass]
public class TestChapter14
{
    private const string chapter = "chapter_14/";

    [TestMethod]
    public void TestInvalidParse()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_parse").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidParse(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTypes()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTypesExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsDeclarationsExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_declarations/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestCompileValidCasts()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/casts").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidComparisons()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/comparisons").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidDeclarators()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/declarators").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/libraries").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidFunctionCalls()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/function_calls").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidDereference()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/dereference").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteValidCasts()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/casts").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidComparisons()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/comparisons").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("global")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("static")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);
    }

    [TestMethod]
    public void TestExecuteValidDeclarators()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/declarators").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidDereference()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/dereference").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidFunctionCalls()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/function_calls").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }
}