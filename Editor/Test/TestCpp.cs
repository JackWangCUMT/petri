using NUnit.Framework;
using System;
using Petri.Cpp;

namespace TestPetri
{
    [TestFixture()]
    public class TestCpp
    {
        Random _random = new Random();

        string RandomLiteral() {
            return _random.Next().ToString();
        }

        [Test()]
        public void TestLiteral()
        {
            var literal = RandomLiteral();

            var e = Expression.CreateFromString(literal, null, false);
            Assert.IsInstanceOf<LiteralExpression>(e);

            var lit = e as LiteralExpression;

            Assert.AreNotSame(literal, lit.Expression);
            Assert.AreEqual(literal, lit.Expression);
        }

        [Test()]
        public void TestLiteralsLiterals()
        {
            var literal = RandomLiteral();

            var e = Expression.CreateFromString(literal, null, false);

            var literals = e.GetLiterals();
            Assert.AreEqual(1, literals.Count);
            Assert.Contains(e, literals);
        }

        [Test()]
        public void TestAddition()
        {
            var e = Expression.CreateFromString("3+4", null, false);
            Assert.IsInstanceOf<BinaryExpression>(e);
            var bin = e as BinaryExpression;
            Assert.AreEqual(Operator.Name.Plus, bin.Operator);

            Assert.IsInstanceOf<LiteralExpression>(bin.Expression1);
            Assert.IsInstanceOf<LiteralExpression>(bin.Expression1);

            Assert.AreEqual(((LiteralExpression)bin.Expression1).Expression, "3");
            Assert.AreEqual(((LiteralExpression)bin.Expression2).Expression, "4");
        }

        [Test()]
        public void TestAdditionMakeCpp()
        {
            var e = Expression.CreateFromString("3+4");
            Assert.AreEqual("3 + 4", e.MakeCpp());
        }

        [Test()]
        public void TestAdditionMakeUserReadable()
        {
            var e = Expression.CreateFromString("3+4");
            Assert.AreEqual("3 + 4", e.MakeUserReadable());
        }
            
        [Test()]
        public void TestPrecedence1()
        {
            var e = Expression.CreateFromString("3+4*5");

            Assert.IsInstanceOf<BinaryExpression>(e);

            var bin1 = e as BinaryExpression;
            Assert.AreEqual(Operator.Name.Plus, e.Operator);

            Assert.IsInstanceOf<BinaryExpression>(bin1.Expression2);
            Assert.AreEqual(Operator.Name.Mult, bin1.Expression2.Operator);
        }
    }
}

