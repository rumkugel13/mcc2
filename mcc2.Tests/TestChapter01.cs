using System.Diagnostics;

namespace mcc2.Tests;

[TestClass]
public class TestChapter01
{
    private bool TestLex(string file)
    {
        Lexer lexer = new Lexer();
        try
        {
            lexer.Lex(File.ReadAllText(file));
        }
        catch
        {
            return false;
        }
        return true;
    }

    [TestMethod]
    public void TestInvalidLex()
    {
        var files = Directory.GetFiles("../../../../writing-a-c-compiler-tests/tests/chapter_1/invalid_lex");

        foreach (var file in files)
        {
            var preProcessed = CompilerDriver.Preprocessor(file);
            Assert.IsFalse(TestLex(preProcessed));
            File.Delete(preProcessed);
        }
    }

    private bool TestParse(string file)
    {
        Lexer lexer = new Lexer();
        List<Lexer.Token> tokens = [];
        try
        {
            tokens = lexer.Lex(File.ReadAllText(file));
        }
        catch
        {
            Assert.Fail($"Expected to pass Lexer for {file}");
        }

        Parser parser = new Parser(file);
        try
        {
            parser.Parse(tokens);
        }
        catch
        {
            return false;
        }

        return true;
    }

    [TestMethod]
    public void TestInvalidParse()
    {
        var files = Directory.GetFiles("../../../../writing-a-c-compiler-tests/tests/chapter_1/invalid_parse");

        foreach (var file in files)
        {
            var preProcessed = CompilerDriver.Preprocessor(file);
            Assert.IsFalse(TestParse(preProcessed));
            File.Delete(preProcessed);
        }
    }

    [TestMethod]
    public void TestCompileValid()
    {
        var files = Directory.GetFiles("../../../../writing-a-c-compiler-tests/tests/chapter_1/valid");
        CompilerDriver.CompilerOptions compilerOptions = new CompilerDriver.CompilerOptions();
        compilerOptions.Optimizations = CompilerDriver.Optimizations.None;
        compilerOptions.PrettyPrint = false;
        compilerOptions.Stages = CompilerDriver.Stages.Assembly;

        foreach (var file in files)
        {
            var preProcessed = CompilerDriver.Preprocessor(file);
            compilerOptions.File = preProcessed;
            try
            {
                var result = CompilerDriver.Compile(compilerOptions);
            }
            catch
            {
                Assert.Fail($"Expected compilation to pass for {preProcessed}");
            }
            finally
            {
                File.Delete(preProcessed);
            }
        }
    }

    [TestMethod]
    public void TestAll()
    {
        var testRunner = "../../../../writing-a-c-compiler-tests/test_compiler";
        var mcc2 = "../../../../mcc2/bin/Debug/net8.0/mcc2";

        using Process process = new Process();
        process.StartInfo.FileName = testRunner;
        process.StartInfo.Arguments = $"{mcc2} --chapter 1 --latest-only";
        process.Start();
        process.WaitForExit();
        Assert.AreEqual(0, process.ExitCode, "Error running tests");
    }
}