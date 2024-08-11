
namespace mcc2
{
    internal class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Invalid Arguments");
                return -1;
            }

            string file = "";
            CompilerDriver.Stages stages = CompilerDriver.Stages.Emitter;
            bool assemble = true;
            bool link = true;
            bool prettyPrint = false;

            foreach (string arg in args)
            {
                if (arg.StartsWith('-'))
                {
                    string option = arg;
                    if (option == "--lex")
                        stages = CompilerDriver.Stages.Lex;
                    else if (option == "--parse")
                        stages = CompilerDriver.Stages.Parse;
                    else if (option == "--validate")
                        stages = CompilerDriver.Stages.Validate;
                    else if (option == "--tacky")
                        stages = CompilerDriver.Stages.Tacky;
                    else if (option == "--codegen")
                        stages = CompilerDriver.Stages.Assembly;
                    else if (option == "-S")
                        assemble = false;
                    else if (option == "-c")
                        link = false;
                    else if (option == "--pretty")
                        prettyPrint = true;
                    else
                    {
                        Console.WriteLine($"Invalid option: {option}");
                        return -4;
                    }
                }
                else
                {
                    file = arg;

                    if (!File.Exists(file))
                    {
                        Console.WriteLine($"Invalid Source File: {file}");
                        return -2;
                    }

                    if (!file.EndsWith(".c"))
                    {
                        Console.WriteLine("Not a C source File");
                        return -3;
                    }
                }
            }

            string processed = CompilerDriver.Preprocessor(file);

            string assembly;
            try
            {
                assembly = CompilerDriver.Compile(processed, stages, prettyPrint);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error on stage {stages}:");
                Console.WriteLine(e.Message);
                return -3 - (int)stages;
            }
            finally
            {
                File.Delete(processed);
            }

            if (stages == CompilerDriver.Stages.Emitter)
            {
                if (assemble && link)
                    CompilerDriver.AssembleAndLink(assembly);
                else if (assemble)
                    CompilerDriver.AssembleOnly(assembly);
            }

            if (assemble)
                File.Delete(assembly);

            return 0;
        }
    }
}