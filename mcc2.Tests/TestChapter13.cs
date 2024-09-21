namespace mcc2.Tests;

[TestClass]
public class TestChapter13
{
    [TestMethod]
    public void TestInvalidLex()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/invalid_lex").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidLex(files);
    }

    [TestMethod]
    public void TestInvalidParse()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/invalid_parse").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidParse(files);
    }

    [TestMethod]
    public void TestInvalidSemanticsTypes()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/invalid_types").Where(a => a.EndsWith(".c"));
        TestUtils.TestInvalidSemantics(files);
    }

    [TestMethod]
    public void TestCompileValidExplicitCasts()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/explicit_casts").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/extra_credit")
            .Where(a => a.EndsWith(".c") && !a.Contains("nan"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidExtraCreditNan()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/extra_credit")
            .Where(a => a.EndsWith(".c") && a.Contains("nan"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidImplicitCasts()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/implicit_casts").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/libraries").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidConstants()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/constants").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidFloatingExpressions()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/floating_expressions").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidFunctionCalls()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/function_calls").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestCompileValidSpecialValues()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/special_values").Where(a => a.EndsWith(".c"));
        TestUtils.TestCompileValid(files);
    }

    [TestMethod]
    public void TestExecuteValidExplicitCasts()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/explicit_casts").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidExtraCredit()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/extra_credit")
            .Where(a => a.EndsWith(".c") && !a.Contains("nan"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidExtraCreditNan()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/extra_credit")
            .Where(a => a.EndsWith(".c") && a.Contains("nan"));
        var helper = TestUtils.TestsPath + "chapter_13/helper_libs/nan.c";
        TestUtils.TestExecuteValidSpecial(files.First(), helper);
    }

    [TestMethod]
    public void TestExecuteValidImplicitCasts()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/implicit_casts").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidLibraries()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("double_and")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("double_parameters")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("double_params")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files, "-lm");

        files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("extern_double")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);

        files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/libraries")
            .Where(a => a.EndsWith(".c") && a.Contains("use_arg")).ToList();
        TestUtils.TestExecuteValidLibraryCall(files);
    }

    [TestMethod]
    public void TestExecuteValidConstants()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/constants").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidFloatingExpressions()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/floating_expressions").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }

    [TestMethod]
    public void TestExecuteValidFunctionCalls()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/function_calls")
            .Where(a => a.EndsWith(".c") && !a.Contains("library_call"));
        TestUtils.TestExecuteValid(files);

        var special = TestUtils.TestsPath + "chapter_13/valid/function_calls/standard_library_call.c";
        TestUtils.TestExecuteValid(files, CompilerDriver.Optimizations.None ,"-lm");   // link math library
    }

    [TestMethod]
    public void TestExecuteValidSpecialValues()
    {
        var files = Directory.GetFiles(TestUtils.TestsPath + "chapter_13/valid/special_values").Where(a => a.EndsWith(".c"));
        TestUtils.TestExecuteValid(files);
    }
}