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
    public class BinaryExpression : Expression
    {
        public BinaryExpression(Language language,
                                    Operator.Name o,
                                    Expression expr1,
                                    Expression expr2) : base(language,
                                                             o)
        {
            this.Expression1 = expr1;
            this.Expression2 = expr2;
        }

        public Expression Expression1 { get; private set; }

        public Expression Expression2 { get; private set; }

        public override bool UsesFunction(Function f)
        {
            return Expression1.UsesFunction(f) || Expression2.UsesFunction(f);
        }

        public override string MakeCpp()
        {
            string e1 = Expression.Parenthesize(this,
                                                    this.Expression1,
                                                    this.Expression1.MakeCpp());
            string e2 = Expression.Parenthesize(this,
                                                    this.Expression2,
                                                    this.Expression2.MakeCpp());
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
            string p1 = Expression.Parenthesize(this,
                                                    this.Expression1,
                                                    this.Expression1.MakeUserReadable());
            string p2 = Expression.Parenthesize(this,
                                                    this.Expression2,
                                                    this.Expression2.MakeUserReadable());
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
}

