namespace mcc2.Tests;

[TestClass]
public class TestChapter17
{
    private const string chapter = "chapter_17/";

    [TestMethod]
    public void TestInvalidParse()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_parse").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidParse(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTypesIncompleteTypes()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/incomplete_types").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsPointerConversions()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/pointer_conversions").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsScalarExpressions()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/scalar_expressions").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsVoid()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/void").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTypesExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_types/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestCompileVoid()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/void").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidSizeof()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/sizeof").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/libraries").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidVoidPointer()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/void_pointer").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteValidVoid()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/void").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidVoidPointer()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/void_pointer").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("pass_alloced")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("sizeof")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("test_for")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);
    }

    [TestMethod]
    public void TestExecuteValidSizeof()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/sizeof").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }
}