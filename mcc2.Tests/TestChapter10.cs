namespace mcc2.Tests;

[TestClass]
public class TestChapter10
{
    [TestMethod]
    public void TestInvalidParse()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_10/invalid_parse").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidParse(files);
    }

    [TestMethod]
    public void TestInvalidParseExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_10/invalid_parse/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidParse(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsDeclarations()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_10/invalid_declarations").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsLabelsExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_10/invalid_labels/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTypes()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_10/invalid_types").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTypesExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_10/invalid_types/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestCompileValid()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_10/valid").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_10/valid/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidExtraCreditLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_10/valid/extra_credit/libraries").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidLibraries()
    {
        // todo: properly use multiple files
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_10/valid/libraries").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteValid()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_10/valid")
            .Where(a => a.EndsWith(".c") && !a.Contains("page_boundary"));
        TestUtils.TestExecuteValid(files);

        var special = TestUtils.TestsPath + "chapter_10/valid/push_arg_on_page_boundary.c";
        var extra = TestUtils.TestsPath + "chapter_10/valid/data_on_page_boundary_" + (OperatingSystem.IsMacOS() ? "osx.s" : "linux.s");
        TestUtils.TestExecuteValidSpecial(special, extra);
    }

    [TestMethod]
    public void TestExecuteValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_10/valid/extra_credit").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidExtraCreditLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_10/valid/extra_credit/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("same_label")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);
    }

    [TestMethod]
    public void TestExecuteValidLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_10/valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("external_linkage_function")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + "chapter_10/valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("external_tentative")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + "chapter_10/valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("external_var_scoping")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + "chapter_10/valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("external_variable")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + "chapter_10/valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("internal_hides")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + "chapter_10/valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("internal_linkage_function")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + "chapter_10/valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("internal_linkage_var")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);
    }
}