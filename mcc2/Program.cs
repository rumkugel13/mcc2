using System;
using System.IO;

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
            int stages = 4;
            bool assemble = true;

            foreach (string arg in args)
            {
                if (arg.StartsWith('-'))
                {
                    string option = arg;
                    if (option == "--lex")
                        stages = 1;
                    else if (option == "--parse")
                        stages = 2;
                    else if (option == "--codegen")
                        stages = 3;
                    else if (option == "-S")
                        assemble = false;
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
                assembly = CompilerDriver.Compile(processed, stages);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error on stage {stages}:");
                Console.WriteLine(e.Message);
                return -3 - stages;
            }
            finally
            {
                File.Delete(processed);
            }

            if (stages == 4 && assemble)
                CompilerDriver.AssembleAndLink(assembly);
            if (assemble)
                File.Delete(assembly);

            return 0;
        }
    }
}