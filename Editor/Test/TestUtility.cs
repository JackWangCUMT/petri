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
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Petri.Test
{
    public class TestUtility
    {
        public static bool PublicInstancePropertiesEqual <T>(T self, T to, params string[] ignore) where T : class
        {
            return InstanceProperyiesEqualFlags(self,
                                                to,
                                                System.Reflection.BindingFlags.Public,
                                                ignore);
        }

        public static bool InstancePropertiesEqual <T>(T self, T to, params string[] ignore) where T : class
        {
            return InstanceProperyiesEqualFlags(self,
                                                to,
                                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
                                                ignore);
        }

        /*
         * Adapted from code form "Big T", available at https://stackoverflow.com/a/844855/1276072 (version of 2016-02-19)
         */
        static bool InstanceProperyiesEqualFlags <T>(T self,
                                                     T to,
                                                     System.Reflection.BindingFlags flags,
                                                     params string[] ignore) where T : class
        {
            if(self != null && to != null) {
                Type type = typeof(T);
                List<string> ignoreList = new List<string>(ignore);
                foreach(System.Reflection.PropertyInfo pi in type.GetProperties(flags | System.Reflection.BindingFlags.Instance)) {
                    if(!ignoreList.Contains(pi.Name)) {
                        object selfValue = type.GetProperty(pi.Name).GetValue(self, null);
                        object toValue = type.GetProperty(pi.Name).GetValue(to, null);

                        if(selfValue != toValue && (selfValue == null || !selfValue.Equals(toValue))) {
                            return false;
                        }
                    }
                }
                return true;
            }
            return self == to;
        }

        public static string RandomPath(string file = "") {
            var random = new Random();

            int count = random.Next(0, 10);
            if(count == 0) {
                return ".";
            }

            string path;

            int rootType = random.Next(3);
            if(rootType == 0) {
                path = "" + System.IO.Path.PathSeparator;
            }
            else if(rootType == 1) {
                path = "." + System.IO.Path.PathSeparator;
            }
            else {
                path = "";
            }

            string domain = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_$-+=*/,.;&:";
            for(int i = 0; i < count; ++i) {
                int length = random.Next(1, 50);

                var component = new string(Enumerable.Repeat(domain, length).Select(s => s[random.Next(s.Length)]).ToArray());
                path += component + System.IO.Path.PathSeparator;
            }

            path += file;

            return path;
        }
    }
}

