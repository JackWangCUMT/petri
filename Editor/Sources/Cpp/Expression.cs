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

namespace Petri
{
    namespace Cpp
    {
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

            public abstract string MakeCpp();

            public abstract string MakeUserReadable();

            public abstract List<LiteralExpression> GetLiterals();

            public virtual bool NeedsReturn {
                get {
                    return false;
                }
            }

            public static Expression CreateFromStringAndEntity(string s, Entity entity, bool allowComma = true)
            {
                return CreateFromStringAndEntity<Expression>(s, entity, allowComma);
            }

            public static Expression CreateFromString(string s, Language language = Language.None, IEnumerable<Cpp.Function> functions = null, Dictionary<string, string> macros = null, bool allowComma = true)
            {
                return CreateFromString<Expression>(s, language, functions, macros, allowComma);
            }

            public static ExpressionType CreateFromStringAndEntity<ExpressionType>(string s, Entity entity, bool allowComma = true) where ExpressionType : Expression
            {
                return CreateFromString<ExpressionType>(s, entity?.Document.Settings.Language ?? Language.None, entity?.Document.AllFunctions, entity?.Document.CppMacros, allowComma);
            }

            public static ExpressionType CreateFromString<ExpressionType>(string s, Language language = Language.None, IEnumerable<Cpp.Function> functions = null, Dictionary<string, string> macros = null, bool allowComma = true) where ExpressionType : Expression
            {
                string unexpanded = s;

                string expanded = Expand(s, macros);
                s = expanded;

                var tup = Expression.Preprocess(s);

                Expression result;

                var exprList = tup.Item1.Split(new char[]{ ';' });
                var parsedList = from e in exprList
                                             select Expression.CreateFromPreprocessedString(e, language, functions, macros, tup.Item2, true);

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

            public Language Language {
                get;
                private set;
            }

            public Operator.Name Operator {
                get;
                private set;
            }

            public string Unexpanded {
                get;
                private set;
            }

            public bool NeedsExpansion {
                get;
                private set;
            }

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

                string namePattern = Parser.NamePattern;
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
                        // FIXME: bug
                        if(i > 0 && s[i - 1] == '@') {
                            // It is a call operator invocation
                            special = true;
                        }
                        nesting.Push(Tuple.Create(special ? ExprType.Invocation : ExprType.Parenthesis, i));
                        break;
                    case ')':
                        if(nesting.Count > 0 && (nesting.Peek().Item1 == ExprType.Invocation || nesting.Peek().Item1 == ExprType.Parenthesis)) {
                            subexprs.Add(Tuple.Create(nesting.Peek().Item1, s.Substring(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1)));
                            var newstr = "@" + (subexprs.Count - 1).ToString() + "@" + (nesting.Peek().Item1 == ExprType.Invocation ? "()" : "");
                            s = s.Remove(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1).Insert(nesting.Peek().Item2, newstr);
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
                            subexprs.Add(Tuple.Create(ExprType.Brackets, s.Substring(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1)));
                            var newstr = "@" + (subexprs.Count - 1).ToString() + "@";
                            s = s.Remove(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1).Insert(nesting.Peek().Item2, newstr);
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
                            subexprs.Add(Tuple.Create(nesting.Peek().Item1, s.Substring(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1)));
                            var newstr = "@" + (subexprs.Count - 1).ToString() + "@";
                            s = s.Remove(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1).Insert(nesting.Peek().Item2, newstr);
                            i += newstr.Length;
                            nesting.Pop();
                        }
                        else
                            throw new Exception(Configuration.GetLocalized("Unexpected closing bracket found!"));
                        break;
                    case '"':
						// First quote
                        if(nesting.Count == 0 || (nesting.Peek().Item1 != ExprType.DoubleQuote && nesting.Peek().Item1 != ExprType.Quote)) {
                            nesting.Push(Tuple.Create(ExprType.DoubleQuote, i));
                        }
						// Second quote
						else if(nesting.Count > 0 && nesting.Peek().Item1 == ExprType.DoubleQuote && s[i - 1] != '\\') {
                            subexprs.Add(Tuple.Create(ExprType.DoubleQuote, s.Substring(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1)));
                            var newstr = "@" + (subexprs.Count - 1).ToString() + "@";
                            s = s.Remove(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1).Insert(nesting.Peek().Item2, newstr);
                            i += newstr.Length;
                            nesting.Pop();
                        }
                        else
                            throw new Exception(Configuration.GetLocalized("{0} expected, but \" found!", nesting.Peek().Item1));
                        break;
                    case '\'':
						// First quote
                        if(nesting.Count == 0 || (nesting.Peek().Item1 != ExprType.Quote && nesting.Peek().Item1 != ExprType.DoubleQuote)) {
                            nesting.Push(Tuple.Create(ExprType.Quote, i));
                        }
						// Second quote
						else if(nesting.Count > 0 && nesting.Peek().Item1 == ExprType.Quote && s[i - 1] != '\\') {
                            subexprs.Add(Tuple.Create(ExprType.Quote, s.Substring(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1)));
                            var newstr = "@" + (subexprs.Count - 1).ToString() + "@";
                            s = s.Remove(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1).Insert(nesting.Peek().Item2, newstr);
                            i += newstr.Length;
                            nesting.Pop();
                        }
                        else
                            throw new Exception(Configuration.GetLocalized("{0} expected, but \' found!", nesting.Peek().Item1));
                        break;
                    }

                    ++i;
                }

                var newExprs = new List<Tuple<ExprType, string>>();
                foreach(var name in Cpp.Operator.Lex) {
                    s = s.Replace(Cpp.Operator.Properties[name].cpp, Cpp.Operator.Properties[name].lexed);
                }

                foreach(var expr in subexprs) {
                    string val = expr.Item2;

                    switch(expr.Item1) {
                    case ExprType.Brackets:
                    case ExprType.Parenthesis:
                    case ExprType.Subscript:
                    case ExprType.Invocation:
                    case ExprType.Template:
                        foreach(var name in Cpp.Operator.Lex) {
                            val = val.Replace(Cpp.Operator.Properties[name].cpp, Cpp.Operator.Properties[name].lexed);
                        }
                        break;
                    }

                    newExprs.Add(Tuple.Create(expr.Item1, val));
                }
                subexprs.Clear();

                foreach(var expr in newExprs) {
                    subexprs.Add(Tuple.Create(expr.Item1, expr.Item2.Replace("\t", "").Replace(" ", "")));
                }

                s = s.Replace("\t", "");
                s = s.Replace(" ", "");

                return Tuple.Create(s, subexprs);
            }

            protected static bool Match(int opIndex, string s, Cpp.Operator.Op prop)
            {
                if(prop.type == Cpp.Operator.Type.Binary) {
                    if(opIndex == 0 || (s[opIndex - 1] != '@' && s[opIndex - 1] != '#') || opIndex + prop.lexed.Length == s.Length || (s[opIndex + prop.lexed.Length] != '@' && s[opIndex + prop.lexed.Length] != '#')) {
                        return false;
                    }
                }
                else if(prop.type == Cpp.Operator.Type.PrefixUnary) {
                    if((opIndex != 0 && s[opIndex - 1] == '@') || opIndex + prop.lexed.Length == s.Length || (s[opIndex + prop.lexed.Length] != '@' && s[opIndex + prop.lexed.Length] != '#')) {
                        return false;
                    }
                }
                else if(prop.type == Cpp.Operator.Type.SuffixUnary) {
                    if(opIndex == 0 || (s[opIndex - 1] != '@' && s[opIndex - 1] != '#') || (opIndex + prop.lexed.Length != s.Length && s[opIndex + prop.lexed.Length] == '@')) {
                        return false;
                    }
                }

                return true;
            }

            protected static Expression CreateFromPreprocessedString(string s, Language language, IEnumerable<Cpp.Function> functions, Dictionary<string, string> macros, List<Tuple<ExprType, string>> subexprs, bool allowComma)
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
                    if(Cpp.Operator.Properties[Cpp.Operator.ByPrecedence[i][0]].associativity == Petri.Cpp.Operator.Associativity.RightToLeft) {
                        bound = s.Length;
                        direction = -1;
                    }
                    else {
                        bound = -1;
                        direction = 1;
                    }

                    int index = bound;
                    var foundOperator = Petri.Cpp.Operator.Name.None;
                    foreach(var op in Cpp.Operator.ByPrecedence[i]) {
                        var prop = Cpp.Operator.Properties[op];
                        if(prop.implemented) {
                            int currentIndex = 0;
                            while(true) {
                                int opIndex = s.Substring(currentIndex).IndexOf(prop.lexed);
                                if(opIndex == -1)
                                    break;
                                opIndex += currentIndex;

                                // If we have found an operator closer to the end of the string (in relation to the operator associativity)
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
                        var prop = Cpp.Operator.Properties[foundOperator];

                        if(prop.type == Petri.Cpp.Operator.Type.Binary) {
                            string e1 = s.Substring(0, index);
                            string e2 = s.Substring(index + prop.lexed.Length);

                            // Method call
                            if(foundOperator == Petri.Cpp.Operator.Name.SelectionRef || foundOperator == Petri.Cpp.Operator.Name.SelectionPtr) {
                                string that = Expression.GetStringFromPreprocessed(e1, subexprs);
                                string invocation = Expression.GetStringFromPreprocessed(e2, subexprs);
                                return CreateMethodInvocation(foundOperator == Cpp.Operator.Name.SelectionPtr, new List<String>{that, invocation}, language, functions, macros); 
                            }

                            return new BinaryExpression(language, foundOperator, Expression.CreateFromPreprocessedString(e1, language, functions, macros, subexprs, true), Expression.CreateFromPreprocessedString(e2, language, functions, macros, subexprs, true));
                        }
                        else if(prop.type == Petri.Cpp.Operator.Type.PrefixUnary) {
                            return new UnaryExpression(language, foundOperator, Expression.CreateFromPreprocessedString(s.Substring(index + prop.lexed.Length), language, functions, macros, subexprs, true));
                        }
                        else if(prop.type == Petri.Cpp.Operator.Type.SuffixUnary) {
                            if(foundOperator == Petri.Cpp.Operator.Name.FunCall) {
                                return CreateFunctionInvocation(GetStringFromPreprocessed(s, subexprs), language, functions, macros);
                            }
                            return new UnaryExpression(language, foundOperator, Expression.CreateFromPreprocessedString(s.Substring(0, index), language, functions, macros, subexprs, true));
                        }
                    }
                }

                return LiteralExpression.CreateFromString(GetStringFromPreprocessed(s, subexprs), language);
            }

            protected static string GetStringFromPreprocessed(string prep, List<Tuple<ExprType, string>> subexprs)
            {
                foreach(var name in Cpp.Operator.Lex) {
                    prep = prep.Replace(Cpp.Operator.Properties[name].lexed, " " + Cpp.Operator.Properties[name].cpp + " ");
                    var newExprs = new List<Tuple<ExprType, string>>();
                    foreach(var expr in subexprs) {
                        switch(expr.Item1) {
                        case ExprType.Brackets:
                        case ExprType.Parenthesis:
                        case ExprType.Subscript:
                        case ExprType.Invocation:
                        case ExprType.Template:
                            newExprs.Add(Tuple.Create(expr.Item1, expr.Item2.Replace(Cpp.Operator.Properties[name].lexed, " " + Cpp.Operator.Properties[name].cpp + " ")));
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
                        prep = prep.Remove(index, lastIndex - index + 1).Insert(index, subexprs[expr].Item2);
                        break;
                    case ExprType.Invocation:
                        prep = prep.Remove(index, lastIndex - index + 4).Insert(index, subexprs[expr].Item2);
                        break;
                    }
                }

                return prep;
            }

            private static Tuple<Scope, string, List<Expression>> ExtractScopeNameAndArgs(string invocation, Language language, IEnumerable<Cpp.Function> functions, Dictionary<string, string> macros) {
                var func = Expand(invocation, macros);
                func = Parser.RemoveParenthesis(func);

                int index = func.IndexOf("(");
                var args = Parser.RemoveParenthesis(func.Substring(index));
                func = func.Substring(0, index);
                var tup = Parser.ExtractScope(func);
                var argsList = Parser.SyntacticSplit(args, ",");
                var exprList = new List<Expression>();
                foreach(var ss in argsList) {
                    exprList.Add(Expression.CreateFromString<Expression>(Parser.RemoveParenthesis(ss), language, functions, macros));
                }

                return Tuple.Create(tup.Item1, tup.Item2, exprList);
            }

            private static FunctionInvocation CreateFunctionInvocation(string invocation, Language language, IEnumerable<Cpp.Function> functions, Dictionary<string, string> macros)
            {
                var scopeNameAndArgs = ExtractScopeNameAndArgs(invocation, language, functions, macros);

                Cpp.Function f;
                if(functions == null) {
                    f = new Cpp.Function(Type.UnknownType, scopeNameAndArgs.Item1, scopeNameAndArgs.Item2, false);
                    int i = 0;
                    foreach(Expression e in scopeNameAndArgs.Item3) {
                        f.Parameters.Add(new Param(Type.UnknownType, "param" + (i++).ToString()));
                    }
                }
                else {
                    f = (functions.FirstOrDefault(delegate(Cpp.Function ff) {
                        return !(ff is Method) && ff.Parameters.Count == scopeNameAndArgs.Item3.Count && scopeNameAndArgs.Item2 == ff.Name && scopeNameAndArgs.Item1.Equals(ff.Enclosing);
                    })) as Function;

                    if(f == null) {
                        throw new Exception(Configuration.GetLocalized("No function match the specified expression."));
                    }
                }

                return new FunctionInvocation(language, f, scopeNameAndArgs.Item3.ToArray());
            }

            private static MethodInvocation CreateMethodInvocation(bool indirection, List<string> invocation, Language language, IEnumerable<Cpp.Function> functions, Dictionary<string, string> macros)
            {
                var scopeNameAndArgs = ExtractScopeNameAndArgs(invocation[1], language, functions, macros);

                Cpp.Method m;
                if(functions == null) {
                    m = new Cpp.Method(Type.UnknownType, Type.UnknownType, scopeNameAndArgs.Item2, "", false);
                    int i = 0;
                    foreach(Expression e in scopeNameAndArgs.Item3) {
                        m.Parameters.Add(new Param(Type.UnknownType, "param" + (i++).ToString()));
                    }
                }
                else {
                    m = (functions.FirstOrDefault(delegate(Cpp.Function ff) {
                        return (ff is Method) && ff.Parameters.Count == scopeNameAndArgs.Item3.Count && scopeNameAndArgs.Item2 == ff.Name && scopeNameAndArgs.Item1.Equals(ff.Enclosing);
                    })) as Method;

                    if(m == null) {
                        throw new Exception(Configuration.GetLocalized("No method match the specified expression."));
                    }
                }

                return new MethodInvocation(language, m, Expression.CreateFromString<Expression>(invocation[0], language, functions, macros), indirection, scopeNameAndArgs.Item3.ToArray());
            }

            protected static string Parenthesize(Expression parent, Expression child, string representation)
            {
                bool parenthesize = false;

                if(child.Operator != Cpp.Operator.Name.None) {
                    var parentProperties = Cpp.Operator.Properties[parent.Operator];
                    var childProperties = Cpp.Operator.Properties[child.Operator];
                    if(parentProperties.precedence < childProperties.precedence) {
                        parenthesize = true;
                    }
                    else if(parentProperties.precedence == childProperties.precedence) {
                        // No need to manage unary operators
                        // We assume the ternary conditional operator does not need to be parenthesized either
                        if(parentProperties.type == Petri.Cpp.Operator.Type.Binary) {
                            var castedParent = (BinaryExpression)parent;
                            // We can assume the associativity is the same for both operators as they have the same precedence

                            // If the operator is left-associative, but the expression was parenthesized from right to left
                            // eg. a + (b + c) (do not forget that IEEE floating point values are not communtative with regards to addition, among others).
                            // So we need to preserve the associativity the user gave at first.
                            if(parentProperties.associativity == Petri.Cpp.Operator.Associativity.LeftToRight && castedParent.Expression2 == child) {
                                parenthesize = true;
                            }
							// If the operator is right-associative, but the expression was parenthesize from left to right
							else if(parentProperties.associativity == Petri.Cpp.Operator.Associativity.RightToLeft && castedParent.Expression1 == child) {
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

        public class EmptyExpression : Expression
        {
            public EmptyExpression(bool doWeCare) : base(Language.None, Cpp.Operator.Name.None)
            {
            }

            public override bool UsesFunction(Function f)
            {
                return false;
            }

            public override string MakeCpp()
            {
                if(DoWeCare)
                    throw new Exception(Configuration.GetLocalized("Empty expression!"));
                else
                    return "";
            }

            public override string MakeUserReadable()
            {
                return "";
            }

            public override List<LiteralExpression> GetLiterals()
            {
                return new List<LiteralExpression>();
            }

            public bool DoWeCare {
                get;
                private set;
            }
        }

        public class BracketedExpression : Expression
        {
            public BracketedExpression(Expression b, Expression expr, Expression a) : base(Language.None, Cpp.Operator.Name.None)
            {
                Before = b;
                Expression = expr;
                After = a;
            }

            public Expression Expression {
                get;
                private set;
            }

            public Expression Before {
                get;
                private set;
            }

            public Expression After {
                get;
                set;
            }

            public override bool UsesFunction(Function f)
            {
                return Expression.UsesFunction(f) || Before.UsesFunction(f) || After.UsesFunction(f);
            }

            public override string MakeCpp()
            {
                return Before.MakeCpp() + "{" + Expression.MakeCpp() + "}" + After.MakeCpp();
            }

            public override string MakeUserReadable()
            {
                return Before.MakeUserReadable() + "{" + Expression.MakeUserReadable() + "}" + After.MakeUserReadable();
            }

            public override List<LiteralExpression> GetLiterals()
            {
                var l = Before.GetLiterals();
                l.AddRange(Expression.GetLiterals());
                l.AddRange(After.GetLiterals());

                return l;
            }
        }

        public class LiteralExpression : Expression
        {
            static public Expression CreateFromString(string s, Language language)
            {
                if(s.Length >= 2 && s.StartsWith("$") && char.IsLower(s[1])) {
                    return new VariableExpression(s.Substring(1), language);
                }
                if(s.Contains("{")) {
                    var tup = Cpp.Expression.Preprocess(s);
                    int currentIndex = 0;
                    while(currentIndex < tup.Item1.Length) {
                        int index = tup.Item1.Substring(currentIndex).IndexOf("@") + currentIndex;
                        if(index == -1)
                            break;

                        int lastIndex = tup.Item1.Substring(index + 1).IndexOf("@") + index + 1;
                        int expr = int.Parse(tup.Item1.Substring(index + 1, lastIndex - (index + 1)));
                        if(tup.Item2[expr].Item1 == ExprType.Brackets) {
                            return new BracketedExpression(Cpp.Expression.CreateFromPreprocessedString(tup.Item1.Substring(0, index), language, null, null, tup.Item2, true),
                                Cpp.Expression.CreateFromPreprocessedString(tup.Item2[expr].Item2.Substring(1, tup.Item2[expr].Item2.Length - 2), language, null, null, tup.Item2, true),
                                Cpp.Expression.CreateFromPreprocessedString(tup.Item1.Substring(lastIndex + 1), language, null, null, tup.Item2, true));
                        }
                        else {
                            currentIndex = lastIndex + 1;
                        }
                    }
                }

                return new LiteralExpression(s);
            }

            protected LiteralExpression(string expr) : base(Language.None, Cpp.Operator.Name.None)
            {
                Expression = expr.Trim();
            }

            protected LiteralExpression(Language language, string expr) : base(language, Cpp.Operator.Name.None)
            {
                Expression = expr.Trim();
            }

            public virtual string Expression {
                get;
                set;
            }

            public override bool UsesFunction(Function f)
            {
                return false;
            }

            public override string MakeCpp()
            {
                return Expression;
            }

            public override string MakeUserReadable()
            {
                return Expression;
            }

            public override List<LiteralExpression> GetLiterals()
            {
                var l = new List<LiteralExpression>();
                l.Add(this);
                return l;
            }
        }

        public class VariableExpression : LiteralExpression
        {
            public VariableExpression(string expr, Language language = Language.None) : base(language, expr)
            {
                Regex name = new Regex(Cpp.Parser.NamePattern);
                Match nameMatch = name.Match(expr);
                if(!nameMatch.Success) {
                    throw new Exception(Configuration.GetLocalized("Invalid variable name specified!"));
                }
            }

            public override string MakeCpp()
            {
                if(Language == Language.C) {
					
                    return "(*PetriNet_getVariable(petriNet, (uint_fast32_t)(" + Prefix + Expression + ")))";
                }
                else if(Language == Language.Cpp) {
                    return "petriNet.getVariable(static_cast<std::uint_fast32_t>(" + Prefix + Expression + ")).value()";
                }

                throw new Exception("Should not get here!");
            }

            public override string MakeUserReadable()
            {
                return "$" + Expression;
            }

            public override bool Equals(object o)
            {
                return o is VariableExpression && ((VariableExpression)o).Expression == Expression;
            }

            public override int GetHashCode()
            {
                return Expression.GetHashCode();
            }

            public string Prefix {
                get {
                    if(Language == Language.C) {
                        return EnumName + "_";
                    }
                    else if(Language == Language.Cpp) {
                        return EnumName + "::";
                    }

                    throw new Exception("Should not get here!");
                }
            }

            public static string EnumName {
                get {
                    return "Petri_Var_Enum";
                }
            }
        }

        public class UnaryExpression : Expression
        {
            public UnaryExpression(Language language, Operator.Name o, Expression expr) : base(language, o)
            {
                this.Expression = expr;
            }

            public Expression Expression {
                get;
                private set;
            }

            public override bool UsesFunction(Function f)
            {
                return Expression.UsesFunction(f);
            }

            public override string MakeCpp()
            {
                string parenthesized = Expression.Parenthesize(this, this.Expression, this.Expression.MakeCpp());
                switch(this.Operator) {
                case Cpp.Operator.Name.FunCall:
                    throw new Exception(Configuration.GetLocalized("Already managed in FunctionInvocation class!"));
                case Cpp.Operator.Name.UnaryPlus:
                    return "+" + parenthesized;
                case Cpp.Operator.Name.UnaryMinus:
                    return "-" + parenthesized;
                case Cpp.Operator.Name.LogicalNot:
                    return "!" + parenthesized;
                case Cpp.Operator.Name.BitwiseNot:
                    return "~" + parenthesized;
                case Cpp.Operator.Name.Indirection:
                    return "*" + parenthesized;
                case Cpp.Operator.Name.AddressOf:
                    return "&" + parenthesized;
                case Cpp.Operator.Name.PreIncr:
                    return "++" + parenthesized;
                case Cpp.Operator.Name.PreDecr:
                    return "--" + parenthesized;
                case Cpp.Operator.Name.PostIncr:
                    return parenthesized + "++";
                case Cpp.Operator.Name.PostDecr:
                    return parenthesized + "--";
                }
                throw new Exception(Configuration.GetLocalized("Operator not implemented!"));
            }

            public override string MakeUserReadable()
            {
                string parenthesized = Expression.Parenthesize(this, this.Expression, this.Expression.MakeUserReadable());
                switch(this.Operator) {
                case Cpp.Operator.Name.FunCall:
                    throw new Exception(Configuration.GetLocalized("Already managed in FunctionInvocation class!"));
                case Cpp.Operator.Name.UnaryPlus:
                    return "+" + parenthesized;
                case Cpp.Operator.Name.UnaryMinus:
                    return "-" + parenthesized;
                case Cpp.Operator.Name.LogicalNot:
                    return "!" + parenthesized;
                case Cpp.Operator.Name.BitwiseNot:
                    return "~" + parenthesized;
                case Cpp.Operator.Name.Indirection:
                    return "*" + parenthesized;
                case Cpp.Operator.Name.AddressOf:
                    return "&" + parenthesized;
                case Cpp.Operator.Name.PreIncr:
                    return "++" + parenthesized;
                case Cpp.Operator.Name.PreDecr:
                    return "--" + parenthesized;
                case Cpp.Operator.Name.PostIncr:
                    return parenthesized + "++";
                case Cpp.Operator.Name.PostDecr:
                    return parenthesized + "--";
                }

                throw new Exception(Configuration.GetLocalized("Operator not implemented!"));
            }

            public override List<LiteralExpression> GetLiterals()
            {
                return Expression.GetLiterals();
            }
        }

        public class BinaryExpression : Expression
        {
            public BinaryExpression(Language language, Operator.Name o, Expression expr1, Expression expr2) : base(language, o)
            {
                this.Expression1 = expr1;
                this.Expression2 = expr2;
            }

            public Expression Expression1 {
                get;
                private set;
            }

            public Expression Expression2 {
                get;
                private set;
            }

            public override bool UsesFunction(Function f)
            {
                return Expression1.UsesFunction(f) || Expression2.UsesFunction(f);
            }

            public override string MakeCpp()
            {
                string e1 = Expression.Parenthesize(this, this.Expression1, this.Expression1.MakeCpp());
                string e2 = Expression.Parenthesize(this, this.Expression2, this.Expression2.MakeCpp());

                switch(this.Operator) {
                case Cpp.Operator.Name.Mult:
                    return e1 + " * " + e2;
                case Cpp.Operator.Name.Div:
                    return e1 + " / " + e2;
                case Cpp.Operator.Name.Mod:
                    return e1 + " % " + e2;
                case Cpp.Operator.Name.Plus:
                    return e1 + " + " + e2;
                case Cpp.Operator.Name.Minus:
                    return e1 + " - " + e2;
                case Cpp.Operator.Name.ShiftLeft:
                    return e1 + " << " + e2;
                case Cpp.Operator.Name.ShiftRight:
                    return e1 + " >> " + e2;
                case Cpp.Operator.Name.Less:
                    return e1 + " < " + e2;
                case Cpp.Operator.Name.LessEqual:
                    return e1 + " <= " + e2;
                case Cpp.Operator.Name.Greater:
                    return e1 + " > " + e2;
                case Cpp.Operator.Name.GreaterEqual:
                    return e1 + " >= " + e2;
                case Cpp.Operator.Name.Equal:
                    return e1 + " == " + e2;
                case Cpp.Operator.Name.NotEqual:
                    return e1 + " != " + e2;
                case Cpp.Operator.Name.BitwiseAnd:
                    return e1 + " & " + e2;
                case Cpp.Operator.Name.BitwiseXor:
                    return e1 + " ^ " + e2;
                case Cpp.Operator.Name.BitwiseOr:
                    return e1 + " | " + e2;
                case Cpp.Operator.Name.LogicalAnd:
                    return e1 + " && " + e2;
                case Cpp.Operator.Name.LogicalOr:
                    return e1 + " || " + e2;
                case Cpp.Operator.Name.Assignment:
                    return e1 + " = " + e2;
                case Cpp.Operator.Name.Comma:
                    return e1 + ", " + e2;
                }

                throw new Exception(Configuration.GetLocalized("Operator not implemented!"));
            }

            public override string MakeUserReadable()
            {
                string p1 = Expression.Parenthesize(this, this.Expression1, this.Expression1.MakeUserReadable());
                string p2 = Expression.Parenthesize(this, this.Expression2, this.Expression2.MakeUserReadable());
                switch(this.Operator) {
                case Cpp.Operator.Name.Mult:
                    return p1 + " * " + p2;
                case Cpp.Operator.Name.Div:
                    return p1 + " / " + p2;
                case Cpp.Operator.Name.Mod:
                    return p1 + " % " + p2;
                case Cpp.Operator.Name.Plus:
                    return p1 + " + " + p2;
                case Cpp.Operator.Name.Minus:
                    return p1 + " - " + p2;
                case Cpp.Operator.Name.ShiftLeft:
                    return p1 + " << " + p2;
                case Cpp.Operator.Name.ShiftRight:
                    return p1 + " >> " + p2;
                case Cpp.Operator.Name.Less:
                    return p1 + " < " + p2;
                case Cpp.Operator.Name.LessEqual:
                    return p1 + " <= " + p2;
                case Cpp.Operator.Name.Greater:
                    return p1 + " > " + p2;
                case Cpp.Operator.Name.GreaterEqual:
                    return p1 + " >= " + p2;
                case Cpp.Operator.Name.Equal:
                    return p1 + " == " + p2;
                case Cpp.Operator.Name.NotEqual:
                    return p1 + " != " + p2;
                case Cpp.Operator.Name.BitwiseAnd:
                    return p1 + " & " + p2;
                case Cpp.Operator.Name.BitwiseXor:
                    return p1 + " ^ " + p2;
                case Cpp.Operator.Name.BitwiseOr:
                    return p1 + " | " + p2;
                case Cpp.Operator.Name.LogicalAnd:
                    return p1 + " && " + p2;
                case Cpp.Operator.Name.LogicalOr:
                    return p1 + " || " + p2;
                case Cpp.Operator.Name.Assignment:
                    return p1 + " = " + p2;
                case Cpp.Operator.Name.PlusAssign:
                    return p1 + " += " + p2;
                case Cpp.Operator.Name.MinusAssign:
                    return p1 + " -= " + p2;
                case Cpp.Operator.Name.MultAssign:
                    return p1 + " *= " + p2;
                case Cpp.Operator.Name.DivAssign:
                    return p1 + " /= " + p2;
                case Cpp.Operator.Name.ModAssign:
                    return p1 + " %= " + p2;
                case Cpp.Operator.Name.ShiftLeftAssign:
                    return p1 + " <<= " + p2;
                case Cpp.Operator.Name.ShiftRightAssign:
                    return p1 + " >>= " + p2;
                case Cpp.Operator.Name.BitwiseAndAssig:
                    return p1 + " &= " + p2;
                case Cpp.Operator.Name.BitwiseXorAssign:
                    return p1 + " ^= " + p2;
                case Cpp.Operator.Name.BitwiseOrAssign:
                    return p1 + " |= " + p2;
                case Cpp.Operator.Name.Comma:
                    return p1 + ", " + p2;
                }

                throw new Exception(Configuration.GetLocalized("Operator not implemented!"));
            }

            public override List<LiteralExpression> GetLiterals()
            {
                var l1 = Expression1.GetLiterals();
                var l2 = Expression2.GetLiterals();
                l1.AddRange(l2);

                return l1;
            }
        }

        // Could have been TernaryExpression, but there is only one ternary operator in C++, so we already specialize it.
        public abstract class TernaryConditionExpression : Expression
        {
            protected TernaryConditionExpression(Language language, Expression expr1, Expression expr2, Expression expr3) : base(language, Cpp.Operator.Name.TernaryConditional)
            {
                this.Expression1 = expr1;
                this.Expression2 = expr2;
                this.Expression3 = expr3;
            }

            public Expression Expression1 {
                get;
                private set;
            }

            public Expression Expression2 {
                get;
                private set;
            }

            public Expression Expression3 {
                get;
                private set;
            }

            public override string MakeCpp()
            {
                // TODO: tbd
                throw new Exception(Configuration.GetLocalized("Operator not implemented!"));
            }

            public override string MakeUserReadable()
            {
                // TODO: tbd
                throw new Exception(Configuration.GetLocalized("Operator not implemented!"));
            }

            public override List<LiteralExpression> GetLiterals()
            {
                var l1 = Expression1.GetLiterals();
                var l2 = Expression2.GetLiterals();
                var l3 = Expression3.GetLiterals();
                l1.AddRange(l2);
                l1.AddRange(l3);

                return l1;
            }
        }

        public class ExpressionList : Expression
        {
            public ExpressionList(Language language, IEnumerable<Expression> expressions) : base(language, Cpp.Operator.Name.None)
            {
                Expressions = new List<Expression>(expressions);
            }

            public List<Expression> Expressions {
                get;
                private set;
            }

            public override bool UsesFunction(Function f)
            {
                foreach(var e in Expressions) {
                    if(e.UsesFunction(f))
                        return true;
                }
                return false;
            }

            public override string MakeCpp()
            {
                return String.Join(";\n", from e in Expressions
                                                      select e.MakeCpp());
            }

            public override string MakeUserReadable()
            {
                return String.Join("; ", from e in Expressions
                                                     select e.MakeUserReadable());
            }

            public override List<LiteralExpression> GetLiterals()
            {
                var l = new List<LiteralExpression>();
                foreach(var e in Expressions) {
                    l.AddRange(e.GetLiterals());
                }
                return l;
            }
        }
    }
}

