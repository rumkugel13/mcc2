namespace mcc2.Tests;

[TestClass]
public class TestChapter11
{
    [TestMethod]
    public void TestInvalidLex()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_11/invalid_lex").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidLex(files);
    }

    [TestMethod]
    public void TestInvalidParse()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_11/invalid_parse").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidParse(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsLabelsExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_11/invalid_labels/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTypes()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_11/invalid_types").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestCompileValidExplicitCasts()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_11/valid/explicit_casts").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_11/valid/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidImplicitCasts()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_11/valid/implicit_casts").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_11/valid/libraries").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidLongExpressions()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_11/valid/long_expressions").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteValidExplicitCasts()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_11/valid/explicit_casts").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_11/valid/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidImplicitCasts()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_11/valid/implicit_casts").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_11/valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("long_args")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + "chapter_11/valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("long_global")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + "chapter_11/valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("maintain_stack")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + "chapter_11/valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("return_long")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);
    }

    [TestMethod]
    public void TestExecuteValidLongExpressions()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_11/valid/long_expressions").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }
}