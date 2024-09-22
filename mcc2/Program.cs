
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
            CompilerDriver.CompilerOptions compilerOptions = new CompilerDriver.CompilerOptions() {
                File = "", 
                Stages = CompilerDriver.Stages.Emitter, 
                Optimizations = CompilerDriver.Optimizations.None, 
                PrettyPrint = false
            };
            bool assemble = true;
            bool link = true;
            string linkOption = "";

            foreach (string arg in args)
            {
                if (arg.StartsWith('-'))
                {
                    string option = arg;
                    switch (option)
                    {
                        case "--lex":
                            compilerOptions.Stages = CompilerDriver.Stages.Lex;
                            break;
                        case "--parse":
                            compilerOptions.Stages = CompilerDriver.Stages.Parse;
                            break;
                        case "--validate":
                            compilerOptions.Stages = CompilerDriver.Stages.Validate;
                            break;
                        case "--tacky":
                            compilerOptions.Stages = CompilerDriver.Stages.Tacky;
                            break;
                        case "--codegen":
                            compilerOptions.Stages = CompilerDriver.Stages.Assembly;
                            break;
                        case "--fold-constants":
                            compilerOptions.Optimizations |= CompilerDriver.Optimizations.FoldConstants;
                            break;
                        case "--propagate-copies":
                            compilerOptions.Optimizations |= CompilerDriver.Optimizations.PropagateCopies;
                            break;
                        case "--eliminate-unreachable-code":
                            compilerOptions.Optimizations |= CompilerDriver.Optimizations.EliminateUnreachableCode;
                            break;
                        case "--eliminate-dead-stores":
                            compilerOptions.Optimizations |= CompilerDriver.Optimizations.EliminateDeadStores;
                            break;
                        case "--optimize":
                            compilerOptions.Optimizations = CompilerDriver.Optimizations.All;
                            break;
                        case "-s":
                        case "-S":
                            assemble = false;
                            break;
                        case "-c":
                            link = false;
                            break;
                        case "--pretty":
                            compilerOptions.PrettyPrint = true;
                            break;
                        default:
                            if (option.StartsWith("-l"))
                                linkOption = option;
                            else
                            {
                                Console.WriteLine($"Invalid option: {option}");
                                return 4;
                            }

                            break;
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

                    if (!(file[^2..] == ".c"))
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
                compilerOptions.File = processed;

                try
                {
                    string assembly = CompilerDriver.Compile(compilerOptions);
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

            if (compilerOptions.Stages == CompilerDriver.Stages.Emitter)
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