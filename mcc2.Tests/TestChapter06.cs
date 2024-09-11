namespace mcc2.Tests;

[TestClass]
public class TestChapter06
{
    [TestMethod]
    public void TestInvalidLexExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_6/invalid_lex/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidLex(files);
    }

    [TestMethod]
    public void TestInvalidParse()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_6/invalid_parse").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidParse(files);
    }

    [TestMethod]
    public void TestInvalidParseExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_6/invalid_parse/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidParse(files);
    }

    [TestMethod]
    public void TestInvalidSemantics()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_6/invalid_semantics").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_6/invalid_semantics/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestCompileValid()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_6/valid").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_6/valid/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteValid()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_6/valid").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_6/valid/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExternal()
    {
        TestUtils.TestExternal(6);
    }

    [TestMethod]
    public void TestExternalExtraCredit()
    {
        TestUtils.TestExternalExtraCredit(6, "--bitwise --compound --increment --goto");
    }
}