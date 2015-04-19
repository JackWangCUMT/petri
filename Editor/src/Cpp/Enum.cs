using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Petri
{
	namespace Cpp {
		public class Enum : IEquatable<Enum> {
			public Enum(String name, IEnumerable<string> members) {
				Name = name;
				Members = members.ToArray();
			}

			public Enum(string commaSeparatedList) {
				var lst = commaSeparatedList.Split(new char[]{','}, StringSplitOptions.None);
				Regex name = new Regex(Cpp.Parser.NamePattern);

				bool ok = lst.Length >= 2;
				if(ok) {
					foreach(var v in lst) {
						Match nameMatch = name.Match(v);
						if(!nameMatch.Success || nameMatch.Value != v) {
							ok = false;
							break;
						}
					}
				}

				if(!ok) {
					throw new Exception("Invalid comma separated-stored C++ enum");
				}

				Name = lst[0];
				Members = lst.Skip(1).ToArray();
			}

			public override string ToString() {
				return Name + "," + String.Join(",", Members);
			}

			public string Name {
				get;
				private set;
			}

			public Cpp.Type Type {
				get {
					return new Cpp.Type(Name, Cpp.Scope.EmptyScope);
				}
			}

			public string[] Members {
				get;
				private set;
			}

			public bool Equals(Cpp.Enum e) {
				if(Name != e.Name || Members.Length != e.Members.Length)
					return false;

				for(int i = 0; i < Members.Length; ++i) {
					if(Members[i] != e.Members[i])
						return false;
				}

				return true;
			}
		}
	}
}

