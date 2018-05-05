using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace VMTranslator
{
    class Program
    {
        // Static varriables
        static string[] memory = { "static", "this", "local", "argument", "that", "constant", "pointer", "temp" };
        static string[] arithmetic = { "add", "sub", "neg", "eq", "gt", "lt", "and", "or", "not" };
        static int lableId = 0;

        static void Translate(StreamReader read, StreamWriter write)
        {
            string line;
            Console.WriteLine("Started translation");

            // Split into lines
            List<string> numLines = new List<string>();
            //Console.WriteLine(numLines.GetLength(0));

            StreamReader file = read;
            while ((line = file.ReadLine()) != null)
            {
                numLines.Add(line);
            }
            file.Close();

            ClearWhitespace(numLines);

            for (int i = 0; i < numLines.Count; ++i)
            {
                // Split into tokens
                string[] token = numLines[i].Split(' ');
                Console.WriteLine(numLines[i]);

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
        static void PushD(StreamWriter write)
        {
            // PUSH D Register onto stack
            write.WriteLine("@SP");
            write.WriteLine("M=D");
            write.WriteLine("@SP");
            write.WriteLine("M=M+1");
        }
        static void PopD(StreamWriter write)
        {
            // POP stack top into D Register
            write.WriteLine("@SP");
            write.WriteLine("M=M-1");
            write.WriteLine("D=M");
        }
        static void Push(StreamReader read, StreamWriter write, string arg, int index)
        {
            switch (arg)
            {
                case "static":
                    write.WriteLine("@16");
                    write.WriteLine("D=A");
                    if (index != '0' && index != 0)
                    {
                        write.WriteLine("@" + index);
                        write.WriteLine("D=D+A");
                    }
                    write.WriteLine("A=D");
                    write.WriteLine("D=M");
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
                    if (index != '0' && index != 0)
                    {
                        write.WriteLine("@" + index);
                        write.WriteLine("D=D+A");
                    }
                    write.WriteLine("D=M");  // D = LCL
                    PushD(write);
                    break;
                case "argument":
                    write.WriteLine("@ARG");
                    write.WriteLine("D=A");
                    if (index != '0' && index != 0)
                    {
                        write.WriteLine("@" + index);
                        write.WriteLine("D=D+A");
                    }
                    write.WriteLine("D=M");
                    PushD(write);
                    break;
                case "temp":
                    if (index < 8)
                    {
                        write.WriteLine("@5");
                        write.WriteLine("D=A");
                        if (index != '0' && index != 0)
                        {
                            write.WriteLine("@" + index);
                            write.WriteLine("D=D+A");
                        }
                        write.WriteLine("A=D");
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
                        write.WriteLine("@THIS");
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
                    write.WriteLine("A=M");
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
                    write.WriteLine("A=M");
                    write.WriteLine("M=D");
                    break;
                case "argument":
                    SetupIndex(write, "ARG", index);
                    write.WriteLine("@R15");
                    write.WriteLine("A=M");
                    write.WriteLine("M=D");
                    break;
                case "temp":
                    if (index < 8)
                    {
                        SetupIndex(write, "5", index);
                        write.WriteLine("@R15");
                        write.WriteLine("A=M");
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
                    write.WriteLine("A=A-1");
                    write.WriteLine("M=-M");
                    break;
                case "eq":
                    PopD(write);
                    write.WriteLine("A=A-1");
                    write.WriteLine("D=M-D");
                    write.WriteLine("R12=A");
                    write.WriteLine("@FALSE" + lableId);
                    lableId++;
                    write.WriteLine("D;JNE");
                    write.WriteLine("A=R12");
                    write.WriteLine("M=1");
                    write.WriteLine("@DONE" + lableId);
                    lableId++;
                    write.WriteLine("D;JMP");
                    write.WriteLine("(FALSE" + (lableId - 2) + ")");
                    write.WriteLine("A=R12");
                    write.WriteLine("M=0");
                    write.WriteLine("(DONE" + (lableId - 1) + ")");
                    break;
                case "gt":
                    PopD(write);
                    write.WriteLine("A=A-1");
                    write.WriteLine("D=M-D");
                    write.WriteLine("R12=A");
                    write.WriteLine("@FALSE" + lableId);
                    lableId++;
                    write.WriteLine("D;JLE");
                    write.WriteLine("A=R12");
                    write.WriteLine("M=1");
                    write.WriteLine("@DONE" + lableId);
                    lableId++;
                    write.WriteLine("D;JMP");
                    write.WriteLine("(FALSE" + (lableId - 2) + ")");
                    write.WriteLine("A=R12");
                    write.WriteLine("M=0");
                    write.WriteLine("(DONE" + (lableId - 1) + ")");
                    break;
                case "lt":
                    PopD(write);
                    write.WriteLine("A=A-1");
                    write.WriteLine("D=M-D");
                    write.WriteLine("R12=A");
                    write.WriteLine("@FALSE" + lableId);
                    lableId++;
                    write.WriteLine("D;JGE");
                    write.WriteLine("A=R12");
                    write.WriteLine("M=1");
                    write.WriteLine("@DONE" + lableId);
                    lableId++;
                    write.WriteLine("D;JMP");
                    write.WriteLine("(FALSE" + (lableId - 2) + ")");
                    write.WriteLine("A=R12");
                    write.WriteLine("M=0");
                    write.WriteLine("(DONE" + (lableId - 1) + ")");
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
                    write.WriteLine("M=!M");
                    break;
            }      
        }
        private static void ClearWhitespace(List<string> fileLines)
        {
            for (int i = 0; i < fileLines.Count; i++)
            {
                //This clears leading whitespace
                int iterator = 0;
                int leadingWhiteSpaceEnd = 0;
                foreach (char c in fileLines[i])
                {
                    if (c != ' ')
                    {
                        leadingWhiteSpaceEnd = iterator;
                        break;
                    }
                    iterator++;
                }
                if (leadingWhiteSpaceEnd != 0)//Else there is no whitespace to clear
                {
                    fileLines[i] = fileLines[i].Substring(leadingWhiteSpaceEnd - 1, fileLines[i].Length - 1);
                }
                //This section will check for the number of commands as well as shave extra spaces in between commands
                while (true)
                {
                    bool checkSpacesMode = false;
                    bool foundSpace = false;
                    iterator = 0;
                    foreach (char c in fileLines[i])
                    {
                        if (checkSpacesMode)//WE are checking if there are consequtive spaces, if there are we need to remove them and restart this.
                        {//Ideally this will jump right back out of spaces mode, if it doesnt, then we have multiple spaces and this line needs a space stripped
                            if (c == ' ')
                            {
                                foundSpace = true;//This implies we need to restart so we will NOT break out of the while(true)
                                fileLines[i].Remove(iterator, 1);//remove this space, restart
                            }
                            else
                            {
                                if (foundSpace)//break and restart
                                {
                                    break;
                                }
                                else
                                {
                                    checkSpacesMode = false;
                                }
                            }
                        }
                        else
                        {
                            if (c == ' ')
                            {
                                checkSpacesMode = true;
                            }
                        }
                        iterator++;
                    }
                    if (!foundSpace)
                    {
                        break;
                    }
                }
                //This clears comments
                int index = fileLines[i].IndexOf("//");
                if (index > -1)
                {
                    fileLines[i] = fileLines[i].Substring(0, index);
                }
            }
            //This clears extra lines.
            int counter = 0;
            while (true)
            {
                if (fileLines[counter] == "" || fileLines[counter] == "\t")
                {
                    fileLines.RemoveAt(counter);
                }
                else
                {
                    counter++;
                    if (counter >= fileLines.Count)
                    {
                        break;
                    }
                }
            }
        }
        static void Main(string[] args)
        {
            string targetDirectory = "C:\\Users\\Sumner\\Documents\\School\\Spring2018\\nand2tetris\\projects\\07\\VMTranslator\\VMTranslator";
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
                        Console.WriteLine("\nDone");
                    }
                }
            }
            Console.WriteLine("\n\nPress any key...");
            Console.ReadKey();
        }
    }
}