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

        public static string Compile(string file, Stages stages = Stages.Emitter, bool prettyPrint = false)
        {
            string output = $"{file[..^2]}.s";
            string source = File.ReadAllText(file);

            if (stages < Stages.Lex)
                return output;

            List<Lexer.Token> tokenList = new Lexer().Lex(source);

            if (stages < Stages.Parse)
                return output;

            ASTProgram programAST = new Parser(source).Parse(tokenList);

            if (stages < Stages.Validate)
                return output;

            Dictionary<string, SemanticAnalyzer.SymbolEntry> symbolTable = [];
            new SemanticAnalyzer().Analyze(programAST, symbolTable);

            if (prettyPrint)
                new PrettyPrinter().Print(programAST, source);

            if (stages < Stages.Tacky)
                return output;

            TAC.TACProgam tacky = new TackyEmitter().Emit(programAST);

            if (stages < Stages.Assembly)
                return output;

            AssemblyProgram assembly = new AssemblyGenerator().Generate(tacky);

            if (stages >= Stages.Emitter)
                File.WriteAllText(output, new CodeEmitter(symbolTable).Emit(assembly).ToString());

            return output;
        }

        public static string AssembleAndLink(string file)
        {
            string output = $"{file[..^2]}";
            using Process process = new Process();
            process.StartInfo.FileName = "gcc";
            process.StartInfo.Arguments = $"{file} -o {output}";
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