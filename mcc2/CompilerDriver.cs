using System.Diagnostics;

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

            List<Lexer.Token> tokenList = [];
            if (stages > 0)
            {
                Lexer lexer = new Lexer();
                tokenList = lexer.Lex(File.ReadAllText(file));
            }
            if (stages > 1)
            {
                Parser parser = new Parser();
                var programAST = parser.Parse(tokenList);
            }
            if (stages > 2)
            {
                //todo: run codegen
            }
            if (stages > 3)
            {
                //todo: run codeemit
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