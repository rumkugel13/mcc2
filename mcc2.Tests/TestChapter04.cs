namespace mcc2.Tests;

[TestClass]
public class TestChapter04
{
    [TestMethod]
    public void TestInvalidParse()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_4/invalid_parse").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidParse(files);
    }

    [TestMethod]
    public void TestCompileValid()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_4/valid").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_4/valid/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteValid()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_4/valid").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_4/valid/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }
}