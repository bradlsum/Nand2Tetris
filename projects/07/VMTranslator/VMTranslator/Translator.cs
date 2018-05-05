using System;
using System.Collections.Generic;
using System.IO;

namespace VMTranslator
{
    public static class Translator
    {
        private static List<string> fileLines = new List<string>();
        private static List<string> assembledLines = new List<string>();
        private static int currentTempLabel = 0;
        
        public static void Translate(string filename)
        {
            string line;

            StreamReader file = new StreamReader(filename);
            while ((line = file.ReadLine()) != null)
            {
                fileLines.Add(line);
            }
            file.Close();
            ClearWhitespace();

            for(int i = 0; i < fileLines.Count; i++)
            {
                DetermineCommandType(fileLines[i], i);
            }

            WriteToFile(filename);
            Console.Write("Press any key to close the VMTranslator");
            Console.ReadKey();
        }

        private static void WriteToFile(string filename)
        {
            string newFile = filename.Split('.')[0];
            newFile += ".asm";
            TextWriter tw = new StreamWriter(newFile);
            for (int i = 0; i < assembledLines.Count; i++)
            {
                tw.WriteLine(assembledLines[i]);
            }
            tw.Close();
        }

        private static void DetermineCommandType(string line, int linenumber)
        {
            string cmd = line.Split(' ')[0];
            cmd = cmd.ToLower();

            switch(cmd)
            {
                case "add":
                    WriteAdd(line);
                    break;
                case "sub":
                    WriteSub(line);
                    break;
                case "neg":
                    WriteNeg(line);
                    break;
                case "eq":
                    WriteEq(line);
                    break;
                case "gt":
                    WriteGt(line);
                    break;
                case "lt":
                    WriteLt(line);
                    break;
                case "and":
                    WriteAnd(line);
                    break;
                case "or":
                    WriteOr(line);
                    break;
                case "not":
                    WriteNot(line);
                    break;
                case "pop":
                    WritePop(line);
                    break;
                case "push":
                    WritePush(line);
                    break;
                default:
                    Console.WriteLine("Error on line {0}: {1}, invalid command type!", linenumber, line);
                    break;
            }
        }

        private static void WriteAdd(string line)//This will add (@SP-2) + (@SP-1)
        {
            WritePopD();//Pop first value from stack//A = SP
            assembledLines.Add("A=A-1");
            assembledLines.Add("M=M+D");//Do the addition and save the result at the same time
        }

        private static void WriteSub(string line)//This will subtract (@SP-2) - (@SP-1)
        {
            WritePopD();//Pop first value from stack//A = SP
            assembledLines.Add("A=A-1");
            assembledLines.Add("M=M-D");//Do the subtraction and save the result at the same time
        }

        private static void WriteNeg(string line)//This will negate the topmost stack value
        {
            assembledLines.Add("@SP");//Load SP
            assembledLines.Add("A=M-1");//Load location of SP into A - 1 to index the top of the stack
            assembledLines.Add("M=-M");//Negate that location
        }

        private static void WriteEq(string line)
        {
            WritePopD();//Pop first value from stack
            assembledLines.Add("@R15");//Load temp location
            assembledLines.Add("M=D");//Store value from stack
            WritePopD();//Pop second value from stack
            assembledLines.Add("@R15");//Load save location
            assembledLines.Add("A=M");//Load value from location into A
            assembledLines.Add("D=D-A");
            assembledLines.Add("@_" + currentTempLabel);
            currentTempLabel++;
            assembledLines.Add("D;JEQ");//Jump if it equals zero
            assembledLines.Add("@_" + currentTempLabel);
            currentTempLabel++;
            assembledLines.Add("D=0;JMP");//Not EQ, force jump to push false on stack//FALSE
            assembledLines.Add("(_" + (currentTempLabel-2) + ")");//Index to the first label
            assembledLines.Add("D=-1");//TRUE
            assembledLines.Add("(_" + (currentTempLabel-1) + ")");//Index to the second label
            WritePushD();//Push the result(D) onto the stack
        }

        private static void WriteGt(string line)//This will check if (@SP-2) > (@SP-1)
        {
            WriteSub(line);//Subtract the two values
            WritePopD();//Bring the value back into D
            assembledLines.Add("@_" + currentTempLabel);
            currentTempLabel++;
            assembledLines.Add("D;JGT");//Jump if greater than 0, meaning the left value (@SP-2) is greater than (@SP-1)
            assembledLines.Add("@_" + currentTempLabel);
            currentTempLabel++;
            assembledLines.Add("D=0;JMP");//FALSE; Force jump
            assembledLines.Add("(_" + (currentTempLabel - 2) + ")");//Index to the first label
            assembledLines.Add("D=-1");//TRUE
            assembledLines.Add("(_" + (currentTempLabel - 1) + ")");//Index to the second label
            WritePushD();//Push result to stack
        }

        private static void WriteLt(string line)//This will check if (@SP-2) < (@SP-1)
        {
            WriteSub(line);//Subtract the two values
            WritePopD();//Bring the value back into D
            assembledLines.Add("@_" + currentTempLabel);
            currentTempLabel++;
            assembledLines.Add("D;JLT");//Jump if less than 0, meaning the left value (@SP-2) is less than (@SP-1)
            assembledLines.Add("@_" + currentTempLabel);
            currentTempLabel++;
            assembledLines.Add("D=0;JMP");//FALSE; Force jump
            assembledLines.Add("(_" + (currentTempLabel - 2) + ")");//Index to the first label
            assembledLines.Add("D=-1");//TRUE
            assembledLines.Add("(_" + (currentTempLabel - 1) + ")");//Index to the second label
            WritePushD();//Push result to stack
        }

        private static void WriteAnd(string line)//This will AND (@SP-2) && (@SP-1)
        {
            WritePopD();//Pop first value from stack//A = SP
            assembledLines.Add("A=A-1");
            assembledLines.Add("M=M&D");//Do the AND and save the result at the same time
        }

        private static void WriteOr(string line)//This will OR (@SP-2) || (@SP-1)
        {
            WritePopD();//Pop first value from stack//A = SP
            assembledLines.Add("A=A-1");
            assembledLines.Add("M=M|D");//Do the OR and save the result at the same time
        }

        private static void WriteNot(string line)//This will NOT (@SP-2) 
        {
            assembledLines.Add("@SP");//Load SP
            assembledLines.Add("A=M-1");//Load location of SP into A - 1 to index the top of the stack
            assembledLines.Add("M=!M");//NOT that location
        }

        private static void WritePop(string line)
        {
            string[] cmd = line.Split(' ');//The second word should be the type of pop we are doing
            switch (cmd[1])
            {
                case "static"://Static has its own region not controlled by a general pointer
                    if(int.Parse(cmd[2]) < 240 && int.Parse(cmd[2]) >= 0)
                    {
                        assembledLines.Add("@16");//Load STATIC starting location
                        assembledLines.Add("D=A");//Save to D
                        assembledLines.Add("@" + cmd[2]);//Load the index of the static
                        assembledLines.Add("D=D+A");//Increment to the index
                        assembledLines.Add("@R15");//Load asm temp location
                        assembledLines.Add("M=D");//Save Index
                        WritePopD();//Pull top of stack into D
                        assembledLines.Add("@R15");//Load saved value location
                        assembledLines.Add("A=M");//Load saved value
                        assembledLines.Add("M=D");//Save to target mem location
                    }
                    else
                    {
                        Console.WriteLine("Error near line: " + line + ", static index out of range!");
                    }
                    break;
                case "this":
                    assembledLines.Add("@THIS");//Load THIS pointer
                    WritePopCommands(cmd[2]);
                    break;
                case "local":
                    assembledLines.Add("@LCL");//Load LCL pointer
                    WritePopCommands(cmd[2]);
                    break;
                case "argument":
                    assembledLines.Add("@ARG");//Load ARG pointer
                    WritePopCommands(cmd[2]);
                    break;
                case "that":
                    assembledLines.Add("@THAT");//Load THAT pointer
                    WritePopCommands(cmd[2]);
                    break;
                case "constant":
                    Console.WriteLine("Error near line: " + line + ", you cannot store values to a constant!");
                    break;
                case "pointer":
                    if (cmd[2] == "0")
                    {
                        WritePopD();
                        assembledLines.Add("@THIS");
                        assembledLines.Add("M=D");
                    }
                    else if (cmd[2] == "1")
                    {
                        WritePopD();
                        assembledLines.Add("@THAT");
                        assembledLines.Add("M=D");
                    }
                    break;
                case "temp":
                    if (int.Parse(cmd[2]) >= 0 && int.Parse(cmd[2]) < 8)//We index at 0, so 0-7 are the valid ranges for offset
                    {
                        assembledLines.Add("@5");//Load the start of temp
                        assembledLines.Add("D=A");//Save value
                        assembledLines.Add("@" + cmd[2]);//Add index
                        assembledLines.Add("D=D+A");//Add D + Index
                        assembledLines.Add("@R15");//Load temp location
                        assembledLines.Add("M=D");//Save target location
                        WritePopD();//Pull the topmost location for the D
                        assembledLines.Add("@R15");//Load saved temp
                        assembledLines.Add("A=M");//Pull location into A
                        assembledLines.Add("M=D");//Save D to location
                    }
                    else
                    {
                        Console.WriteLine("Error found near: " + line + ", temp index out of range!");
                    }
                    break;
                default:
                    Console.WriteLine("Error found near: " + line + ", invalid command type");
                    break;
            }
        }

        private static void WritePush(string line)
        {
            string[] cmd = line.Split(' ');//The second word should be the type of push we are doing
            switch (cmd[1])
            {
                case "static"://Static has its own region not controlled by a general pointer
                    assembledLines.Add("@16");//Load STATIC starting location
                    assembledLines.Add("D=A");//Load save to D
                    assembledLines.Add("@" + cmd[2]);//Load the index of the static
                    assembledLines.Add("D=D+A");//Increment to the index
                    assembledLines.Add("A=D");//Load that location into A
                    assembledLines.Add("D=M");//Load the value of THIS+Index
                    WritePushD();//Push the value onto the stack.
                    break;
                case "this":
                    assembledLines.Add("@THIS");//Load THIS pointer
                    WritePrepPush(cmd[2]);
                    WritePushD();//Push the value onto the stack.
                    break;
                case "local":
                    assembledLines.Add("@LCL");//Load LCL pointer
                    WritePrepPush(cmd[2]);
                    WritePushD();//Push the value onto the stack.
                    break;
                case "argument":
                    assembledLines.Add("@ARG");//Load ARG pointer
                    WritePrepPush(cmd[2]);
                    WritePushD();//Push the value onto the stack.
                    break;
                case "that":
                    assembledLines.Add("@THAT");//Load THAT pointer
                    WritePrepPush(cmd[2]);
                    WritePushD();//Push the value onto the stack.
                    break;
                case "constant":
                    if (int.Parse(cmd[2]) > 0 && int.Parse(cmd[2]) < 32768)
                    {
                        assembledLines.Add("@" + cmd[2]);
                        assembledLines.Add("D=A");
                        WritePushD();
                    }
                    else
                    {
                        Console.WriteLine("Error found near: " + line + ", constant out of range!");
                    }
                    break;
                case "pointer":
                    if (cmd[2] == "0")
                    {
                        assembledLines.Add("@THIS");//Load this ptr
                        assembledLines.Add("D=M");//Load value into D
                        WritePushD();//push onto stack
                    }
                    else if (cmd[2] == "1")
                    {
                        assembledLines.Add("@THAT");//Load that ptr
                        assembledLines.Add("D=M");//Load value into D
                        WritePushD();//push onto stack
                    }
                    break;
                case "temp":
                    if(int.Parse(cmd[2]) >= 0 && int.Parse(cmd[2]) < 8)//We index at 0, so 0-7 are the valid ranges for offset
                    {
                        assembledLines.Add("@5");
                        assembledLines.Add("D=A");
                        assembledLines.Add("@" + cmd[2]);
                        assembledLines.Add("D=D+A");
                        assembledLines.Add("A=D");
                        assembledLines.Add("D=M");
                        WritePushD();
                    }
                    else
                    {
                        Console.WriteLine("Error found near: " + line + ", temp index out of range!");
                    }
                    break;
                default:
                    Console.WriteLine("Error found near: " + line + ", invalid command type");
                    break;
            }
        }

        private static void WritePrepPush(string cmd)
        {
            assembledLines.Add("D=M");//Load A location
            assembledLines.Add("@" + cmd);//Load the index of A
            assembledLines.Add("D=D+A");//Increment to the index
            assembledLines.Add("A=D");//Load that location into A
            assembledLines.Add("D=M");//Load the value of A+Index
        }

        private static void WritePopCommands(string cmd)
        {
            assembledLines.Add("D=M");//Load A location
            assembledLines.Add("@" + cmd);//Load the index of A
            assembledLines.Add("D=D+A");//Increment to the index
            assembledLines.Add("@R15");//Load Temp location
            assembledLines.Add("M=D");//Save location
            WritePopD();//Pull the topmost value off the stack into D
            assembledLines.Add("@R15");//Open the location again
            assembledLines.Add("A=M");//Pull the location into A
            assembledLines.Add("M=D");//Save D to memory
        }

        private static void WritePopD()
        {
            assembledLines.Add("@SP");//Load Stack Pointer
            assembledLines.Add("AM=M-1");//Load the stack location in A
            assembledLines.Add("D=M");//Load the value at that location in the stack
        }

        private static void WritePushD()
        {
            assembledLines.Add("@SP");//Load Stack Pointer
            assembledLines.Add("A=M");//Load the SP location
            assembledLines.Add("M=D");//Put the value of D into the SP location
            assembledLines.Add("@SP");//Load Stack Pointer
            assembledLines.Add("M=M+1");//Increment the stack pointer
        }

        private static void ClearWhitespace()
        {
            for (int i = 0; i < fileLines.Count; i++)
            {
                //This clears leading whitespace
                int iterator = 0;
                int leadingWhiteSpaceEnd = 0;
                foreach(char c in fileLines[i])
                {
                    if (c != ' ')
                    {
                        leadingWhiteSpaceEnd = iterator;
                        break;
                    }
                    iterator++;
                }
                if(leadingWhiteSpaceEnd != 0)//Else there is no whitespace to clear
                {
                    fileLines[i] = fileLines[i].Substring(leadingWhiteSpaceEnd - 1, fileLines[i].Length - 1);
                }
                //This section will check for the number of commands as well as shave extra spaces in between commands
                while(true)
                {
                    bool checkSpacesMode = false;
                    bool foundSpace = false;
                    iterator = 0;
                    foreach (char c in fileLines[i])
                    {
                        if (checkSpacesMode)//WE are checking if there are consequtive spaces, if there are we need to remove them and restart this.
                        {//Ideally this will jump right back out of spaces mode, if it doesnt, then we have multiple spaces and this line needs a space stripped
                            if(c == ' ')
                            {
                                foundSpace = true;//This implies we need to restart so we will NOT break out of the while(true)
                                fileLines[i].Remove(iterator, 1);//remove this space, restart
                            }
                            else
                            {
                                if(foundSpace)//break and restart
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
                            if(c == ' ')
                            {
                                checkSpacesMode = true;
                            }
                        }
                        iterator++;
                    }
                    if(!foundSpace)
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
    }
}
