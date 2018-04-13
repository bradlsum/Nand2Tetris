using System;
using System.IO;
using System.Linq;

namespace VMTranslator
{
    class Program
    {
        static string[] memory = { "static", "this", "local", "argument", "that", "constant", "pointer", "temp" };
        static string[] arithmetic = { "add", "sub", "neg", "eq", "gt", "lt", "and", "or", "not" };
        static int lableId = 0;

        static void Translate(StreamReader read, StreamWriter write)
        {
            Console.WriteLine("Started translation");

            // Split into lines
            string[] numLines = read.ReadToEnd().Split('\n');
            Console.WriteLine(numLines.GetLength(0));
            foreach(string line in numLines)
            {
                Console.WriteLine(line);
                if (line[0] == '/')
                {
                    Console.WriteLine("Comment");
                }
                else if (line == "" || line[0] ==  '\t' || line[0] == '\n' || line.Length == 0)
                {
                    Console.WriteLine("Empty line");
                }
                else
                {
                    // Split into tokens
                    string[] token = line.Split(' ');
                    Console.WriteLine(token.GetLength(0));
                    Console.WriteLine(line);

                    int x = Int32.Parse(token[2]); // Get index as int

                    if (token[0] == "push")
                    {
                        Push(read, write, token[1], x);
                    }
                    else if (token[0] == "pop")
                    {
                        Pop(read, write, token[1], x);
                    }
                    else if (arithmetic.Contains(token[0]))
                    {
                        WriteArithmetic(token[0], write);
                    }
                    else
                    {
                        Console.WriteLine("Not a legal operation");
                    }
                }

            }
        }
        static void PushD(StreamWriter write)
        {
            // PUSH D Register onto stack
            write.WriteLine("@SP");
            write.WriteLine("M=D");
            write.WriteLine("AM=M+1");
        }
        static void PopD(StreamWriter write)
        {
            // POP stack top into D Register
            write.WriteLine("@SP");
            write.WriteLine("AM=M-1");
            write.WriteLine("D=M");
        }
        static void Push(StreamReader read, StreamWriter write, string arg, int index)
        {
            switch (arg)
            {
                case "static":
                    write.WriteLine("@16");
                    write.WriteLine("D=A");
                    write.WriteLine("@" + index);
                    write.WriteLine("D=D+A");
                    PushD(write);
                    break;
                case "this":
                    write.WriteLine("@THIS  ");
                    write.WriteLine("D=M");
                    PushD(write);
                    break;
                case "that":
                    write.WriteLine("@THAT");
                    write.WriteLine("D=M");
                    PushD(write);
                    break;
                case "local":
                    write.WriteLine("@LCL");
                    write.WriteLine("@" + index);
                    write.WriteLine("A=D+A");  // D = LCL + index
                    write.WriteLine("D=M");  // D = LCL
                    PushD(write);
                    break;
                case "argument":
                    write.WriteLine("@ARG");
                    write.WriteLine("D=A");
                    write.WriteLine("@" + index);
                    write.WriteLine("A=D+A");
                    write.WriteLine("D=M");
                    PushD(write);
                    break;
                case "temp":
                    if (index < 8)
                    {
                        write.WriteLine("@TEMP");
                        write.WriteLine("D=A");
                        write.WriteLine("@" + index);
                        write.WriteLine("A=D+A");
                        write.WriteLine("D=M");
                        PushD(write);
                    }
                    else
                    {
                        Console.WriteLine("Temp out of scope");
                    }
                    break;
                case "constant":
                    write.WriteLine("@" + index);
                    write.WriteLine("D=A");
                    PushD(write);
                    break;
                case "pointer":
                    if (index == 0)
                    {
                        write.WriteLine("@THIS  ");
                        write.WriteLine("D=M");
                        PushD(write);
                    }
                    else if (index == 1)
                    {
                        write.WriteLine("@THAT");
                        write.WriteLine("D=M");
                        PushD(write);
                    }
                    else
                    {
                        Console.WriteLine("Invalid pointer");
                    }
                    break;
            }
        }
        static void Pop(StreamReader read, StreamWriter write, string arg, int index)
        {
            switch (arg)
            {
                case "static":
                    SetupIndex(write, "16", index);
                    write.WriteLine("@R15");
                    write.WriteLine("M=D");
                    break;
                case "this":
                    PopD(write);
                    write.WriteLine("@THIS");
                    write.WriteLine("M=D");
                    break;
                case "that":
                    PopD(write);
                    write.WriteLine("@THAT");
                    write.WriteLine("M=D");
                    break;
                case "local":
                    SetupIndex(write, "LCL", index);
                    write.WriteLine("@R15");
                    write.WriteLine("M=D");
                    break;
                case "argument":
                    SetupIndex(write, "ARG", index);
                    write.WriteLine("@R15");
                    write.WriteLine("M=D");
                    break;
                case "temp":
                    if (index < 8)
                    {
                        SetupIndex(write, "TEMP", index);
                        write.WriteLine("@R15");
                        write.WriteLine("M=D");
                    }
                    else
                    {
                        Console.WriteLine("Temp out of scope");
                    }
                    break;
                case "pointer":
                    if (index == 0)
                    {
                        PopD(write);
                        write.WriteLine("@THIS");
                        write.WriteLine("M=D");
                    }
                    else if (index == 1)
                    {
                        PopD(write);
                        write.WriteLine("@THAT");
                        write.WriteLine("M=D");
                    }
                    else
                    {
                        Console.WriteLine("Invalid pointer");
                    }
                    break;
            }
        }
        static void SetupIndex(StreamWriter write, string type, int index)
        {
            write.WriteLine("@" + type);
            write.WriteLine("D=A");
            write.WriteLine("@" + index);
            write.WriteLine("D=D+A");
            write.WriteLine("@R15");
            write.WriteLine("M=D");
            PopD(write);
        }
        static void WriteArithmetic(string op, StreamWriter write)
        {
            switch(op)
            {
                case "add":
                    PopD(write); // Pop top of stack into D; side effect: A = SP
                    write.WriteLine("A=A-1");
                    write.WriteLine("M=M+D");
                    break;
                case "sub":
                    PopD(write); // side effect: A = SP
                    write.WriteLine("A=A-1");
                    write.WriteLine("M=M-D");
                    break;
                case "neg":
                    PopD(write);
                    write.WriteLine("A=A-1");
                    write.WriteLine("M=!D");
                    write.WriteLine("M=M+1");
                    break;
                case "eq":
                    PopD(write);
                    write.WriteLine("A=A-1");
                    write.WriteLine("D=M-D");
                    write.WriteLine("R12=A");
                    write.WriteLine("@FALSE");
                    write.WriteLine("D;JNE");
                    write.WriteLine("A=R12");
                    write.WriteLine("M=1");
                    write.WriteLine("@DONE");
                    write.WriteLine("D;JMP");
                    write.WriteLine("(FALSE)" + lableId);
                    lableId++;
                    write.WriteLine("A=R12");
                    write.WriteLine("M=0");
                    write.WriteLine("(DONE)" + lableId);
                    lableId++;
                    break;
                case "gt":
                    PopD(write);
                    write.WriteLine("A=A-1");
                    write.WriteLine("D=M-D");
                    write.WriteLine("R12=A");
                    write.WriteLine("@FALSE");
                    write.WriteLine("D;JLE");
                    write.WriteLine("A=R12");
                    write.WriteLine("M=1");
                    write.WriteLine("@DONE");
                    write.WriteLine("D;JMP");
                    write.WriteLine("(FALSE)" + lableId);
                    lableId++;
                    write.WriteLine("A=R12");
                    write.WriteLine("M=0");
                    write.WriteLine("(DONE)" + lableId);
                    lableId++;
                    break;
                case "lt":
                    PopD(write);
                    write.WriteLine("A=A-1");
                    write.WriteLine("D=M-D");
                    write.WriteLine("R12=A");
                    write.WriteLine("@FALSE");
                    write.WriteLine("D;JGE");
                    write.WriteLine("A=R12");
                    write.WriteLine("M=1");
                    write.WriteLine("@DONE");
                    write.WriteLine("D;JMP");
                    write.WriteLine("(FALSE)" + lableId);
                    lableId++;
                    write.WriteLine("A=R12");
                    write.WriteLine("M=0");
                    write.WriteLine("(DONE)" + lableId);
                    lableId++;
                    break;
                case "and":
                    PopD(write);
                    write.WriteLine("A=A-1");
                    write.WriteLine("M=M&D");
                    break;
                case "or":
                    PopD(write);
                    write.WriteLine("A=A-1");
                    write.WriteLine("M=M|D");
                    break;
                case "not":
                    PopD(write);
                    write.WriteLine("A=A-1");
                    write.WriteLine("M=!D");
                    break;
            }      
        }
        static void Main(string[] args)
        {
            string targetDirectory = "C:\\Users\\Playe\\OneDrive\\Documents\\School\\spring2018\\nand2tetris\\projects\\07\\VMTranslator\\VMTranslator";
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
            {
                if (fileName.Contains(".vm"))
                {
                    try
                    {
                        string outfilename = fileName.Substring(0, (fileName.Length - 3)) + ".asm";
                        using (var writer = new StreamWriter(outfilename))
                        {
                            using (var reader = new StreamReader(fileName))
                            {
                                Translate(reader, writer);
                                Console.WriteLine("VM file assembled");
                            }
                        }
                    }
                    catch
                    {
                        Console.WriteLine("File not found");
                    }
                }
            }
            Console.WriteLine("\n\nPress any key...");
            Console.ReadKey();
        }
    }
}