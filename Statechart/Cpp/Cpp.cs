using System;
using System.Collections.Generic;
using Regex = System.Text.RegularExpressions.Regex;
using System.Linq;

namespace Statechart
{
	namespace Cpp
	{
		public class Duration {
			public Duration(string s) {
				this.Value = s;
			}

			public string Value {
				get {
					return value;
				}
				set {
					Regex regex = new Regex(@"^[0-9]*(\.[0-9]+)?(_ns|_us|_ms|_s)");
					var match = regex.Match(value);

					if(!match.Success) {
						throw new ArgumentException("Invalid timeout duration.");
					}
					else {
						this.value = value;
					}

				}
			}

			private string value;
		}
	}
}

