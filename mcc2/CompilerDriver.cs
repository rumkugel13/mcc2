using System.Diagnostics;
using mcc2.Assembly;
using mcc2.AST;

namespace mcc2
{
    public class CompilerDriver
    {
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

        public static string Compile(string file, int stages = 4)
        {
            string output = $"{file[..^2]}.s";
            string source = File.ReadAllText(file);

            List<Lexer.Token> tokenList = [];
            ASTProgram programAST = null;
            AssemblyProgram assembly = null;
            if (stages > 0)
            {
                Lexer lexer = new Lexer();
                tokenList = lexer.Lex(source);
            }
            if (stages > 1)
            {
                Parser parser = new Parser(source);
                programAST = parser.Parse(tokenList);

                //PrettyPrinter prettyPrinter = new PrettyPrinter();
                //prettyPrinter.Print(programAST, source);
            }
            if (stages > 2)
            {
                AssemblyGenerator generator = new();
                assembly = generator.Generate(programAST);
            }
            if (stages > 3)
            {
                CodeEmitter codeEmitter = new();
                var builder = codeEmitter.Emit(assembly);

                File.WriteAllText(output, builder.ToString());
            }

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
    }
}