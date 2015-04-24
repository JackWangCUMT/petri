using System;
using System.Collections.Generic;
using Regex = System.Text.RegularExpressions.Regex;
using System.Linq;

namespace Petri
{
	namespace Cpp
	{
		public class Duration {
			public Duration(string s) {
				this.Value = s;
			}

			public string Value {
				get {
					return _value;
				}
				set {
					Regex regex = new Regex(@"^[0-9]*(\.[0-9]+)?(ns|us|ms|s)");
					var match = regex.Match(value);

					if(!match.Success) {
						throw new ArgumentException("Invalid timeout duration.");
					}
					else {
						this._value = value;
					}

				}
			}

			private string _value;
		}
	}
}

