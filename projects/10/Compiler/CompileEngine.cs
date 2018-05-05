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

        public static StreamWriter XMLOutFile;
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
            }

            WriteToFile(foldername, filename);
            Console.Write("Press any key to close the VMTranslator");
            Console.ReadKey();
        }

        public CompileEngine(string file, StreamWriter VMOutFile, StreamWriter XMLOutFile, bool includeSource, bool tokensOnly)
        {
            tokenEngine = new TokenEngine(file);
            if (XMLOutFile != null)
            {

            }
        }

        private static void WriteToFile(string foldername, string filename)
        {
            string newFile = foldername;

            // This should be VM?
            newFile += "\\" + filename + ".asm";

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

        public void CompileClass()
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

        public void CompileClassVarDec()
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

        public void CompileSubroutine()
        {
            OpenXMLTag("subRoutineDec");
            WriteXMLTag();//write the method, function, or constructor call
            //Expect();
            if(tokenEngine.GetTokenType() == TokenType.IDENTIFIER)//means we wrote constructor
            {
                WriteXMLTag();//Write the indentifier

            }
            else if(tokenEngine.GetTokenType() == TokenType.KEYWORD)//return type
            {
                WriteXMLTag();
                if (tokenEngine.GetTokenType() == TokenType.IDENTIFIER)//means we wrote constructor
                {
                    WriteXMLTag();//Write the indentifier
                    if(tokenEngine.GetTokenType() == TokenType.SYMBOL && tokenEngine.GetSymbol() == "(")
                    {
                        WriteXMLTag();
                    }
                }
                else
                {
                    throw new Exception("Expected 'identifier' found: " + tokenEngine.GetUnknown());
                }

            }
            else
            {
                throw new Exception("Expected 'identifier' or 'keyword' found: " + tokenEngine.GetUnknown());
            }

            CloseXMLTag();
        }

        public void CompileParameterlist()
        {

        }

        public void CompileVarDec()
        {

        }

        public void CompileStatements()
        {

        }

        public void CompileDo()
        {

        }

        public void CompileLet()
        {
            OpenXMLTag("letStatement");
            WriteXMLTag();
            WriteXMLTag();
            WriteXMLTag();
            CloseXMLTag();
        }

        public void CompileWhile()
        {

        }

        public void ComplieReturn()
        {

        }

        public void CompileIf()
        {

        }

        public void CompileExpression()
        {

        }

        public void CompileTerm()
        {

        }

        public void CompileExpressionList()
        {

        }

        public void CompileIdentifier()
        {
            if (tokenEngine.GetTokenType() == TokenType.IDENTIFIER)
            {
                WriteXMLTag();
            }
        }

        public void CompileType()
        {
            if (tokenEngine.GetKeyword() == "int" || tokenEngine.GetKeyword() == "char" || tokenEngine.GetKeyword() == "boolean" || tokenEngine.GetTokenType() == TokenType.IDENTIFIER)
            {
                WriteXMLTag();
            }
        }

        public void CompileClassName()
        {
            CompileIdentifier();
        }

        public void SubroutineName()
        {
            CompileIdentifier();
        }

        public void CompileVarName()
        {
            CompileIdentifier();
        }

        private bool Expect(TokenType type, string value, bool allowThrow)
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

        private bool Expect(TokenType type, bool allowThrow)
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

        public void Indent()
        {
            for(int i = 0; i < indentLevel; i++)
            {
                XMLOutFile.Write('\t');
            }
        }

        public void WriteXMLTag()
        {
            if (writeXML == true)
            {
                Indent();
                XMLOutFile.Write('<' + tokenEngine.GetTokenType().ToString() + '>' + tokenEngine.GetStringVal() + "</" + tokenEngine.GetTokenType().ToString() + '>' + '\n');
                tokenEngine.Advance();
            }
        }

        public void OpenXMLTag(string tag)
        {
            if (writeXML == true)
            {
                Indent();
                XMLOutFile.Write('<' + tag + '>' + '\n');
                indentLevel++;
                tags[indentLevel] = tag;
                tokenEngine.Advance();
            }
        }

        public void CloseXMLTag()
        {
            if (writeXML == true)
            {
                indentLevel--;
                Indent();
                XMLOutFile.Write("</" + tags[indentLevel+1] + '>' + '\n');
            }
        }
    }
}
