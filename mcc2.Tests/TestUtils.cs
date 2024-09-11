using System.Diagnostics;

namespace mcc2.Tests;

public static class TestUtils
{
    public static string TestsPath = "../../../../writing-a-c-compiler-tests/tests/";

    internal static bool TestLex(string file)
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

    internal static void TestInvalidLex(IEnumerable<string> files)
    {
        foreach (var file in files)
        {
            var preProcessed = CompilerDriver.Preprocessor(file);
            var result = TestLex(preProcessed);
            File.Delete(preProcessed);
            Assert.IsFalse(result, $"Expected Lexing to fail for {file}");
        }
    }

    internal static bool TestParse(string file)
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

    internal static void TestInvalidParse(IEnumerable<string> files)
    {
        foreach (var file in files)
        {
            var preProcessed = CompilerDriver.Preprocessor(file);
            var result = TestParse(preProcessed);
            File.Delete(preProcessed);
            Assert.IsFalse(result, $"Expected Parsing to fail for {file}");
        }
    }

    internal static void TestCompileValid(IEnumerable<string> files)
    {
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

    internal static int GetReturnVal(string exe)
    {
        using Process process = new Process();
        process.StartInfo.FileName = exe;
        process.Start();
        process.WaitForExit();
        return process.ExitCode;
    }

    internal static int GetExpectedReturnVal(string file)
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

    internal static void TestExecuteValid(IEnumerable<string> files)
    {
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

            var expected = TestUtils.GetExpectedReturnVal(file);
            var actual = TestUtils.GetReturnVal(result);
            File.Delete(result);
            Assert.AreEqual(expected, actual, $"Expected return values to match for {file}");
        }
    }

    internal static void TestExternal(int chapter)
    {
        var testRunner = "../../../../writing-a-c-compiler-tests/test_compiler";
        var mcc2 = "../../../../mcc2/bin/Debug/net8.0/mcc2";

        using Process process = new Process();
        process.StartInfo.FileName = testRunner;
        process.StartInfo.Arguments = $"{mcc2} --chapter {chapter} --latest-only";
        process.Start();
        process.WaitForExit();
        Assert.AreEqual(0, process.ExitCode, "Error running tests");
    }
}