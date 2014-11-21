using System;
using System.Collections.Generic;

namespace Petri
{
	namespace Cpp
	{
		public class Generator
		{
			public Generator()
			{
				this.Headers = "";
				this.Body = "";
			}

			public void AddHeader(string header)
			{
				Headers += "#include " + header + "\n";
			}

			public void AddLine(string line)
			{
				this.Body += line + "\n";
			}

			public void Add(string line)
			{
				this.Body += line;
			}

			public static Generator operator +(Generator gen, string s)
			{
				gen.AddLine(s);
				return gen;
			}

			public void Indent()
			{
				string val = this.Body;
				val = val.Replace("\t", "");

				string newVal = "";

				Dictionary<char, int> dict = new Dictionary<char, int>();
				dict['{'] = 1;
				dict['}'] = -1;
				dict['('] = 2;
				dict[')'] = -2;

				int currentIndent = 0;

				foreach(string line in val.Split('\n')) {
					int firstIndent = 0;
					int deltaNext = 0;
					for(int i = 0; i < line.Length; ++i) {
						if(dict.ContainsKey(line[i])) {
							int delta = dict[line[i]];

							if(i == 0 && delta < 0) {
								firstIndent = delta;
							}

							deltaNext += delta;
						}
					}

					string newLine = this.GetNTab(currentIndent + firstIndent) + line;
					currentIndent += deltaNext;

					newVal += newLine + "\n";
				}

				this.Body = newVal;
			}

			public void Write(string filename)
			{
				System.IO.File.WriteAllText(filename, this.Value);
			}

			public string Value {
				get {
					this.Indent();
					return this.Headers + "\n" + this.Body + "\n";
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

			private string GetNTab(int n)
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

