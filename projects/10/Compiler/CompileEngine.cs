using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace Jack_Compiler
{
    class CompileEngine
    {
        private static bool writeXML = false;
        private static bool writeTokens = false;
        private static TokenEngine tokenEngine;

        private static List<string> fileLines = new List<string>();
        private static List<string> tokenList = new List<string>();
        private static List<string> assembledLines = new List<string>();
        private static string fileName = "";
        private static string functionName = "";
        private static int indentLevel = 0;
        
        public static string[] tags;

        public static void CompileFolder(string foldername, string filename)
        {
            List<string> fileList = new List<string>();

            string[] fileEntries = Directory.GetFiles(foldername);//Load all of the files in the target dir
            foreach (string file in fileEntries)
            {
                if (file.Contains(".jack"))//Load all files that can be translated
                {
                    fileList.Add(file);
                }
            }

            for (int count = 0; count < fileList.Count; count++)
            {
                fileName = Path.GetFileName(fileList[count]);
                fileName = fileName.Split('.')[0];
                tokenEngine = new TokenEngine(fileLines[count]);
                CompileClass();
            }

            WriteToFile(foldername, filename);
            Console.Write("Press any key to close the Compiler");
            Console.ReadKey();
        }

        private static void WriteToFile(string foldername, string filename)
        {
            string newFile = foldername;
            
            newFile += "\\" + filename + ".jack";

            TextWriter tw = new StreamWriter(newFile);
            for (int i = 0; i < assembledLines.Count; i++)
            {
                tw.WriteLine(assembledLines[i]);
            }
            tw.Close();
        }

        public void Constructor()
        {

        }

        public static void CompileClass()
        {
            if (writeTokens)
            {
                OpenXMLTag("tokens");
                while (tokenEngine.HasMoreTokens())
                {
                    WriteXMLTag();
                }
                CloseXMLTag();
            }
            else
            {
                if (tokenEngine.GetKeyword() == "class")
                {
                    tokenEngine.Advance();
                    OpenXMLTag("class");

                    if (tokenEngine.GetKeyword() == "identifier")
                    {
                        CompileClassName();

                        if (tokenEngine.GetSymbol() == "{")
                        {
                            while (tokenEngine.HasMoreTokens())
                            {
                                if ((tokenEngine.GetKeyword() == "static") || (tokenEngine.GetKeyword() == "field"))
                                {
                                    WriteXMLTag();
                                    CompileClassVarDec();
                                    continue;
                                }
                                else if ((tokenEngine.GetKeyword() == "constructor") || (tokenEngine.GetKeyword() == "function") || (tokenEngine.GetKeyword() == "method"))
                                {
                                    CompileSubroutine();
                                    continue;
                                }
                                else if (tokenEngine.GetSymbol() == "}")
                                {
                                    WriteXMLTag();
                                    break;
                                }
                                else if (tokenEngine.GetKeyword() == "var")
                                {
                                    WriteXMLTag();
                                    CompileClassVarDec();
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("Expected '{' found: " + tokenEngine.GetUnknown());
                        }
                    }
                    CloseXMLTag();
                }
            }
        }

        public static void CompileClassVarDec()
        {
            CompileType();
            CompileClassName();

            if (tokenEngine.GetSymbol() == ",")
            {
                while (tokenEngine.GetSymbol() != ";")
                {
                    if (tokenEngine.GetTokenType().ToString() == "identifier")
                    {
                        CompileVarName();
                    }
                }
            }

            if(tokenEngine.GetSymbol() == ";")
            {
                WriteXMLTag();
            }
        }

        public static void CompileSubroutine()
        {
            OpenXMLTag("subRoutineDec");
            WriteXMLTag();//write the method, function, or constructor call
            if (Expect(TokenType.IDENTIFIER, false) || Expect(TokenType.KEYWORD, false))
            {
                Expect(TokenType.IDENTIFIER, true);//this can throw, the only option is indentifier here
                CompileParameterlist();
                CloseXMLTag();//subRoutineDec");
                OpenXMLTag("subRoutineBody");
                Expect(TokenType.SYMBOL, "{", true);
                while(true)
                {
                    if(tokenEngine.GetKeyword() == "var")
                    {
                        CompileVarDec();
                    }
                    else
                    {
                        break;
                    }
                }
                bool done = false;
                OpenXMLTag("statements");
                while(!done)
                {
                    if (tokenEngine.currentTokenType == TokenType.KEYWORD)
                    {
                        switch (tokenEngine.GetKeyword())
                        {
                            case "let":
                                CompileLet();
                                break;
                            case "do":
                                CompileDo();
                                break;
                            case "return":
                                CompileReturn();
                                Expect(TokenType.SYMBOL, "}", true);
                                done = true;
                                break;
                            case "if":
                                CompileIf();
                                break;
                            case "while":
                                CompileWhile();
                                break;
                            default:
                                throw new Exception("Expected 'let', 'do', or 'return' found: " + tokenEngine.GetUnknown());
                        }
                    }
                    else
                    {
                        throw new Exception("Expected KEYWORD found: " + tokenEngine.currentTokenType);
                    }
                }
                CloseXMLTag();
            }
            else
            {
                throw new Exception("Expected 'identifier' or 'keyword' found: " + tokenEngine.GetUnknown());
            }
            CloseXMLTag();
        }

        public static void CompileInnerSubroutine()
        {
            OpenXMLTag("ifElseOrWhileBody");
            bool done = false;
            OpenXMLTag("statements");
            while (!done)
            {
                if (tokenEngine.currentTokenType == TokenType.KEYWORD)
                {
                    switch (tokenEngine.GetKeyword())
                    {
                        case "let":
                            CompileLet();
                            break;
                        case "do":
                            CompileDo();
                            break;
                        case "if":
                            CompileIf();
                            break;
                        case "while":
                            CompileWhile();
                            break;
                        default:
                            throw new Exception("Expected 'let', 'do', 'while', or 'if found: " + tokenEngine.GetUnknown());
                    }
                }
                else
                {
                    Expect(TokenType.SYMBOL, "}", true);
                }
            }
            CloseXMLTag();
        }

        public static void CompileParameterlist()
        {
            OpenXMLTag("parameterList");
            Expect(TokenType.SYMBOL, "(", true);
            while(true)
            {
                if(Expect(TokenType.SYMBOL, ")", false))//This is primarily to check for no args
                {
                    break;
                }
                else
                {
                    Expect(TokenType.KEYWORD, true);//Look for string,bool,int, etc
                    Expect(TokenType.IDENTIFIER, true);
                    if(!Expect(TokenType.SYMBOL, ",", false))//Check for ',' to look for more args
                    {
                        Expect(TokenType.SYMBOL, ")", true);//If no ',' then we MUST find the end or there is an issue
                        break;
                    }
                }
            }
            CloseXMLTag();
        }

        public static void CompileArgList()
        {
            OpenXMLTag("argList");
            Expect(TokenType.SYMBOL, "(", true);
            while (true)
            {
                if (Expect(TokenType.SYMBOL, ")", false))//This is primarily to check for no args
                {
                    break;
                }
                else
                {
                    if(!Expect(TokenType.IDENTIFIER, false) && !Expect(TokenType.STRING_CONST, false) && !Expect(TokenType.INT_CONST, false))
                    {
                        throw new Exception("Expected 'IDENTIFIER' or 'STRING' or 'INT' found: " + tokenEngine.GetUnknown());
                    }
                    else//Check for ',' to look for more args
                    {
                        if(!Expect(TokenType.SYMBOL, ",", false))
                        {
                            Expect(TokenType.SYMBOL, ")", true);//If no ',' then we MUST find the end or there is an issue
                            break;
                        }
                    }
                }
            }
            CloseXMLTag();
        }

        public static void CompileVarDec()
        {
            OpenXMLTag("varDec");
            Expect(TokenType.KEYWORD, "var", true);
            if(tokenEngine.IsType())
            {
                WriteXMLTag();
                while (true)
                {
                    Expect(TokenType.IDENTIFIER, true);//This is primarily to check for no args
                    if(Expect(TokenType.SYMBOL, ";", false))
                    {
                        break;
                    }
                    else
                    {
                        Expect(TokenType.SYMBOL, ",", true); //Break if we didnt find a ',' or ';'
                    }
                }
            }
            else
            {
                throw new Exception("Expected 'SYMBOL' of type 'TYPE' found: " + tokenEngine.GetUnknown());
            }
            CloseXMLTag();
        }

        public static void CompileDo()
        {
            OpenXMLTag("doStatement");
            Expect(TokenType.KEYWORD, "do", true);
            Expect(TokenType.IDENTIFIER, true);
            Expect(TokenType.SYMBOL, ".", true);
            Expect(TokenType.IDENTIFIER, true);
            CompileArgList();
            CloseXMLTag();
        }

        public static void CompileLet()
        {
            OpenXMLTag("letStatement");
            Expect(TokenType.KEYWORD, "let", true);
            Expect(TokenType.IDENTIFIER, true);
            Expect(TokenType.SYMBOL, "=", true);
            CompileExpression(";");
            CloseXMLTag();
        }

        public static void CompileWhile()
        {
            OpenXMLTag("whileLoop");
            Expect(TokenType.KEYWORD, "while", true);
            CompileExpression(")");
            CompileInnerSubroutine();
            CloseXMLTag();
        }

        public static void CompileReturn()
        {
            Expect(TokenType.KEYWORD, "return", true);
            if (!Expect(TokenType.IDENTIFIER, false))
            {
                Expect(TokenType.SYMBOL, ";", true);
            }
        }

        public static void CompileIf()
        {
            OpenXMLTag("ifStatement");
            Expect(TokenType.KEYWORD, "if", true);
            CompileExpression(")");
            CompileInnerSubroutine();
            CloseXMLTag();
            if(Expect(TokenType.KEYWORD, "else", false))
            {
                OpenXMLTag("elseStatement");
                CompileInnerSubroutine();
                CloseXMLTag();
            }
        }

        public static void CompileExpression(string termination)
        {
            OpenXMLTag("expression");
            bool done = false;
            Expect(TokenType.SYMBOL, termination, true);//If we immediately have a termination we have an issue
            while (!done)
            {
                OpenXMLTag("term");
                if (Expect(TokenType.SYMBOL, "(", false))//If we dont have a nested expression
                {
                    CompileExpression(")");
                }
                else
                {
                    //if(Expect())
                }
                CloseXMLTag();
            }
        }

        public void CompileTerm()
        {
            Expect(TokenType.INT_CONST, false);
            Expect(TokenType.STRING_CONST, false);
            Expect(TokenType.KEYWORD, false);
            if (Expect(TokenType.IDENTIFIER, false))
            {
                if (Expect(TokenType.SYMBOL, "[", false))
                    CompileExpression("}");
            }

            //Expect(TokenType.)


        }

        public static void CompileExpressionList()
        {

        }

        public static void CompileIdentifier()
        {
            if (tokenEngine.GetTokenType() == TokenType.IDENTIFIER)
            {
                WriteXMLTag();
            }
        }

        public static void CompileType()
        {
            if (tokenEngine.GetKeyword() == "int" || tokenEngine.GetKeyword() == "char" || tokenEngine.GetKeyword() == "boolean" || tokenEngine.GetTokenType() == TokenType.IDENTIFIER)
            {
                WriteXMLTag();
            }
        }

        public static void CompileClassName()
        {
            CompileIdentifier();
        }

        public static void SubroutineName()
        {
            CompileIdentifier();
        }

        public static void CompileVarName()
        {
            CompileIdentifier();
        }

        private static bool Expect(TokenType type, string value, bool allowThrow)
        {
            if (tokenEngine.currentTokenType == type)
            {
                switch(type)
                {
                    case TokenType.IDENTIFIER:
                        if(tokenEngine.GetIdentifier() == value)
                        {
                            WriteXMLTag();
                        }
                        else
                        {
                            if (allowThrow)
                            {
                                throw new Exception("Expected value '" + value + "' found: " + tokenEngine.GetUnknown());
                            }
                            else
                            {
                                return false;
                            }
                        }
                        break;
                    case TokenType.INT_CONST:
                        if (tokenEngine.GetIntVal() == int.Parse(value))
                        {
                            WriteXMLTag();
                        }
                        else
                        {
                            if (allowThrow)
                            {
                                throw new Exception("Expected value '" + value + "' found: " + tokenEngine.GetUnknown());
                            }
                            else
                            {
                                return false;
                            }
                        }
                        break;
                    case TokenType.KEYWORD:
                        if (tokenEngine.GetKeyword() == value)
                        {
                            WriteXMLTag();
                        }
                        else
                        {
                            if (allowThrow)
                            {
                                throw new Exception("Expected value '" + value + "' found: " + tokenEngine.GetUnknown());
                            }
                            else
                            {
                                return false;
                            }
                        }
                        break;
                    case TokenType.STRING_CONST:
                        if (tokenEngine.GetStringVal() == value)
                        {
                            WriteXMLTag();
                        }
                        else
                        {
                            if (allowThrow)
                            {
                                throw new Exception("Expected value '" + value + "' found: " + tokenEngine.GetUnknown());
                            }
                            else
                            {
                                return false;
                            }
                        }
                        break;
                    case TokenType.SYMBOL:
                        if (tokenEngine.GetSymbol() == value)
                        {
                            WriteXMLTag();
                        }
                        else
                        {
                            if (allowThrow)
                            {
                                throw new Exception("Expected value '" + value + "' found: " + tokenEngine.GetUnknown());
                            }
                            else
                            {
                                return false;
                            }
                        }
                        break;
                    default:
                        if (allowThrow)
                        {
                            throw new Exception("Invalid TokenType passed to Expect");
                        }
                        else
                        {
                            return false;
                        }
                }
                WriteXMLTag();
                return true;
            }
            else
            {
                if (allowThrow)
                {
                    throw new Exception("Expected '" + type + "' found: " + tokenEngine.GetUnknown());
                }
                else
                {
                    return false;
                }
            }
        }

        private static bool Expect(TokenType type, bool allowThrow)
        {
            if(tokenEngine.currentTokenType == type)
            {
                WriteXMLTag();
                return true;
            }
            else
            {
                if (allowThrow)
                {
                    throw new Exception("Expected TokenType '" + type + "' found: " + tokenEngine.currentTokenType);
                }
                else
                {
                    return false;
                }
            }
        }

        public static string Indent()
        {
            string temp = "";
            for(int i = 0; i < indentLevel; i++)
            {
                temp += "\t";
            }
            return temp;
        }

        public static void WriteXMLTag()
        {
            if (writeXML == true)
            {
                string temp = Indent();
                assembledLines.Add(temp + '<' + tokenEngine.GetTokenType().ToString() + '>' + tokenEngine.GetStringVal() + "</" + tokenEngine.GetTokenType().ToString() + '>' + '\n');
                tokenEngine.Advance();
            }
        }

        public static void OpenXMLTag(string tag)
        {
            if (writeXML == true)
            {
                string temp = Indent();
                assembledLines.Add(temp + '<' + tag + '>' + '\n');
                indentLevel++;
                tags[indentLevel] = tag;
                tokenEngine.Advance();
            }
        }

        public static void CloseXMLTag()
        {
            if (writeXML == true)
            {
                indentLevel--;
                string temp = Indent();
                assembledLines.Add(temp + "</" + tags[indentLevel+1] + '>' + '\n');
            }
        }
    }
}
