namespace mcc2.Tests;

[TestClass]
public class TestChapter05
{
    [TestMethod]
    public void TestInvalidParse()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_5/invalid_parse").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidParse(files);
    }

    [TestMethod]
    public void TestInvalidParseExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_5/invalid_parse/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidParse(files);
    }

    [TestMethod]
    public void TestInvalidSemantics()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_5/invalid_semantics").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_5/invalid_semantics/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsExtraCreditBitwise()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_5/invalid_semantics/extra_credit")
            .Where(a => a.EndsWith(".c") && a.Contains("bitwise"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsExtraCreditCompound()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_5/invalid_semantics/extra_credit")
            .Where(a => a.EndsWith(".c") && a.Contains("compound"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsExtraCreditPrefix()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_5/invalid_semantics/extra_credit")
            .Where(a => a.EndsWith(".c") && a.Contains("prefix"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsExtraCreditPostfix()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_5/invalid_semantics/extra_credit")
            .Where(a => a.EndsWith(".c") && a.Contains("postfix"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestCompileValid()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_5/valid").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_5/valid/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidExtraCreditBitwise()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_5/valid/extra_credit")
            .Where(a => a.EndsWith(".c") && a.Contains("bitwise") && !a.Contains("compound"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidExtraCreditCompound()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_5/valid/extra_credit")
            .Where(a => a.EndsWith(".c") && a.Contains("compound"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidExtraCreditIncrDecr()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_5/valid/extra_credit")
            .Where(a => a.EndsWith(".c") && (a.Contains("incr") || a.Contains("decr") || a.Contains("prefix") || a.Contains("postfix")));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteValid()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_5/valid").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_5/valid/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidExtraCreditBitwise()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_5/valid/extra_credit")
            .Where(a => a.EndsWith(".c") && a.Contains("bitwise") && !a.Contains("compound"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidExtraCreditCompound()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_5/valid/extra_credit")
            .Where(a => a.EndsWith(".c") && a.Contains("compound"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidExtraCreditIncrDecr()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_5/valid/extra_credit")
            .Where(a => a.EndsWith(".c") && (a.Contains("incr") || a.Contains("decr") || a.Contains("prefix") || a.Contains("postfix")));
        TestUtils.TestExecuteValid(files);
    }
}