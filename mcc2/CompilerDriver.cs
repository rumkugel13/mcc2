using System.Diagnostics;
using mcc2.Assembly;
using mcc2.AST;

namespace mcc2
{
    public class CompilerDriver
    {
        public enum Stages
        {
            None,
            Lex,
            Parse,
            Validate,
            Tacky,
            Assembly,
            Emitter,
        }

        [Flags]
        public enum Optimizations
        {
            None = 0,
            FoldConstants = 1,
            PropagateCopies = 2,
            EliminateUnreachableCode = 4,
            EliminateDeadStores = 8,
            All = FoldConstants | PropagateCopies | EliminateUnreachableCode | EliminateDeadStores
        }

        public struct CompilerOptions
        {
            public string File;
            public Stages Stages;
            public Optimizations Optimizations;
            public bool PrettyPrint;
        }

        public static string Preprocessor(string file)
        {
            string output = $"{file[..^2]}.i";
            using Process process = new Process();
            process.StartInfo.FileName = "gcc";
            process.StartInfo.Arguments = $"-E -P {file} -o {output}";
            process.Start();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                Console.WriteLine("Error running Preprocessor");
            }
            return output;
        }

        public static string Compile(CompilerOptions compilerOptions)// string file, Stages stages = Stages.Emitter, bool prettyPrint = false)
        {
            string output = $"{compilerOptions.File[..^2]}.s";
            string source = File.ReadAllText(compilerOptions.File);

            if (compilerOptions.Stages < Stages.Lex)
                return output;

            List<Lexer.Token> tokenList = new Lexer().Lex(source);

            if (compilerOptions.Stages < Stages.Parse)
                return output;

            ASTProgram programAST = new Parser(source).Parse(tokenList);

            if (compilerOptions.Stages < Stages.Validate)
                return output;

            Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable = [];
            Dictionary<string, SemanticAnalyzer.StructEntry> typeTable = [];
            new SemanticAnalyzer().Analyze(programAST, symbolTable, typeTable);

            if (compilerOptions.PrettyPrint)
                new PrettyPrinter(symbolTable, typeTable).Print(programAST);

            if (compilerOptions.Stages < Stages.Tacky)
                return output;

            TAC.TACProgam tacky = new TackyEmitter(symbolTable, typeTable).Emit(programAST);

            if (compilerOptions.Optimizations != Optimizations.None)
                tacky = new TackyOptimizer().Optimize(tacky, compilerOptions.Optimizations);

            if (compilerOptions.Stages < Stages.Assembly)
                return output;

            AssemblyProgram assembly = new AssemblyGenerator(symbolTable, typeTable).Generate(tacky);

            if (compilerOptions.Stages >= Stages.Emitter)
                File.WriteAllText(output, new CodeEmitter().Emit(assembly).ToString());

            return output;
        }

        public static string AssembleAndLink(string file, string linkOption)
        {
            string output = $"{file[..^2]}";
            using Process process = new Process();
            process.StartInfo.FileName = "gcc";
            process.StartInfo.Arguments = $"{file} -o {output} {linkOption}";
            process.Start();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                Console.WriteLine("Error running AssembleAndLink");
            }
            return output;
        }

        public static string AssembleAndLinkFiles(List<string> files, string output, string linkOption)
        {
            using Process process = new Process();
            process.StartInfo.FileName = "gcc";
            foreach (var file in files)
                process.StartInfo.ArgumentList.Add(file);
            process.StartInfo.ArgumentList.Add("-o");
            process.StartInfo.ArgumentList.Add(output);
            if (!string.IsNullOrEmpty(linkOption))
                process.StartInfo.ArgumentList.Add(linkOption);
            process.Start();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                Console.WriteLine("Error running AssembleAndLink");
            }
            return output;
        }

        public static string AssembleOnly(string file)
        {
            string output = $"{file[..^2]}.o";
            using Process process = new Process();
            process.StartInfo.FileName = "gcc";
            process.StartInfo.Arguments = $"-c {file} -o {output}";
            process.Start();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                Console.WriteLine("Error running AssembleOnly");
            }
            return output;
        }
    }
}