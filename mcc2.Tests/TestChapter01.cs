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
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return false;
        }
        return true;
    }

    [TestMethod]
    public void TestInvalidLex()
    {
        var files = Directory.GetFiles("../../../../writing-a-c-compiler-tests/tests/chapter_1/invalid_lex").Where(a => a.EndsWith(".c"));

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
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Assert.Fail($"Expected to pass Lexer for {file}");
        }

        Parser parser = new Parser(file);
        try
        {
            parser.Parse(tokens);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return false;
        }

        return true;
    }

    [TestMethod]
    public void TestInvalidParse()
    {
        var files = Directory.GetFiles("../../../../writing-a-c-compiler-tests/tests/chapter_1/invalid_parse").Where(a => a.EndsWith(".c"));

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
        var files = Directory.GetFiles("../../../../writing-a-c-compiler-tests/tests/chapter_1/valid").Where(a => a.EndsWith(".c"));
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

    private int GetReturnVal(string exe)
    {
        using Process process = new Process();
        process.StartInfo.FileName = exe;
        process.Start();
        process.WaitForExit();
        return process.ExitCode;
    }

    private int GetExpectedReturnVal(string file)
    {
        using Process process = new Process();
        process.StartInfo.FileName = "gcc";
        process.StartInfo.Arguments = $"{file} -o {file}.out";
        process.Start();
        process.WaitForExit();
        Assert.AreEqual(0, process.ExitCode, "Error running gcc");

        var expected = GetReturnVal($"{file}.out");
        File.Delete($"{file}.out");
        return expected;
    }

    [TestMethod]
    public void TestExecuteValid()
    {
        var files = Directory.GetFiles("../../../../writing-a-c-compiler-tests/tests/chapter_1/valid").Where(a => a.EndsWith(".c"));
        CompilerDriver.CompilerOptions compilerOptions = new CompilerDriver.CompilerOptions();
        compilerOptions.Optimizations = CompilerDriver.Optimizations.None;
        compilerOptions.PrettyPrint = false;
        compilerOptions.Stages = CompilerDriver.Stages.Emitter;

        foreach (var file in files)
        {
            var preProcessed = CompilerDriver.Preprocessor(file);
            compilerOptions.File = preProcessed;

            var assembly = "";
            try
            {
                assembly = CompilerDriver.Compile(compilerOptions);
            }
            catch
            {
                Assert.Fail($"Expected compilation to pass for {preProcessed}");
            }
            finally
            {
                File.Delete(preProcessed);
            }
            var result = CompilerDriver.AssembleAndLink(assembly, "");
            File.Delete(assembly);

            var expected = GetExpectedReturnVal(file);
            var actual = GetReturnVal(result);
            File.Delete(result);
            Assert.AreEqual(expected, actual, $"Expected return values to match for {file}");
        }
    }

    [TestMethod]
    public void TestExternal()
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