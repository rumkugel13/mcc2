namespace mcc2.Tests;

[TestClass]
public class TestChapter15
{
    private const string chapter = "chapter_15/";

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
    public void TestCompileValidCasts()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/casts").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidAllocation()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/allocation").Where(a => a.EndsWith(".c"));
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
        // todo: properly use multiple files
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/libraries").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidInitialization()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/initialization").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidPointerArithmetic()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/pointer_arithmetic").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidSubscripting()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/subscripting").Where(a => a.EndsWith(".c"));
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
    public void TestExecuteValidAllocation()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/allocation").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("global_array")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("return_pointer")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("set_array")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);
    }

    [TestMethod]
    public void TestExecuteValidDeclarators()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/declarators").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidPointerArithmetic()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/pointer_arithmetic").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidInitialization()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/initialization").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidSubscripting()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/subscripting").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }
}