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

            if (stages > 0)
            {
                //todo: run lexer
            }
            if (stages > 1)
            {
                //todo: run parser
            }
            if (stages > 2)
            {
                //todo: run codegen
            }
            if (stages > 3)
            {
                //todo: run codeemit
                File.WriteAllLines(output, [
                "   .globl main",
                "main:",
                "movl   $2, %eax",
                "ret"
            ]);
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