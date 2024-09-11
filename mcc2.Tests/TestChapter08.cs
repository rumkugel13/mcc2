namespace mcc2.Tests;

[TestClass]
public class TestChapter08
{
    [TestMethod]
    public void TestInvalidParse()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_8/invalid_parse").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidParse(files);
    }

    [TestMethod]
    public void TestInvalidParseExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_8/invalid_parse/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidParse(files);
    }

    [TestMethod]
    public void TestInvalidSemantics()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_8/invalid_semantics").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_8/invalid_semantics/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestCompileValid()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_8/valid").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_8/valid/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteValid()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_8/valid").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_8/valid/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }
}