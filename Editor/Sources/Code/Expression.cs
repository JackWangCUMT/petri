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
using System.Linq;
using System.Text.RegularExpressions;

namespace Petri.Editor.Code
{
    public enum Language
    {
        C,
        Cpp,
        CSharp,
    }

    public abstract class Expression
    {
        public enum ExprType
        {
            Parenthesis,
            Invocation,
            Subscript,
            Template,
            Quote,
            DoubleQuote,
            Brackets,
            Number,
            ID}

        ;

        protected Expression(Language language, Operator.Name op)
        {
            Operator = op;
            Language = language;
            Unexpanded = "";
        }

        public abstract bool UsesFunction(Function f);

        public abstract string MakeCode();

        public abstract string MakeUserReadable();

        public abstract List<LiteralExpression> GetLiterals();

        public virtual bool NeedsReturn { get { return false; } }

        public static Expression CreateFromStringAndEntity(string s,
                                                           Entity entity,
                                                           bool allowComma = true)
        {
            return CreateFromStringAndEntity<Expression>(s, entity, allowComma);
        }

        public static Expression CreateFromString(string s,
                                                  Language language,
                                                  IEnumerable<Function> functions = null,
                                                  Dictionary<string, string> macros = null,
                                                  bool allowComma = true)
        {
            return CreateFromString<Expression>(s, language, functions, macros, allowComma);
        }

        public static ExpressionType CreateFromStringAndEntity<ExpressionType>(string s,
                                                                               Entity entity,
                                                                               bool allowComma = true) where ExpressionType : Expression
        {
            return CreateFromString<ExpressionType>(s,
                                                    entity.Document.Settings.Language,
                                                    entity.Document.AllFunctions,
                                                    entity.Document.PreprocessorMacros,
                                                    allowComma);
        }

        public static ExpressionType CreateFromString<ExpressionType>(string s,
                                                                      Language language,
                                                                      IEnumerable<Function> functions = null,
                                                                      Dictionary<string, string> macros = null,
                                                                      bool allowComma = true) where ExpressionType : Expression
        {
            string unexpanded = s;
            string expanded = Expand(s, macros);
            s = expanded;
            var tup = Expression.Preprocess(s);
            Expression result;
            var exprList = tup.Item1.Split(new char[]{ ';' });
            var parsedList = from e in exprList
                                      select Expression.CreateFromPreprocessedString(e,
                                                                                     language,
                                                                                     functions,
                                                                                     macros,
                                                                                     tup.Item2,
                                                                                     true);
            if(parsedList.Count() > 1) {
                result = new ExpressionList(language, parsedList);
            }
            else {
                var it = parsedList.GetEnumerator();
                it.MoveNext();
                result = it.Current;
            }
            if(!(result is ExpressionType))
                throw new Exception(Configuration.GetLocalized("Unable to get a valid expression."));
            result.Unexpanded = unexpanded;
            result.NeedsExpansion = !unexpanded.Equals(expanded);
            return (ExpressionType)result;
        }

        public Language Language { get; private set; }

        public Operator.Name Operator { get; private set; }

        public string Unexpanded { get; private set; }

        public bool NeedsExpansion { get; private set; }

        private static string Expand(string expression, IDictionary<string, string> macros)
        {
            if(macros != null) {
                foreach(var macro in macros) {
                    expression = expression.Replace(macro.Key, macro.Value);
                }
            }
            return expression;
        }

        protected static Tuple<string, List<Tuple<ExprType, string>>> Preprocess(string s)
        {
            s = Parser.RemoveParenthesis(s.Trim()).Trim();
            var subexprs = new List<Tuple<ExprType, string>>();
            string namePattern = Parser.VariablePattern;
            namePattern = namePattern.Substring(0, namePattern.Length - 1) + "\\s*)?.*";
            var findName = new Regex(namePattern);
            var findNumber = new Regex("(" + Parser.NumberPattern + ")?.*");
            var nesting = new Stack<Tuple<ExprType, int>>();
            for(int i = 0; i < s.Length;) {
                if(nesting.Count == 0 || (nesting.Peek().Item1 != ExprType.Quote && nesting.Peek().Item1 != ExprType.DoubleQuote)) {
                    var sub = s.Substring(i);
                    var m1 = findName.Match(sub);
                    if(m1.Success) {
                        var id = m1.Groups["name"].Value;
                        if(id.Length > 0) {
                            subexprs.Add(Tuple.Create(ExprType.ID, id));
                            var newstr = "@" + (subexprs.Count - 1).ToString() + "@";
                            s = s.Remove(i, id.Length).Insert(i, newstr);
                            i += newstr.Length;
                            continue;
                        }
                    }
                    var m2 = findNumber.Match(sub);
                    if(m2.Success) {
                        var num = m2.Groups["number"].Value;
                        if(num.Length > 0) {
                            subexprs.Add(Tuple.Create(ExprType.Number, num));
                            var newstr = "@" + (subexprs.Count - 1).ToString() + "@";
                            s = s.Remove(i, num.Length).Insert(i, newstr);
                            i += newstr.Length;
                            continue;
                        }
                    }
                }
                switch(s[i]) {
                case '(':
                    bool special = false;
                    if(i > 0 && s[i - 1] == '@') {// It is a call operator invocation
                        special = true;
                    }
                    nesting.Push(Tuple.Create(special ? ExprType.Invocation : ExprType.Parenthesis,
                                              i));
                    break;
                case ')':
                    if(nesting.Count > 0 && (nesting.Peek().Item1 == ExprType.Invocation || nesting.Peek().Item1 == ExprType.Parenthesis)) {
                        subexprs.Add(Tuple.Create(nesting.Peek().Item1,
                                                  s.Substring(nesting.Peek().Item2,
                                                              i - nesting.Peek().Item2 + 1)));
                        var newstr = "@" + (subexprs.Count - 1).ToString() + "@" + (nesting.Peek().Item1 == ExprType.Invocation ? "()" : "");
                        s = s.Remove(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1).Insert(nesting.Peek().Item2,
                                                                                                newstr);
                        i += newstr.Length;
                        nesting.Pop();
                    }
                    else
                        throw new Exception(Configuration.GetLocalized("Unexpected closing parenthesis found!"));
                    break;
                case '{':
                    nesting.Push(Tuple.Create(ExprType.Brackets, i));
                    break;
                case '}':
                    if(nesting.Count > 0 && nesting.Peek().Item1 == ExprType.Brackets) {
                        subexprs.Add(Tuple.Create(ExprType.Brackets,
                                                  s.Substring(nesting.Peek().Item2,
                                                              i - nesting.Peek().Item2 + 1)));
                        var newstr = "@" + (subexprs.Count - 1).ToString() + "@";
                        s = s.Remove(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1).Insert(nesting.Peek().Item2,
                                                                                                newstr);
                        i += newstr.Length;
                        nesting.Pop();
                    }
                    else
                        throw new Exception(Configuration.GetLocalized("Unexpected closing bracket found!"));
                    break;
                case '[':
                    nesting.Push(Tuple.Create(ExprType.Subscript, i));
                    break;
                case ']':
                    if(nesting.Count > 0 && nesting.Peek().Item1 == ExprType.Subscript) {
                        subexprs.Add(Tuple.Create(nesting.Peek().Item1,
                                                  s.Substring(nesting.Peek().Item2,
                                                              i - nesting.Peek().Item2 + 1)));
                        var newstr = "@" + (subexprs.Count - 1).ToString() + "@";
                        s = s.Remove(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1).Insert(nesting.Peek().Item2,
                                                                                                newstr);
                        i += newstr.Length;
                        nesting.Pop();
                    }
                    else
                        throw new Exception(Configuration.GetLocalized("Unexpected closing bracket found!"));
                    break;
                case '"':// First quote
                    if(nesting.Count == 0 || (nesting.Peek().Item1 != ExprType.DoubleQuote && nesting.Peek().Item1 != ExprType.Quote)) {
                        nesting.Push(Tuple.Create(ExprType.DoubleQuote, i));
                    }
                    // Second quote
                    else if(nesting.Count > 0 && nesting.Peek().Item1 == ExprType.DoubleQuote && s[i - 1] != '\\') {
                        subexprs.Add(Tuple.Create(ExprType.DoubleQuote,
                                                  s.Substring(nesting.Peek().Item2,
                                                              i - nesting.Peek().Item2 + 1)));
                        var newstr = "@" + (subexprs.Count - 1).ToString() + "@";
                        s = s.Remove(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1).Insert(nesting.Peek().Item2,
                                                                                                newstr);
                        i += newstr.Length;
                        nesting.Pop();
                    }
                    else {
                        throw new Exception(Configuration.GetLocalized("{0} expected, but \" found!",
                                                                       nesting.Peek().Item1));
                    }
                    break;
                case '\'':// First quote
                    if(nesting.Count == 0 || (nesting.Peek().Item1 != ExprType.Quote && nesting.Peek().Item1 != ExprType.DoubleQuote)) {
                        nesting.Push(Tuple.Create(ExprType.Quote, i));
                    }
                    // Second quote
                    else if(nesting.Count > 0 && nesting.Peek().Item1 == ExprType.Quote && s[i - 1] != '\\') {
                        subexprs.Add(Tuple.Create(ExprType.Quote,
                                                  s.Substring(nesting.Peek().Item2,
                                                              i - nesting.Peek().Item2 + 1)));
                        var newstr = "@" + (subexprs.Count - 1).ToString() + "@";
                        s = s.Remove(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1).Insert(nesting.Peek().Item2,
                                                                                                newstr);
                        i += newstr.Length;
                        nesting.Pop();
                    }
                    else {
                        throw new Exception(Configuration.GetLocalized("{0} expected, but \' found!",
                                                                       nesting.Peek().Item1));
                    }
                    break;
                }
                ++i;
            }
            var newExprs = new List<Tuple<ExprType, string>>();
            foreach(var name in Code.Operator.Lex) {
                s = s.Replace(Code.Operator.Properties[name].cpp,
                              Code.Operator.Properties[name].lexed);
            }
            foreach(var expr in subexprs) {
                string val = expr.Item2;
                switch(expr.Item1) {
                case ExprType.Brackets:
                case ExprType.Parenthesis:
                case ExprType.Subscript:
                case ExprType.Invocation:
                case ExprType.Template:
                    foreach(var name in Code.Operator.Lex) {
                        val = val.Replace(Code.Operator.Properties[name].cpp,
                                          Code.Operator.Properties[name].lexed);
                    }
                    break;
                }
                newExprs.Add(Tuple.Create(expr.Item1, val));
            }
            subexprs.Clear();
            foreach(var expr in newExprs) {
                subexprs.Add(Tuple.Create(expr.Item1,
                                          expr.Item2.Replace("\t", "").Replace(" ",
                                                                               "")));
            }
            s = s.Replace("\t", "");
            s = s.Replace(" ", "");
            return Tuple.Create(s, subexprs);
        }

        protected static bool Match(int opIndex, string s, Operator.Op prop)
        {
            if(prop.type == Code.Operator.Type.Binary) {
                if(opIndex == 0 || (s[opIndex - 1] != '@' && s[opIndex - 1] != '#') || opIndex + prop.lexed.Length == s.Length || (s[opIndex + prop.lexed.Length] != '@' && s[opIndex + prop.lexed.Length] != '#')) {
                    return false;
                }
            }
            else if(prop.type == Code.Operator.Type.PrefixUnary) {
                if((opIndex != 0 && s[opIndex - 1] == '@') || opIndex + prop.lexed.Length == s.Length || (s[opIndex + prop.lexed.Length] != '@' && s[opIndex + prop.lexed.Length] != '#')) {
                    return false;
                }
            }
            else if(prop.type == Code.Operator.Type.SuffixUnary) {
                if(opIndex == 0 || (s[opIndex - 1] != '@' && s[opIndex - 1] != '#') || (opIndex + prop.lexed.Length != s.Length && s[opIndex + prop.lexed.Length] == '@')) {
                    return false;
                }
            }
            return true;
        }

        protected static Expression CreateFromPreprocessedString(string s,
                                                                 Language language,
                                                                 IEnumerable<Function> functions,
                                                                 Dictionary<string, string> macros,
                                                                 List<Tuple<ExprType, string>> subexprs,
                                                                 bool allowComma)
        {
            if(Regex.Match(s, "^@[0-9]+@$").Success) {
                int nb = int.Parse(s.Substring(1, s.Length - 2));
                switch(subexprs[nb].Item1) {
                case ExprType.Parenthesis:
                case ExprType.Invocation:
                case ExprType.Template:
                case ExprType.Subscript:
                    s = Expression.GetStringFromPreprocessed(s, subexprs);
                    var tup = Expression.Preprocess(s);
                    s = tup.Item1;
                    subexprs = tup.Item2;
                    break;
                }
            }
            for(int i = allowComma ? 17 : 16; i >= 0; --i) {
                int bound;
                int direction;
                if(Code.Operator.Properties[Code.Operator.ByPrecedence[i][0]].associativity == Code.Operator.Associativity.RightToLeft) {
                    bound = s.Length;
                    direction = -1;
                }
                else {
                    bound = -1;
                    direction = 1;
                }
                int index = bound;
                var foundOperator = Code.Operator.Name.None;
                foreach(var op in Code.Operator.ByPrecedence[i]) {
                    var prop = Code.Operator.Properties[op];
                    if(prop.implemented) {
                        int currentIndex = 0;
                        while(true) {
                            int opIndex = s.Substring(currentIndex).IndexOf(prop.lexed);
                            if(opIndex == -1)
                                break;
                            opIndex += currentIndex;// If we have found an operator closer to the end of the string (in relation to the operator associativity)
                            if(opIndex.CompareTo(index) == direction) {
                                if(!Match(opIndex, s, prop)) {
                                    currentIndex = opIndex + prop.lexed.Length;
                                    continue;
                                }
                                index = opIndex;
                                foundOperator = op;
                            }
                            ++currentIndex;
                        }
                    }
                }
                if(index != bound) {
                    var prop = Code.Operator.Properties[foundOperator];
                    if(prop.type == Code.Operator.Type.Binary) {
                        string e1 = s.Substring(0, index);
                        string e2 = s.Substring(index + prop.lexed.Length);// Method call
                        if(foundOperator == Code.Operator.Name.SelectionRef || foundOperator == Code.Operator.Name.SelectionPtr) {
                            string that = Expression.GetStringFromPreprocessed(e1, subexprs);
                            string invocation = Expression.GetStringFromPreprocessed(e2,
                                                                                     subexprs);
                            return CreateMethodInvocation(foundOperator == Code.Operator.Name.SelectionPtr,
                                                          that,
                                                          invocation,
                                                          language,
                                                          functions,
                                                          macros);
                        }
                        return new BinaryExpression(language,
                                                    foundOperator,
                                                    Expression.CreateFromPreprocessedString(e1,
                                                                                            language,
                                                                                            functions,
                                                                                            macros,
                                                                                            subexprs,
                                                                                            true),
                                                    Expression.CreateFromPreprocessedString(e2,
                                                                                            language,
                                                                                            functions,
                                                                                            macros,
                                                                                            subexprs,
                                                                                            true));
                    }
                    else if(prop.type == Code.Operator.Type.PrefixUnary) {
                        return new UnaryExpression(language, foundOperator,
                                                   Expression.CreateFromPreprocessedString(s.Substring(index + prop.lexed.Length),
                                                                                           language,
                                                                                           functions,
                                                                                           macros,
                                                                                           subexprs,
                                                                                           true));
                    }
                    else if(prop.type == Code.Operator.Type.SuffixUnary) {
                        if(foundOperator == Code.Operator.Name.FunCall) {
                            return CreateFunctionInvocation(GetStringFromPreprocessed(s, subexprs),
                                                            language,
                                                            functions,
                                                            macros);
                        }
                        return new UnaryExpression(language,
                                                   foundOperator,
                                                   Expression.CreateFromPreprocessedString(s.Substring(0,
                                                                                                       index),
                                                                                           language,
                                                                                           functions,
                                                                                           macros,
                                                                                           subexprs,
                                                                                           true));
                    }
                }
            }
            return LiteralExpression.CreateFromString(GetStringFromPreprocessed(s, subexprs),
                                                      language);
        }

        protected static string GetStringFromPreprocessed(string prep,
                                                          List<Tuple<ExprType, string>> subexprs)
        {
            foreach(var name in Code.Operator.Lex) {
                prep = prep.Replace(Code.Operator.Properties[name].lexed,
                                    " " + Code.Operator.Properties[name].cpp + " ");
                var newExprs = new List<Tuple<ExprType, string>>();
                foreach(var expr in subexprs) {
                    switch(expr.Item1) {
                    case ExprType.Brackets:
                    case ExprType.Parenthesis:
                    case ExprType.Subscript:
                    case ExprType.Invocation:
                    case ExprType.Template:
                        newExprs.Add(Tuple.Create(expr.Item1,
                                                  expr.Item2.Replace(Code.Operator.Properties[name].lexed,
                                                                     " " + Code.Operator.Properties[name].cpp + " ")));
                        break;
                    default:
                        newExprs.Add(expr);
                        break;
                    }
                }
                subexprs = newExprs;
            }
            int index;
            while(true) {
                index = prep.IndexOf("@");
                if(index == -1)
                    break;
                int lastIndex = prep.Substring(index + 1).IndexOf("@") + index + 1;
                int expr = int.Parse(prep.Substring(index + 1, lastIndex - (index + 1)));
                switch(subexprs[expr].Item1) {
                case ExprType.DoubleQuote:
                case ExprType.Quote:
                case ExprType.Parenthesis:
                case ExprType.Brackets:
                case ExprType.Subscript:
                case ExprType.ID:
                case ExprType.Number:
                    prep = prep.Remove(index, lastIndex - index + 1).Insert(index,
                                                                            subexprs[expr].Item2);
                    break;
                case ExprType.Invocation:
                    prep = prep.Remove(index, lastIndex - index + 5).Insert(index,
                                                                            subexprs[expr].Item2);
                    break;
                }
            }
            return prep;
        }

        private static Tuple<Scope, string, List<Expression>> ExtractScopeNameAndArgs(string invocation,
                                                                                      Language language,
                                                                                      IEnumerable<Function> functions,
                                                                                      Dictionary<string, string> macros)
        {
            var func = Expand(invocation, macros);
            func = Parser.RemoveParenthesis(func);
            int index = func.IndexOf("(");
            var args = Parser.RemoveParenthesis(func.Substring(index));
            func = func.Substring(0, index);
            var tup = Parser.ExtractScope(language, func);
            var argsList = Parser.SyntacticSplit(args, ",");
            var exprList = new List<Expression>();
            foreach(var ss in argsList) {
                exprList.Add(Expression.CreateFromString<Expression>(Parser.RemoveParenthesis(ss),
                                                                     language,
                                                                     functions,
                                                                     macros));
            }
            return Tuple.Create(tup.Item1, tup.Item2.Trim(), exprList);
        }

        private static FunctionInvocation CreateFunctionInvocation(string invocation,
                                                                   Language language,
                                                                   IEnumerable<Function> functions,
                                                                   Dictionary<string, string> macros)
        {
            var scopeNameAndArgs = ExtractScopeNameAndArgs(invocation,
                                                           language,
                                                           functions,
                                                           macros);
            Function f = null;
            if(functions != null) {
                f = (functions.FirstOrDefault(delegate(Function ff) {
                    return !(ff is Method) && ff.Parameters.Count == scopeNameAndArgs.Item3.Count && scopeNameAndArgs.Item2 == ff.Name && ((scopeNameAndArgs.Item1 == null && ff.Enclosing == null) || (scopeNameAndArgs.Item1 != null && scopeNameAndArgs.Item1.Equals(ff.Enclosing)));
                })) as Function;
            }

            if(f == null) {
                f = new Function(Type.UnknownType(language),
                                 scopeNameAndArgs.Item1,
                                 scopeNameAndArgs.Item2,
                                 false);
                int i = 0;
                foreach(Expression e in scopeNameAndArgs.Item3) {
                    f.Parameters.Add(new Param(Type.UnknownType(language),
                                               "param" + (i++).ToString()));
                }
            }

            return new FunctionInvocation(language, f, scopeNameAndArgs.Item3.ToArray());
        }

        private static FunctionInvocation CreateMethodInvocation(bool indirection,
                                                                 string that,
                                                                 string invocation,
                                                                 Language language,
                                                                 IEnumerable<Function> functions,
                                                                 Dictionary<string, string> macros)
        {
            var scopeNameAndArgs = ExtractScopeNameAndArgs(invocation,
                                                           language,
                                                           functions,
                                                           macros);
            if(Scope.GetSeparator(language) == Code.Operator.Properties[Code.Operator.Name.SelectionRef].cpp && Regex.Match(that,
                                                                                                                            "(" + Parser.NamePattern + Scope.GetSeparator(language) + ")*" + Parser.NamePattern).Success) {
                var scopes = that.Split(new string[]{ Scope.GetSeparator(language) },
                                        StringSplitOptions.None);
                Scope outerScope = null;
                foreach(var s in scopes) {
                    outerScope = Scope.MakeFromNamespace(language, s.Trim(), outerScope);
                }
                var scope = Scope.MakeFromScopes(scopeNameAndArgs.Item1, outerScope);

                return CreateFunctionInvocation(scope.ToString() + invocation,
                                                language,
                                                functions,
                                                macros);
            }
            Method m = null;
            if(functions != null) {
                m = (functions.FirstOrDefault(delegate(Function ff) {
                    return (ff is Method) && ff.Parameters.Count == scopeNameAndArgs.Item3.Count && scopeNameAndArgs.Item2 == ff.Name && scopeNameAndArgs.Item1.Equals(ff.Enclosing);
                })) as Method;
            }
            if(m == null) {
                m = new Method(Type.UnknownType(language),
                               Type.UnknownType(language),
                               scopeNameAndArgs.Item2,
                               "",
                               false);
                int i = 0;
                foreach(Expression e in scopeNameAndArgs.Item3) {
                    m.Parameters.Add(new Param(Type.UnknownType(language),
                                               "param" + (i++).ToString()));
                }
            }
            return new MethodInvocation(language, m,
                                        Expression.CreateFromString<Expression>(that,
                                                                                language,
                                                                                functions,
                                                                                macros),
                                        indirection,
                                        scopeNameAndArgs.Item3.ToArray());
        }

        protected static string Parenthesize(Expression parent,
                                             Expression child,
                                             string representation)
        {
            bool parenthesize = false;
            if(child.Operator != Code.Operator.Name.None) {
                var parentProperties = Code.Operator.Properties[parent.Operator];
                var childProperties = Code.Operator.Properties[child.Operator];
                if(parentProperties.precedence < childProperties.precedence) {
                    parenthesize = true;
                }
                else if(parentProperties.precedence == childProperties.precedence) {// No need to manage unary operators
                    // We assume the ternary conditional operator does not need to be parenthesized either
                    if(parentProperties.type == Code.Operator.Type.Binary) {
                        var castedParent = (BinaryExpression)parent;// We can assume the associativity is the same for both operators as they have the same precedence
                        // If the operator is left-associative, but the expression was parenthesized from right to left
                        // eg. a + (b + c) (do not forget that IEEE floating point values are not communtative with regards to addition, among others).
                        // So we need to preserve the associativity the user gave at first.
                        if(parentProperties.associativity == Code.Operator.Associativity.LeftToRight && castedParent.Expression2 == child) {
                            parenthesize = true;
                        }
                            // If the operator is right-associative, but the expression was parenthesize from left to right
                        else if(parentProperties.associativity == Code.Operator.Associativity.RightToLeft && castedParent.Expression1 == child) {
                            parenthesize = true;
                        }
                    }
                }
            }
            if(parenthesize)
                return "(" + representation + ")";
            else
                return representation;
        }

    }
}