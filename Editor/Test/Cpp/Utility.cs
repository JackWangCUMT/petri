using System;

namespace TestPetri.Cpp
{
    public class Utility
    {
        static Random _random = new Random();

        public static string RandomLiteral() {
            return _random.Next().ToString();
        }
    }
}

