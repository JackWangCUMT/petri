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
using Petri.Editor.Cpp;

namespace Petri.Test.Cpp
{
    [TestFixture()]
    public class TestPrecedence
    {
        [Test()]
        public void TestPrecedence1()
        {
            // GIVEN a composition of an addition and multiplication
            // WHEN we create an expression from it
            var e = Expression.CreateFromString("3+4*5", Language.CSharp);

            // THEN we get a binary expression representing the addition
            Assert.IsInstanceOf<BinaryExpression>(e);

            var bin1 = e as BinaryExpression;
            Assert.AreEqual(Operator.Name.Plus, e.Operator);

            // AND containing a binary expression representing the multiplication
            Assert.IsInstanceOf<BinaryExpression>(bin1.Expression2);
            Assert.AreEqual(Operator.Name.Mult, bin1.Expression2.Operator);
        }
    }
}

