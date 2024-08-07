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

        public static string Compile(string file, Stages stages = Stages.Emitter)
        {
            string output = $"{file[..^2]}.s";
            string source = File.ReadAllText(file);

            if (stages >= Stages.Lex)
            {
                Lexer lexer = new();
                List<Lexer.Token> tokenList = lexer.Lex(source);

                if (stages >= Stages.Parse)
                {
                    Parser parser = new(source);
                    ASTProgram programAST = parser.Parse(tokenList);

                    //PrettyPrinter prettyPrinter = new PrettyPrinter();
                    //prettyPrinter.Print(programAST, source);

                    if (stages >= Stages.Tacky)
                    {
                        TackyEmitter tackyEmitter = new TackyEmitter();
                        var tacky = tackyEmitter.Emit(programAST);

                        if (stages >= Stages.Assembly)
                        {
                            AssemblyGenerator generator = new();
                            AssemblyProgram assembly = generator.Generate(programAST);

                            if (stages >= Stages.Emitter)
                            {
                                CodeEmitter codeEmitter = new();
                                var builder = codeEmitter.Emit(assembly);

                                File.WriteAllText(output, builder.ToString());
                            }
                        }
                    }
                }
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