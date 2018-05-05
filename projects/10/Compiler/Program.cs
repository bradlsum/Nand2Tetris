using System;

/// <summary>
/// This is a Jack Compiler that takes JACK code and turns it into valid HACK VM code.
/// Author: Taylor May, Sumner Bradley
/// Date: 4/28/18
/// </summary>

namespace Jack_Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Please enter a folder to translate:");
            string respond = Console.ReadLine();
            Console.WriteLine("Please enter the name of the output file");
            string filename = Console.ReadLine();

            CompileEngine.CompileFolder(respond, filename);
        }
    }
}