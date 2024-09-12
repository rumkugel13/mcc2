namespace mcc2.Tests;

[TestClass]
public class TestChapter16
{
    private const string chapter = "chapter_16/";

    [TestMethod]
    public void TestInvalidLex()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_lex").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidLex(files);
    }

    [TestMethod]
    public void TestInvalidParse()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_parse").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidParse(files);
    }

    [TestMethod]
    public void TestInvalidParseExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_parse/extra_credit").Where(a => a.EndsWith(".c"));
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
    public void TestInvalidSemanticsLabelsExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "invalid_labels/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestCompileValidCharConstants()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/char_constants").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidChars()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/chars").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidStringsAsInitializers()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/strings_as_initializers").Where(a => a.EndsWith(".c"));
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
    public void TestCompileValidStringsAsLvalues()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/strings_as_lvalues").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteValidCharConstants()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/char_constants").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidChars()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/chars")
            .Where(a => a.EndsWith(".c") && !a.Contains("page_boundary"));
        TestUtils.TestExecuteValid(files);

        var special = TestUtils.TestsPath + chapter + "valid/chars/push_arg_on_page_boundary.c";
        var extra = TestUtils.TestsPath + chapter + "valid/chars/data_on_page_boundary_" + (OperatingSystem.IsMacOS() ? "osx.s" : "linux.s");
        TestUtils.TestExecuteValidSpecial(special, extra);
    }

    [TestMethod]
    public void TestExecuteValidLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("char_arguments")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("global_char")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("return_char")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);
    }

    [TestMethod]
    public void TestExecuteValidStringsAsLvalues()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/strings_as_lvalues").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidStringsAsInitializers()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + chapter + "valid/strings_as_initializers").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }
}