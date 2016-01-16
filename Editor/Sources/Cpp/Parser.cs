/*
 * Copyright (c) 2015 Rémi Saurel
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Petri.Editor
{
    static class StringExtensions
    {
        public static string TrimSpaces(this String s)
        {
            s = s.Replace('\t', ' ');
            while(s.Contains("  "))
                s = s.Replace("  ", " ");

            s = s.Replace("\n ", "\n");

            while(s.Contains("\n\n"))
                s = s.Replace("\n\n", "\n");

            return s;
        }
    }

    namespace Cpp
    {
        public class Parser
        {
            public static string Preprocess(String s)
            {
                s = s.Replace('\t', ' ');

                var lines = s.Split('\n');

                // Removing preprocessor directives and one-line comments
                for(int i = 0; i < lines.Length; ++i) {
                    if(lines[i].StartsWith("#")) {
                        lines[i] = "";
                        continue;
                    }

                    int index = lines[i].IndexOf("//");
                    if(index != -1)
                        lines[i] = lines[i].Substring(0, index); 
                }

                s = String.Join("\n", lines);

                // Removing multi-lines comments
                string s2 = "";
                bool inString = false;
                int commentStart = -1;
                for(int i = 0; i < s.Length; ++i) {
                    char c = s[i];
                    if(c == '"' && commentStart == -1)
                        inString = !inString;

                    if(commentStart == -1 && !inString && c == '/' && i < s.Length - 1 && s[i + 1] == '*') {
                        commentStart = i;
                    }

                    if(commentStart != -1 && c == '*' && i < s.Length - 1 && s[i + 1] == '/') {
                        ++i;
                        commentStart = -1;
                        continue;
                    }

                    if(commentStart == -1)
                        s2 += c;
                }
                s = s2;

                // Removing leading and trailling whitespaces 
                while(s.Contains("\n "))
                    s = s.Replace("\n ", "\n");
                while(s.Contains(" \n"))
                    s = s.Replace(" \n", "\n");

                while(s.Contains("\n\n"))
                    s = s.Replace("\n\n", "\n");

                // Trying to put template declarations on the same line that class declarations
                //s.Replace("\nclass", "class");
                //s.Replace("\nclass", "class");

                s.Replace(";", ";\n");

                while(s.Contains("  "))
                    s = s.Replace("  ", " ");

                s.Replace(" :", ":");
                s.Replace(": ", ":");
					
                s = s.Replace("{", "\n{\n");
                s = s.Replace("}", "\n}\n");


                while(s.Contains("\n\n"))
                    s = s.Replace("\n\n", "\n");

                return s;
            }

            enum Visibility
            {
Public,
                Protected,
                Private}

            ;

            public static List<Function> Parse(Language language, string filename)
            {
                var functions = new List<Function>();

                System.IO.StreamReader file = System.IO.File.OpenText(filename);
                string s = file.ReadToEnd();
                s = Preprocess(s);

                string[] lines = s.Split('\n');

                // The tuple contains the current Scope as its first element, current scope's braces nesting level and visibility (only used into classes and structs)
                var currentScope = new Stack<Tuple<Scope, int, Visibility>>();
                currentScope.Push(new Tuple<Scope, int, Visibility>(null, 0, Visibility.Public));
                int braces = 0;
                foreach(string l in lines) {
                    if(l == "{")
                        ++braces;
                    else if(l == "}") {
                        if(currentScope.Count > 1 && currentScope.Peek().Item2 == braces)
                            currentScope.Pop();
                        --braces;
                    }

                    var match = Regex.Match(l, ClassDeclaration);
                    if(!l.EndsWith(";") && match.Success) {
                        Visibility vis = Visibility.Public;
                        if(l.StartsWith("class"))
                            vis = Visibility.Private;
                        currentScope.Push(Tuple.Create(Scope.MakeFromClass(new Type(language, match.Groups["name"].Value, currentScope.Peek().Item1)), braces + 1, vis));
                    }

                    match = Regex.Match(l, NamespaceDeclaration);
                    if(match.Success) {
                        currentScope.Push(Tuple.Create(Scope.MakeFromScopes(currentScope.Peek().Item1, Scope.MakeFromNamespace(match.Groups["name"].Value, currentScope.Peek().Item1)), braces + 1, Visibility.Public));
                    }

                    // If we are in a class, change the visibility of its members according to their visibility specifiers
                    if(currentScope.Count > 1) {
                        Visibility vis = currentScope.Peek().Item3;
                        if(l.StartsWith("public:"))
                            vis = Visibility.Public;
                        if(l.StartsWith("protected:"))
                            vis = Visibility.Protected;
                        if(l.StartsWith("private:"))
                            vis = Visibility.Private;
                        if(vis != currentScope.Peek().Item3) {
                            var tup = currentScope.Pop();
                            tup = Tuple.Create(tup.Item1, tup.Item2, vis);
                            currentScope.Push(tup);
                        }
                    }

                    // Make sure that we don't go inside a function or method body
                    if(braces == currentScope.Peek().Item2) {
                        if(currentScope.Count == 1 || currentScope.Peek().Item1.IsNamespace || (l.Contains("static") && currentScope.Peek().Item3 == Visibility.Public)) {
                            var function = ParseFunction(language, l, currentScope.Peek().Item1, filename);
                            if(function != null) {
                                functions.Add(function);
                            }
                        }
                        else if(currentScope.Peek().Item3 == Visibility.Public) {
                            var m = ParseMethod(language, l, filename, currentScope.Peek().Item1.Class);
                            if(m != null)
                                functions.Add(m);
                        }
                    }
                }

                return functions;
            }

            public static string NamePattern {
                get {
                    return "(?<name>[a-zA-Z_][a-zA-Z0-9_]*)";
                }
            }

            public static string NumberPattern {
                get {
                    return @"(?<number>([0-9]+\.?)|(\.[0-9]+)|([0-9]+\.[0-9]+))";
                }
            }

            public static string TemplatePattern {
                get {
                    // Matches C++ template specialization of a type (which may contain (), <>, :, letters, numbers, white spaces…
                    // Probably not complete due to horrible syntax disambiguation needed…
                    return @"(?<template>([ ]*<([^>]*>[ ]*([> ]|::[ ]*[a-zA-Z0-9_()]*)*))?)";
                }
            }

            public static string DeclarationEndPattern {
                get {
                    return @" ?(;|{)[.]*";
                }
            }

            public static string ClassDeclaration {
                get {
                    return "^(class|struct) " + NamePattern;
                }
            }

            public static string NamespaceDeclaration {
                get {
                    return "^(namespace) " + NamePattern;
                }
            }

            public static Function ParseFunction(Language language, string l, Scope enclosing, string filename)
            {
                System.Text.RegularExpressions.Match match = Function.Regex.Match(l);

                if(match.Success) {
                    Function function = new Function(new Type(language, match.Groups["type"].Value), enclosing, match.Groups["name"].Value, match.Groups["template"].Value != "");

                    Regex param = new Regex(@" ?" + Type.RegexPattern + @" ?(" + NamePattern + ")?");
                    string[] parameters = match.Groups["parameters"].Value.Split(',');

                    foreach(string parameter in parameters) {
                        System.Text.RegularExpressions.Match paramMatch = param.Match(parameter);
                        if(paramMatch.Success) {
                            Param p = new Param(new Type(language, paramMatch.Groups["type"].Value), paramMatch.Groups["name"].Value);
                            function.AddParam(p);
                        }
                    }

                    function.Header = filename;

                    return function;
                }

                return null;
            }

            public static Function ParseMethod(Language language, string l, string filename, Type classType)
            {
                var match = Method.Regex.Match(l);

                if(match.Success) {
                    var meth = new Method(classType, new Type(language, match.Groups["type"].Value), match.Groups["name"].Value, match.Groups["attributes"].Value, match.Groups["template"].Value != "");

                    Regex param = new Regex(@" ?" + Type.RegexPattern + @" ?(" + NamePattern + ")?");
                    string[] parameters = match.Groups["parameters"].Value.Split(',');

                    foreach(string parameter in parameters) {
                        System.Text.RegularExpressions.Match paramMatch = param.Match(parameter);
                        if(paramMatch.Success) {
                            Param p = new Param(new Type(language, paramMatch.Groups["type"].Value), paramMatch.Groups["name"].Value);
                            meth.AddParam(p);
                        }
                    }

                    meth.Header = filename;

                    return meth;
                }

                return null;
            }

            public static Tuple<Scope, string> ExtractScope(Language language, string name)
            {
                int index = name.LastIndexOf(Scope.GetSeparator(language));
                Tuple<Scope, string> tup;
                if(index == -1)
                    return new Tuple<Scope, string>(null, name);
                else
                    tup = ExtractScope(language, name.Substring(0, index));

                return Tuple.Create(Scope.MakeFromNamespace(tup.Item2, tup.Item1), name.Substring(index + 2));
            }

            public static List<string> SyntacticSplit(string s, string separator)
            {
                var result = new List<string>();
                int paren = 0;
                bool quote = false, apostrophe = false;
                int lastIndex = 0;
                for(int i = 0; i < s.Length; ++i) {
                    switch(s[i]) {
                    case '(':
                    case '{':
                    case '[':
                    case '<':
                        if(!quote && !apostrophe)
                            ++paren;
                        break;
                    case ')':
                    case '}':
                    case ']':
                    case '>':
                        if(!quote && !apostrophe)
                            --paren;
                        break;
                    case '\'':
                        if(!quote)
                            apostrophe = !apostrophe;
                        break;
                    case '"':
                        if(!apostrophe)
                            quote = !quote;
                        break;
                    default:
                        if(s[i] == separator[0] && (i + separator.Length <= s.Length) && (s.Substring(i, separator.Length) == separator) && !quote && !apostrophe && paren == 0) {
                            result.Add(s.Substring(lastIndex, i - lastIndex));
                            lastIndex = i + separator.Length;
                        }
                        break;
                    }
                }

                if(lastIndex < s.Length) {
                    result.Add(s.Substring(lastIndex, s.Length - lastIndex));
                }

                return result;
            }

            public static string RemoveParenthesis(string s)
            {
                while(true) {
                    s = s.Trim();
                    if(!s.StartsWith("(") || !s.EndsWith(")"))
                        break;

                    int parent = 1;
                    for(int i = 1; i < s.Length - 1; ++i) {
                        if(s[i] == '(')
                            ++parent;
                        else if(s[i] == ')') {
                            --parent;
                            if(parent == 0)
                                return s;
                        }
                    }

                    s = s.Substring(1, s.Length - 2);
                }

                return s;
            }
        }
    }
}

