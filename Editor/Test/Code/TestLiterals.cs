/*
 * Copyright (c) 2016 Rémi Saurel
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

using NUnit.Framework;
using System;
using Petri.Editor.Code;

namespace Petri.Test.Code
{
    [TestFixture()]
    public class TestLiterals
    {
        [Test(), Repeat(20)]
        public void TestLiteral()
        {
            // GIVEN a random literal string
            var literal = CodeUtility.RandomLiteral();

            // WHEN we create an expression from it
            var e = Expression.CreateFromString(literal, Language.CSharp);

            // THEN it is a LiteralExpression
            Assert.IsInstanceOf<LiteralExpression>(e);

            var lit = e as LiteralExpression;

            // AND the textual content of the expression is the same as the initial literal
            Assert.AreNotSame(literal, lit.Expression);
            Assert.AreEqual(literal, lit.Expression);
        }

        [Test()]
        public void TestLiteralWithAt()
        {
            // GIVEN a literal string with an @ symbol inside
            var literal = "\"a string with a @ symbol…\"";

            // WHEN we create an expression from it
            var e = Expression.CreateFromString(literal, Language.CSharp);

            // THEN it is a LiteralExpression
            Assert.IsInstanceOf<LiteralExpression>(e);

            var lit = e as LiteralExpression;

            // AND the textual content of the expression is the same as the initial literal
            Assert.AreNotSame(literal, lit.Expression);
            Assert.AreEqual(literal, lit.Expression);
        }

        [Test()]
        public void TestLiteralWithQuote1()
        {
            // GIVEN a literal string with a singly quoted character inside
            var literal = "'a'";

            // WHEN we create an expression from it
            var e = Expression.CreateFromString(literal, Language.CSharp);

            // THEN it is a LiteralExpression
            Assert.IsInstanceOf<LiteralExpression>(e);

            var lit = e as LiteralExpression;

            // AND the textual content of the expression is the same as the initial literal
            Assert.AreNotSame(literal, lit.Expression);
            Assert.AreEqual(literal, lit.Expression);
        }

        [Test()]
        public void TestLiteralWithQuote2()
        {
            // GIVEN a literal string with a singly quoted quote character inside
            var literal = "'\\''";

            // WHEN we create an expression from it
            var e = Expression.CreateFromString(literal, Language.CSharp);

            // THEN it is a LiteralExpression
            Assert.IsInstanceOf<LiteralExpression>(e);

            var lit = e as LiteralExpression;

            // AND the textual content of the expression is the same as the initial literal
            Assert.AreNotSame(literal, lit.Expression);
            Assert.AreEqual(literal, lit.Expression);
        }

        [Test()]
        public void TestLiteralWithQuote3()
        {
            // GIVEN a literal string with a singly quoted double quote character inside
            var literal = "'\"'";

            // WHEN we create an expression from it
            var e = Expression.CreateFromString(literal, Language.CSharp);

            // THEN it is a LiteralExpression
            Assert.IsInstanceOf<LiteralExpression>(e);

            var lit = e as LiteralExpression;

            // AND the textual content of the expression is the same as the initial literal
            Assert.AreNotSame(literal, lit.Expression);
            Assert.AreEqual(literal, lit.Expression);
        }

        [Test()]
        public void TestLiteralWithDoubleQuote1()
        {
            // GIVEN a literal string with a doubly quoted character inside
            var literal = "\"a\"";

            // WHEN we create an expression from it
            var e = Expression.CreateFromString(literal, Language.CSharp);

            // THEN it is a LiteralExpression
            Assert.IsInstanceOf<LiteralExpression>(e);

            var lit = e as LiteralExpression;

            // AND the textual content of the expression is the same as the initial literal
            Assert.AreNotSame(literal, lit.Expression);
            Assert.AreEqual(literal, lit.Expression);
        }

        [Test()]
        public void TestLiteralWithDoubleQuote2()
        {
            // GIVEN a literal string with a doubly quoted backslash character inside
            var literal = "\"\\\"";

            // WHEN we create an expression from it
            var e = Expression.CreateFromString(literal, Language.CSharp);

            // THEN it is a LiteralExpression
            Assert.IsInstanceOf<LiteralExpression>(e);

            var lit = e as LiteralExpression;

            // AND the textual content of the expression is the same as the initial literal
            // FIXME: ?
            //Assert.AreNotSame(literal, lit.Expression);
            Assert.AreEqual(literal, lit.Expression);
        }

        [Test()]
        public void TestLiteralWithDoubleQuote3()
        {
            // GIVEN a literal string with a doubly quoted quote character inside
            var literal = "\"'\"";

            // WHEN we create an expression from it
            var e = Expression.CreateFromString(literal, Language.CSharp);

            // THEN it is a LiteralExpression
            Assert.IsInstanceOf<LiteralExpression>(e);

            var lit = e as LiteralExpression;

            // AND the textual content of the expression is the same as the initial literal
            Assert.AreNotSame(literal, lit.Expression);
            Assert.AreEqual(literal, lit.Expression);
        }

        [Test()]
        public void TestLiteralWithDoubleQuote4()
        {
            // GIVEN a literal string with a doubly quoted double quote character inside
            var literal = "\"\\\"\"";

            // WHEN we create an expression from it
            var e = Expression.CreateFromString(literal, Language.CSharp);

            // THEN it is a LiteralExpression
            Assert.IsInstanceOf<LiteralExpression>(e);

            var lit = e as LiteralExpression;

            // AND the textual content of the expression is the same as the initial literal
            Assert.AreNotSame(literal, lit.Expression);
            Assert.AreEqual(literal, lit.Expression);
        }

        [Test()]
        public void TestLiteralsLiterals()
        {
            // GIVEN a random literal string
            var literal = CodeUtility.RandomLiteral();

            // WHEN we create an expression from it
            var e = Expression.CreateFromString(literal, Language.CSharp);

            var literals = e.GetLiterals();

            // THEN the set of literals in the whole expression is only {initialLiteral}
            Assert.AreEqual(1, literals.Count);
            Assert.Contains(e, literals);
        }

        [Test()]
        public void TestAddition()
        {
            // GIVEN a simple addition expression
            var addition = "3+4";

            // WHEN we create an expression from it
            var e = Expression.CreateFromString(addition, Language.CSharp);

            // THEN the resulting expression is a BinaryExpression, consisting of the right operator and the right literal subexpressions.
            Assert.IsInstanceOf<BinaryExpression>(e);
            var bin = e as BinaryExpression;
            Assert.AreEqual(Operator.Name.Plus, bin.Operator);

            Assert.IsInstanceOf<LiteralExpression>(bin.Expression1);
            Assert.IsInstanceOf<LiteralExpression>(bin.Expression1);

            Assert.AreEqual(((LiteralExpression)bin.Expression1).Expression, "3");
            Assert.AreEqual(((LiteralExpression)bin.Expression2).Expression, "4");
        }
    }
}

