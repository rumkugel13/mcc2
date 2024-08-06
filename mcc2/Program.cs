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

            string file = args[0];

            if (!File.Exists(file))
            {
                Console.WriteLine("Invalid Source File");
                return -2;
            }

            if (!file.EndsWith(".c"))
            {
                Console.WriteLine("Not a C source File");
                return -3;
            }

            string processed = CompilerDriver.Preprocessor(file);

            int stages = 4;
            bool assemble = true;
            if (args.Length == 2)
            {
                string option = args[1];
                if (option == "--lex")
                    stages = 1;
                else if (option == "--parse")
                    stages = 2;
                else if (option == "--codegen")
                    stages = 3;
                else if (option == "-S")
                    assemble = false;
            }

            string assembly;
            try
            {
                assembly = CompilerDriver.Compile(processed, stages);
            }
            catch
            {
                Console.WriteLine($"Error on stage {stages}");
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