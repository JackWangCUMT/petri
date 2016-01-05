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

namespace Petri.Editor.Cpp
{
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
            string parenthesized = Expression.Parenthesize(this,
                                                               this.Expression,
                                                               this.Expression.MakeUserReadable());
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
}

