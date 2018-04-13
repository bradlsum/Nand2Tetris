/*
By Sumner J Bradley
Date 3/20/18
*/

using System;
using System.IO;
using System.Collections.Generic;

public static class Assembler
{
    static void Assemble(StreamReader read, StreamWriter write)
    {
        Dictionary<string, int> sTable = new Dictionary<string, int>
        {
            { "SP", 0 },
            { "LCL", 1 },
            { "ARG", 2 },
            { "THIS", 3 },
            { "SCREEN", 16384 },
            { "KBD", 24576 }
        };

        for (int i = 0; i < 15; i++)
        {
            sTable["R" + i] = i;
        }

        Dictionary<string, int> lTable = new Dictionary<string, int>();

        // First allocated symbol
        int lIndex = 0, lCount = 0, sIndex = 16;
        List<string> lines = new List<string>();
        {
            string line;
            while ((line = read.ReadLine()) != null)
            {
                line = line.Trim();
                int ind = line.IndexOf("//");
                if (ind != -1)
                    line = line.Substring(0, ind).Trim();

                if (line.Length <= 1)
                    continue;

                lines.Add(line);
            }
        }

        foreach (string line in lines)
        {
            if (line[0] == '(' && line[line.Length - 1] == ')')
            {
                string label = line.Substring(1, line.Length - 2);
                // Throw exception
                if (lTable.ContainsKey(label)) throw new exep("Duplicate: " + label + " on line " + lIndex);
                lTable[label] = lCount;
            }
            else lCount++;
        }

        foreach (string line in lines)
        {
            if (line[0] == '@' && char.IsLetter(line[1]))
            {
                string symbol = line.Substring(1);
                if (lTable.ContainsKey(symbol))
                    continue;
                if (!sTable.ContainsKey(symbol))
                    sTable[symbol] = sIndex++;
            }
            lIndex++;
        }

        //Process into assembly
        lIndex = 0;
        foreach (string line in lines)
        {
            if (line.Length <= 1 || line[0] == '(') continue;
            int val;
            if (line[0] == '@')
            {
                string lValue = line.Substring(1);
                if (char.IsDigit(line[1])) val = int.Parse(lValue);
                else if (lTable.ContainsKey(lValue)) val = lTable[lValue];
                else if (sTable.ContainsKey(lValue)) val = sTable[lValue];
                // Throw exeption
                else throw new exep("Unexpected symbol: " + lValue + " at line " + lIndex);
            }
            else
            {
                val = (1 << 15 | 1 << 14 | 1 << 13);
                string alocation = string.Empty, destination = string.Empty, jump = string.Empty;
                {
                    string[] bits = line.Split(';');
                    // Throw exeption
                    if (bits.Length == 0) throw new exep("Empty command at line " + lIndex);
                    if (bits.Length > 1) jump = bits[1];
                    bits = bits[0].Split('=');
                    if (bits.Length == 1)
                    {
                        destination = string.Empty;
                        alocation = bits[0];
                    }
                    else if (bits.Length == 2)
                    {
                        destination = bits[0];
                        alocation = bits[1];
                    }
                    else
                        throw new exep("Syntax error on line " + lIndex);
                }

                foreach (char C in destination)
                {
                    switch (C)
                    {
                        case 'A':
                            val |= (1 << 5);
                            break;
                        case 'D':
                            val |= (1 << 4);
                            break;
                        case 'M':
                            val |= (1 << 3);
                            break;
                        case '0':
                            //val |= 0
                            break;
                        default:
                            throw new exep("Unknown destination: '" + C + "'' in '" + destination + "' on line " + lIndex);
                    }
                }

                switch (jump)
                {
                    case "":
                        break;
                    case "JGT":
                        val |= 1;
                        break;
                    case "JEQ":
                        val |= 2;
                        break;
                    case "JGE":
                        val |= 3;
                        break;
                    case "JLT":
                        val |= 4;
                        break;
                    case "JNE":
                        val |= 5;
                        break;
                    case "JLE":
                        val |= 6;
                        break;
                    case "JMP":
                        val |= 7;
                        break;
                    default:
                        throw new exep("Unknown jump: '" + jump + "' on line " + lIndex);
                }

                if (alocation.Contains("M"))
                    val |= (1 << 12);

                switch (alocation)
                {
                    case "":
                        break;
                    case "0":
                        val |= 2688;
                        break;
                    case "1":
                        val |= 4032;
                        break;
                    case "-1":
                        val |= 3712;
                        break;
                    case "D":
                        val |= 768;
                        break;
                    case "A":
                    case "M":
                        val |= 3072;
                        break;
                    case "!D":
                        val |= 832;
                        break;
                    case "!A":
                    case "!M":
                        val |= 3136;
                        break;
                    case "-D":
                        val |= 960;
                        break;
                    case "-A":
                    case "-M":
                        val |= 3264;
                        break;
                    case "D+1":
                        val |= 1984;
                        break;
                    case "A+1":
                    case "M+1":
                        val |= 3520;
                        break;
                    case "D-1":
                        val |= 896;
                        break;
                    case "A-1":
                    case "M-1":
                        val |= 3200;
                        break;
                    case "D+A":
                    case "D+M":
                        val |= 128;
                        break;
                    case "D-A":
                    case "D-M":
                        val |= 1216;
                        break;
                    case "A-D":
                    case "M-D":
                        val |= 448;
                        break;
                    case "D&A":
                    case "D&M":
                        //val |= 0;
                        break;
                    case "D|A":
                    case "D|M":
                        val |= 1344;
                        break;
                    default:
                        throw new exep("Unknown expression: '" + alocation + "' on line " + lIndex);
                }
            }

            string value = Convert.ToString(val, 2);
            string fLine = new string('0', 16 - value.Length) + value;

            write.WriteLine(fLine);

            lIndex++;

            /*Console.WriteLine(line);
            Console.WriteLine(fLine);*/
        }
    }

    class exep : Exception
    {
        public exep(string message) : base(message) {}
    }

    public static void Main(string[] args)
    {
        string targetDirectory = "C:\\Users\\Playe\\OneDrive\\Documents\\School\\spring2018\\nand2tetris\\projects\\06\\ConsoleApp1\\ConsoleApp1";
        // Process the list of files found in the directory.
        string[] fileEntries = Directory.GetFiles(targetDirectory);
        foreach (string fileName in fileEntries)
        {
            if (fileName.Contains(".asm"))
            {
                try
                {
                    using (var writer = new StreamWriter(Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + ".hack")))
                    {
                        using (var reader = new StreamReader(fileName))
                        {
                            Assemble(reader, writer);
                        }
                    }
                }
                catch (exep e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        // Process finished
        Console.Write("Done, press any key!");
        Console.ReadKey();
    }
}