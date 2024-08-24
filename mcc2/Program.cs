
namespace mcc2
{
    internal class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Invalid Arguments");
                return 1;
            }

            List<string> files = [];
            CompilerDriver.Stages stages = CompilerDriver.Stages.Emitter;
            bool assemble = true;
            bool link = true;
            bool prettyPrint = false;
            string linkOption = "";

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
                    else if (option.StartsWith("-l"))
                        linkOption = option;
                    else
                    {
                        Console.WriteLine($"Invalid option: {option}");
                        return 4;
                    }
                }
                else
                {
                    var file = arg;

                    if (!File.Exists(file))
                    {
                        Console.WriteLine($"Invalid Source File: {file}");
                        return 2;
                    }

                    if (!file.EndsWith(".c"))
                    {
                        Console.WriteLine("Not a C source File");
                        return 3;
                    }

                    files.Add(file);
                }
            }

            List<string> assemblyFiles = [];
            foreach (var file in files)
            {
                string processed = CompilerDriver.Preprocessor(file);

                try
                {
                    string assembly = CompilerDriver.Compile(processed, stages, prettyPrint);
                    assemblyFiles.Add(assembly);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return 10;
                }
                finally
                {
                    File.Delete(processed);
                }
            }

            if (stages == CompilerDriver.Stages.Emitter)
            {
                if (assemble && link)
                {
                    if (assemblyFiles.Count == 1)
                        CompilerDriver.AssembleAndLink(assemblyFiles[0], linkOption);
                    else
                        CompilerDriver.AssembleAndLinkFiles(assemblyFiles, $"{assemblyFiles[0][..^2]}", linkOption);
                }
                else if (assemble)
                {
                    foreach (var assembly in assemblyFiles)
                    {
                        CompilerDriver.AssembleOnly(assembly);
                    }
                }
            }

            if (assemble)
            {
                foreach (var assembly in assemblyFiles)
                {
                    File.Delete(assembly);
                }
            }

            return 0;
        }
    }
}