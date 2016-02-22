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

using System;
using System.Linq;
using Petri.Editor.Code;

namespace Petri.Test
{
    public class CodeUtility
    {
        static Random _random = new Random();

        public enum LiteralType
        {
            Char,
            String,
            UInt,
            ULong,
            ULongLong,
            Float,
            Double,
            LongDouble
        }

        public static string RandomChar(bool inString)
        {
            string c = new string((char)_random.Next(32, 128), 1);
            if(c == @"\" || ((inString && c == "\"") || c == "'")) {
                c = @"\" + c;
            }
            return c;
        }

        public static string RandomLiteral(LiteralType type)
        {
            switch(type) {
            case LiteralType.Char:
                return "'" + RandomChar(false) + "'";
            case LiteralType.String:
                int count = _random.Next(50);
                string result = "";
                for(int i = 0; i < count; ++i) {
                    result += RandomChar(true);
                }

                return '"' + result + '"';

            case LiteralType.UInt:
                return _random.Next(1 << 16).ToString() + "U";
            case LiteralType.ULong:
                return _random.Next(1 << 16).ToString() + "UL";
            case LiteralType.ULongLong:
                return _random.Next(1 << 16).ToString() + "ULL";


            case LiteralType.Float:
                return (_random.NextDouble() * _random.Next(1, 30)).ToString(System.Globalization.CultureInfo.InvariantCulture) + "F";
            case LiteralType.Double:
                return (_random.NextDouble() * _random.Next(1, 30)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            case LiteralType.LongDouble:
                return (_random.NextDouble() * _random.Next(1, 30)).ToString(System.Globalization.CultureInfo.InvariantCulture) + "L";
            }

            return _random.Next().ToString();
        }

        public static string RandomLiteral()
        {
            var types = System.Enum.GetValues(typeof(LiteralType));
            var type = (LiteralType)types.GetValue(_random.Next(types.Length));
            var lit = RandomLiteral(type);

            return lit;
        }

        public static string RandomIdentifier()
        {
            int length = _random.Next(1, 50);
            string domain = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_";
            var result = new string(Enumerable.Repeat(domain, length).Select(s => s[_random.Next(s.Length)]).ToArray());

            if(Char.IsDigit(result[0])) {
                result = domain[_random.Next(52)] + result.Substring(1);
            }

            return result;
        }

        public static Language RandomLanguage() {
            var languages = System.Enum.GetValues(typeof(Language));
            return (Language)languages.GetValue(_random.Next(languages.Length));
        }
    }
}

