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
using System.Linq;

namespace Petri.Editor.Code
{
    public class EmptyExpression : Expression
    {
        public EmptyExpression(Language language, bool doWeCare) : base(language,
                                                                        Code.Operator.Name.None)
        {
        }

        public override bool UsesFunction(Function f)
        {
            return false;
        }

        public override string MakeCode()
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

        public bool DoWeCare { get; private set; }

    }

    public class BracketedExpression : Expression
    {
        public BracketedExpression(Expression b, Expression expr, Expression a) : base(b.Language,
                                                                                       Code.Operator.Name.None)
        {
            Before = b;
            Expression = expr;
            After = a;
        }

        public Expression Expression { get; private set; }

        public Expression Before { get; private set; }

        public Expression After { get; set; }

        public override bool UsesFunction(Function f)
        {
            return Expression.UsesFunction(f) || Before.UsesFunction(f) || After.UsesFunction(f);
        }

        public override string MakeCode()
        {
            return Before.MakeCode() + "{" + Expression.MakeCode() + "}" + After.MakeCode();
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
                var tup = Code.Expression.Preprocess(s);
                int currentIndex = 0;
                while(currentIndex < tup.Item1.Length) {
                    int index = tup.Item1.Substring(currentIndex).IndexOf("@") + currentIndex;
                    if(index == -1)
                        break;
                    int lastIndex = tup.Item1.Substring(index + 1).IndexOf("@") + index + 1;
                    int expr = int.Parse(tup.Item1.Substring(index + 1,
                                                             lastIndex - (index + 1)));
                    if(tup.Item2[expr].Item1 == ExprType.Brackets) {
                        return new BracketedExpression(Code.Expression.CreateFromPreprocessedString(tup.Item1.Substring(0,
                                                                                                                        index),
                                                                                                    language,
                                                                                                    null,
                                                                                                    null,
                                                                                                    tup.Item2,
                                                                                                    true),
                                                       Code.Expression.CreateFromPreprocessedString(tup.Item2[expr].Item2.Substring(1,
                                                                                                                                    tup.Item2[expr].Item2.Length - 2),
                                                                                                    language,
                                                                                                    null,
                                                                                                    null,
                                                                                                    tup.Item2,
                                                                                                    true),
                                                       Code.Expression.CreateFromPreprocessedString(tup.Item1.Substring(lastIndex + 1),
                                                                                                    language,
                                                                                                    null,
                                                                                                    null,
                                                                                                    tup.Item2,
                                                                                                    true));
                    }
                    else {
                        currentIndex = lastIndex + 1;
                    }
                }
            }
            return new LiteralExpression(language, s);
        }

        protected LiteralExpression(Language language, string expr) : base(language,
                                                                           Code.Operator.Name.None)
        {
            Expression = expr.Trim();
        }

        public virtual string Expression { get; set; }

        public override bool UsesFunction(Function f)
        {
            return false;
        }

        public override string MakeCode()
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
        public VariableExpression(string expr, Language language) : base(language,
                                                                         expr)
        {
            Regex name = new Regex(Parser.GetNamePattern(true));
            Match nameMatch = name.Match(expr);
            if(!nameMatch.Success) {
                throw new Exception(Configuration.GetLocalized("Invalid variable name specified!"));
            }
        }

        public override string MakeCode()
        {
            if(Language == Language.C) {
                return "(*PetriNet_getVariable(petriNet, (uint_fast32_t)(" + Prefix + Expression + ")))";
            }
            else if(Language == Language.Cpp) {
                return "petriNet.getVariable(static_cast<std::uint_fast32_t>(" + Prefix + Expression + ")).value()";
            }
            else if(Language == Language.CSharp) {
                return "petriNet.GetVariable((UInt32)(" + Prefix + Expression + ")).Value";
            }
            throw new Exception("VariableExpression.MakeCode: Should not get there!");
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
                else if(Language == Language.CSharp) {
                    return EnumName + ".";
                }
                throw new Exception("VariableExpression.Prefix: Should not get there!");
            }
        }

        public static string EnumName { get { return "Petri_Var_Enum"; } }

    }

    // Could have been TernaryExpression, but there is only one ternary operator in C++, so we already specialize it.
    public abstract class TernaryConditionExpression : Expression
    {
        protected TernaryConditionExpression(Language language,
                                             Expression expr1,
                                             Expression expr2,
                                             Expression expr3) : base(language,
                                                                      Code.Operator.Name.TernaryConditional)
        {
            this.Expression1 = expr1;
            this.Expression2 = expr2;
            this.Expression3 = expr3;
        }

        public Expression Expression1 { get; private set; }

        public Expression Expression2 { get; private set; }

        public Expression Expression3 { get; private set; }

        public override string MakeCode()
        {
            return Parenthesize(this, Expression1, Expression1.MakeCode()) + " ? " + Parenthesize(this,
                                                                                                  Expression1,
                                                                                                  Expression2.MakeCode()) + " : " + Parenthesize(this,
                                                                                                                                                 Expression3,
                                                                                                                                                 Expression3.MakeCode());
        }

        public override string MakeUserReadable()
        {
            return Parenthesize(this, Expression1, Expression1.MakeUserReadable()) + " ? " + Parenthesize(this,
                                                                                                          Expression1,
                                                                                                          Expression2.MakeUserReadable()) + " : " + Parenthesize(this,
                                                                                                                                                                 Expression3,
                                                                                                                                                                 Expression3.MakeUserReadable());
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
        public ExpressionList(Language language, IEnumerable<Expression> expressions) : base(language,
                                                                                             Code.Operator.Name.None)
        {
            Expressions = new List<Expression>(expressions);
        }

        public List<Expression> Expressions { get; private set; }

        public override bool UsesFunction(Function f)
        {
            foreach(var e in Expressions) {
                if(e.UsesFunction(f))
                    return true;
            }
            return false;
        }

        public override string MakeCode()
        {
            return String.Join(";\n",
                               from e in Expressions
                                        select e.MakeCode());
        }

        public override string MakeUserReadable()
        {
            return String.Join("; ",
                               from e in Expressions
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

