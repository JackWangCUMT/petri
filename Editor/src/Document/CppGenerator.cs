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
using ExprType = Petri.Cpp.Expression.ExprType;

namespace Petri
{
	namespace Cpp
	{
		public class Generator
		{
			public Generator()
			{
				Headers = "";
				Body = "";
				Indentation = true;
			}

			public bool Indentation {
				get;
				set;
			}

			public void AddHeader(string header)
			{
				Headers += "#include " + header + "\n";
			}

			public void AddLine(string line)
			{
				Body += line + "\n";
			}

			public void Add(string line)
			{
				Body += line;
			}

			public static Generator operator +(Generator gen, string s)
			{
				gen.AddLine(s);
				return gen;
			}

			public static string Indent(string val)
			{
				string newVal = "";

				Dictionary<char, int> dict = new Dictionary<char, int>();
				dict['{'] = 1;
				dict['}'] = -1;
				dict['('] = 2;
				dict[')'] = -2;

				int currentIndent = 0;

				var nesting = new Stack<Cpp.Expression.ExprType>();

				foreach(string line in val.Split('\n')) {
					int existingIndent = 0;
					for(int i = 0; i < line.Length; ++i) {
						if(line[i] == '\t') {
							++existingIndent;
						}
						else {
							break;
						}
					}
					int firstIndent = 0;
					int deltaNext = 0;

					for(int i = 0; i < line.Length; ++i) {
						int delta = 0;
						switch(line[i]) {
						case '(':
							nesting.Push(ExprType.Parenthesis);
							delta = 2;
							break;
						case ')':
							if(nesting.Count > 0 && nesting.Peek() == ExprType.Parenthesis) {
								delta = -2;
								nesting.Pop();
							}
							break;
						case '{':
							delta = 1;
							nesting.Push(ExprType.Brackets);
							break;
						case '}':
							if(nesting.Count > 0 && nesting.Peek() == ExprType.Brackets) {
								delta = -1;
								nesting.Pop();
							}
							break;
						case '[':
							delta = 2;
							nesting.Push(ExprType.Subscript);
							break;
						case ']':
							if(nesting.Count > 0 && nesting.Peek() == ExprType.Subscript) {
								delta = -2;
								nesting.Pop();
							}
							break;
						case '"':
							// First quote
							if(nesting.Count == 0 || (nesting.Peek() != ExprType.DoubleQuote && nesting.Peek() != ExprType.Quote)) {
								nesting.Push(ExprType.DoubleQuote);
							}
							// Second quote
							else if(nesting.Count > 0 && nesting.Peek() == ExprType.DoubleQuote && line[i - 1] != '\\') {
								nesting.Pop();
							}
							break;
						case '\'':
							// First quote
							if(nesting.Count == 0 || (nesting.Peek() != ExprType.Quote && nesting.Peek() != ExprType.DoubleQuote)) {
								nesting.Push(ExprType.Quote);
							}
							// Second quote
							else if(nesting.Count > 0 && nesting.Peek() == ExprType.Quote && line[i - 1] != '\\') {
								nesting.Pop();
							}
							break;
						}

						if(i == 0 && delta < 0) {
							firstIndent = delta;
						}

						deltaNext += delta;
					}

					string newLine = GetNTab(currentIndent + firstIndent - existingIndent) + line;
					currentIndent += deltaNext;

					newVal += newLine + "\n";
				}

				return newVal;
			}

			public void Write(string filename)
			{
				System.IO.File.WriteAllText(filename, this.Value);
			}

			public string Value {
				get {
					if(Indentation) {
						Body = Indent(Body);
					}
					return Headers + "\n" + Body + "\n";
				}
			}

			public string Headers {
				get;
				set;
			}

			public string Body {
				get;
				set;
			}

			private static string GetNTab(int n)
			{
				string s = "";
				for(int i = 0; i < n; ++i) {
					s += '\t';
				}

				return s;
			}
		}
	}
}

