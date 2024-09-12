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
        string source = File.ReadAllText(file);
        Lexer lexer = new Lexer();
        List<Lexer.Token> tokens = [];
        try
        {
            tokens = lexer.Lex(source);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Assert.Fail($"Expected to pass Lexer for {file}");
        }

        Parser parser = new Parser(source);
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

    internal static bool TestSemantics(string file)
    {
        string source = File.ReadAllText(file);
        Lexer lexer = new Lexer();
        List<Lexer.Token> tokens = [];
        try
        {
            tokens = lexer.Lex(source);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Assert.Fail($"Expected to pass Lexer for {file}");
        }

        Parser parser = new Parser(source);
        AST.ASTProgram program = new AST.ASTProgram([]);
        try
        {
            program = parser.Parse(tokens);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Assert.Fail($"Expected to pass Parser for {file}");
        }

        SemanticAnalyzer semanticAnalyzer = new SemanticAnalyzer();
        try
        {
            semanticAnalyzer.Analyze(program, [], []);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return false;
        }

        return true;
    }

    internal static void TestInvalidSemantics(IEnumerable<string> files)
    {
        foreach (var file in files)
        {
            var preProcessed = CompilerDriver.Preprocessor(file);
            var result = TestSemantics(preProcessed);
            File.Delete(preProcessed);
            Assert.IsFalse(result, $"Expected Semantic Analyzer to fail for {file}");
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

    internal static int GetExpectedReturnVal(string file, string linkOption = "")
    {
        using Process process = new Process();
        process.StartInfo.FileName = "gcc";
        process.StartInfo.Arguments = $"{file} -o {file}.out" + (string.IsNullOrEmpty(linkOption) ? "" : " " + linkOption);
        process.StartInfo.RedirectStandardError = true;
        process.Start();
        process.WaitForExit();
        Assert.AreEqual(0, process.ExitCode, "Error running gcc:\n" + process.StandardError.ReadToEnd());

        var expected = GetReturnVal($"{file}.out");
        File.Delete($"{file}.out");
        return expected;
    }

    internal static int GetExpectedReturnValMulti(string[] files)
    {
        using Process process = new Process();
        process.StartInfo.FileName = "gcc";
        foreach (var file in files)
                process.StartInfo.ArgumentList.Add(file);
        process.StartInfo.ArgumentList.Add("-o");
        process.StartInfo.ArgumentList.Add($"{files[0]}.out");
        process.StartInfo.RedirectStandardError = true;
        process.Start();
        process.WaitForExit();
        Assert.AreEqual(0, process.ExitCode, "Error running gcc:\n" + process.StandardError.ReadToEnd());

        var expected = GetReturnVal($"{files[0]}.out");
        File.Delete($"{files[0]}.out");
        return expected;
    }

    internal static void TestExecuteValid(IEnumerable<string> files, string linkOption = "")
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
            var result = CompilerDriver.AssembleAndLink(assembly, linkOption);
            File.Delete(assembly);

            var expected = GetExpectedReturnVal(file, linkOption);
            var actual = GetReturnVal(result);
            File.Delete(result);
            Assert.AreEqual(expected, actual, $"Expected return values to match for {file}");
        }
    }

    internal static void TestExecuteValidSpecial(string file, string extra)
    {
        CompilerDriver.CompilerOptions compilerOptions = new CompilerDriver.CompilerOptions();
        compilerOptions.Optimizations = CompilerDriver.Optimizations.None;
        compilerOptions.PrettyPrint = false;
        compilerOptions.Stages = CompilerDriver.Stages.Emitter;

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

        var result = CompilerDriver.AssembleAndLinkFiles([assembly, extra], assembly[..^2], "");
        File.Delete(assembly);

        var expected = GetExpectedReturnValMulti([file, extra]);
        var actual = GetReturnVal(result);
        File.Delete(result);
        Assert.AreEqual(expected, actual, $"Expected return values to match for {file}");
    }

    internal static void TestExecuteValidLibraryCall(List<string> files)
    {
        Debug.Assert(files.Count == 2, "Only two supported for now");
        CompilerDriver.CompilerOptions compilerOptions = new CompilerDriver.CompilerOptions();
        compilerOptions.Optimizations = CompilerDriver.Optimizations.None;
        compilerOptions.PrettyPrint = false;
        compilerOptions.Stages = CompilerDriver.Stages.Emitter;

        var sources = new List<string>(files);
        var assemblies = new List<string>();
        foreach (var file in sources)
        {
            var preProcessed = CompilerDriver.Preprocessor(file);
            compilerOptions.File = preProcessed;

            try
            {
                assemblies.Add(CompilerDriver.Compile(compilerOptions));
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

        var expectedExe = CompilerDriver.AssembleAndLinkFiles(sources, files[0][..^2] + ".exp", "");
        var expected = GetReturnVal(expectedExe);
        File.Delete(expectedExe);

        var actualExe = CompilerDriver.AssembleAndLinkFiles(assemblies, files[0][..^2] + ".act", "");
        var actual = GetReturnVal(actualExe);
        File.Delete(actualExe);

        Assert.AreEqual(expected, actual, $"Expected return values to match for {files[0]} and {files[1]}");

        var gccFirst = CompilerDriver.AssembleAndLinkFiles([assemblies[0],sources[1]], files[0][..^2] + ".exp", "");
        var gccFirstVal = GetReturnVal(gccFirst);
        File.Delete(gccFirst);

        var mccFirst = CompilerDriver.AssembleAndLinkFiles([sources[0],assemblies[1]], files[0][..^2] + ".exp", "");
        var mccFirstVal = GetReturnVal(mccFirst);
        File.Delete(mccFirst);
        Assert.AreEqual(expected, gccFirstVal, $"Expected return values to match for mixed {files[0]} and {files[1]}");
        Assert.AreEqual(expected, mccFirstVal, $"Expected return values to match for mixed {files[0]} and {files[1]}");
    }

    internal static void TestExternal(int chapter)
    {
        var testRunner = "../../../../writing-a-c-compiler-tests/test_compiler";
        var mcc2 = "../../../../mcc2/bin/Debug/net8.0/mcc2";

        using Process process = new Process();
        process.StartInfo.FileName = testRunner;
        process.StartInfo.Arguments = $"{mcc2} --chapter {chapter} --latest-only";
        process.StartInfo.RedirectStandardError = true;
        process.Start();
        process.WaitForExit();
        Assert.AreEqual(0, process.ExitCode, "Error running tests:\n" + process.StandardError.ReadToEnd());
    }

    internal static void TestExternalExtraCredit(int chapter, string extra)
    {
        var testRunner = "../../../../writing-a-c-compiler-tests/test_compiler";
        var mcc2 = "../../../../mcc2/bin/Debug/net8.0/mcc2";

        using Process process = new Process();
        process.StartInfo.FileName = testRunner;
        process.StartInfo.Arguments = $"{mcc2} --chapter {chapter} --latest-only {extra}";
        process.StartInfo.RedirectStandardError = true;
        process.Start();
        process.WaitForExit();
        Assert.AreEqual(0, process.ExitCode, "Error running tests:\n" + process.StandardError.ReadToEnd());
    }
}