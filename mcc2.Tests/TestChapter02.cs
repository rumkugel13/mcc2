namespace mcc2.Tests;

[TestClass]
public class TestChapter02
{
    [TestMethod]
    public void TestInvalidParse()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_2/invalid_parse").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidParse(files);
    }

    [TestMethod]
    public void TestCompileValid()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_2/valid").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteValid()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_2/valid").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }
}