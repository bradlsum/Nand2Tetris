using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace Jack_Compiler
{
    public enum TokenType { KEYWORD, SYMBOL, IDENTIFIER, INT_CONST, STRING_CONST, UNKNOWN };

    public class TokenEngine
    {
        public int currentTokenIndex;
        public TokenType currentTokenType;
        private static List<string> fileLines = new List<string>();
        private static List<string> tokenList = new List<string>();

        public static List<string> keywords = new List<string>()
        {
            "class",
            "method",
            "function",
            "constructor",
            "int",
            "boolean",
            "char",
            "void",
            "static",
            "field",
            "let",
            "do",
            "if",
            "else",
            "while",
            "return",
            "true",
            "false",
            "null",
            "this",
            "string"
        };

        private static List<string> types = new List<string>()
        {
            "int",
            "char",
            "boolean",
            "string"
        };

        private static List<string> operators = new List<string>()
        {
            "+",
            "-",
            "*",
            "/",
            "&",
            "|",
            "<",
            ">",
            "=",
            "~"
        };

        private static List<string> symbols = new List<string>()
        {
            "{",
            "}",
            "[",
            "]",
            "(",
            ")",
            ".",
            ",",
            ";"
        };

        public TokenEngine(string fileName)
        {
            fileLines.Clear();
            StreamReader file = new StreamReader(fileName);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                fileLines.Add(line);
            }
            file.Close();
            ClearWhitespace();
            GenerateTokenList();
            currentTokenIndex = 0;
            currentTokenType = GetTokenType();
        }

        public bool HasMoreTokens()
        {
            if(currentTokenIndex + 1 != tokenList.Count)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Advance()
        {
            currentTokenIndex++;
            currentTokenType = GetTokenType();
        }

        public void Retreat()
        {
            currentTokenIndex--;
            currentTokenType = GetTokenType();
        }

        public TokenType GetTokenType()
        {
            int testValue = 0;
            string keywordtest = tokenList[currentTokenIndex].ToLower();
            if(keywords.Contains(keywordtest))
            {
                return TokenType.KEYWORD;
            }
            else if(symbols.Contains(tokenList[currentTokenIndex]) || operators.Contains(tokenList[currentTokenIndex]))
            {
                return TokenType.SYMBOL;
            }
            else if(int.TryParse(tokenList[currentTokenIndex], out testValue))
            {
                return TokenType.INT_CONST;
            }
            else if(tokenList[currentTokenIndex][0] == '\"')
            {
                return TokenType.STRING_CONST;
            }
            else if(Regex.IsMatch(tokenList[currentTokenIndex], @"^[a-zA-Z]+$"))
            {
                return TokenType.IDENTIFIER;
            }
            else
            {
                return TokenType.UNKNOWN;
            }
        }

        public string GetKeyword()
        {
            if (keywords.Contains(tokenList[currentTokenIndex].ToLower()))
            {
                return tokenList[currentTokenIndex].ToLower();
            }
            else
            {
                return null;
            }
        }

        public string GetSymbol()
        {
            if (symbols.Contains(tokenList[currentTokenIndex]) || operators.Contains(tokenList[currentTokenIndex]))
            {
                return tokenList[currentTokenIndex];
            }
            else
            {
                return null;
            }
        }

        public string GetIdentifier()
        {
            if(Regex.IsMatch(tokenList[currentTokenIndex], @"^[a-zA-Z]+$"))
            {
                return tokenList[currentTokenIndex];
            }
            else
            {
                return null;
            }
        }

        public int GetIntVal()
        {
            int testValue;
            if (int.TryParse(tokenList[currentTokenIndex], out testValue))
            {
                return testValue;
            }
            else
            {
                return -128000;
            }
        }

        public bool IsType()
        {
            if (types.Contains(tokenList[currentTokenIndex]))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public string GetTypeVal()
        {
            if (types.Contains(tokenList[currentTokenIndex]))
            {
                return tokenList[currentTokenIndex];
            }
            else
            {
                return null;
            }
        }

        public string GetStringVal()
        {
            if (tokenList[currentTokenIndex][0] == '\"')
            {
                return tokenList[currentTokenIndex].Replace("\"", "");
            }
            else
            {
                return null;
            }
        }

        public string GetUnknown()
        {
            return tokenList[currentTokenIndex];
        }

        private static void ClearWhitespace()
        {
            //Clear Comments
            for (int i = 0; i < fileLines.Count; i++)
            {
                int index = fileLines[i].IndexOf("//");
                if (index > -1)
                {
                    fileLines[i] = fileLines[i].Substring(0, index);
                }
            }
            RemoveBlockComments();
            //This clears extra lines.
            int counter = 0;
            while (true)
            {
                if (fileLines[counter] == "" || fileLines[counter] == "\t" || fileLines[counter] == "\r")
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

        private static void GenerateTokenList()
        {
            for (int i = 0; i < fileLines.Count; i++)
            {
                foreach (string op in operators)
                {
                    fileLines[i] = fileLines[i].Replace(op, " " + op + " ");//Add spaces to help with separation
                }
                foreach (string op in symbols)
                {
                    fileLines[i] = fileLines[i].Replace(op, " " + op + " ");//Add spaces to help with separation
                }
                List<string> lineTokens = fileLines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                tokenList.AddRange(lineTokens);
            }
        }

        private static void RemoveBlockComments()
        {
            while (true)
            {
                bool foundBlock = false;
                int startline = -1, endline = -1;
                for (int i = 0; i < fileLines.Count; i++)
                {
                    if (!foundBlock)
                    {
                        if (fileLines[i].Contains("/*"))
                        {
                            if (fileLines[i].Contains("*/"))
                            {
                                startline = i;
                                endline = i;
                                foundBlock = true;
                                break;
                            }
                            else
                            {
                                startline = i;
                                foundBlock = true;
                            }
                        }
                    }
                    else
                    {
                        if (fileLines[i].Contains("*/"))
                        {
                            endline = i;
                            break;
                        }
                    }
                }
                if (foundBlock)
                {
                    if (endline != -1)
                    {
                        fileLines.RemoveRange(startline, (1 + (endline - startline)));
                    }
                    else
                    {
                        Console.WriteLine("Error! Block comment was NOT closed!");
                        return;
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }
}
