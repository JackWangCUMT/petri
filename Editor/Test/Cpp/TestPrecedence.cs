using NUnit.Framework;
using System;
using Petri.Cpp;

namespace TestPetri
{
    [TestFixture()]
    public class TestPrecedence
    {
        [Test()]
        public void TestPrecedence1()
        {
            // GIVEN a composition of an addition and multiplication
            // WHEN we create an expression from it
            var e = Expression.CreateFromString("3+4*5");

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

