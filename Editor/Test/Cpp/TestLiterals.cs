using NUnit.Framework;
using System;
using Petri.Cpp;

namespace TestPetri.Cpp
{
    [TestFixture()]
    public class TestLiterals
    {
        [Test()]
        public void TestLiteral()
        {
            // GIVEN a random ltteral string
            var literal = Utility.RandomLiteral();

            // WHEN we create an expression from it
            var e = Expression.CreateFromString(literal);

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
            var literal = Utility.RandomLiteral();

            // WHEN we create an expression from it
            var e = Expression.CreateFromString(literal);

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
            var e = Expression.CreateFromString(addition);

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

