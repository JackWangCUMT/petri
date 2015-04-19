using System;
using System.Collections.Generic;

namespace Petri
{
	namespace Cpp {
		public class Param {
			public Param(Type type, string name) {
				this.Type = type;
				this.Name = name.Trim();
			}

			public Type Type { get; private set; }
			public string Name { get; private set; }
		}

		public class Function
		{
			// With 'enclosing', you can create a static method, or a namespace-enclosed function
			public Function(Type returnType, Scope enclosing, string name, bool template)
			{
				this.ReturnType = returnType;
				this.Name = name.Trim();

				Parameters = new List<Param>();
				this.Enclosing = enclosing;
			}

			public Type ReturnType {
				get;
				private set;
			}

			public string Name {
				get;
				private set;
			}

			public bool Template {
				get;
				private set;
			}

			public string QualifiedName {
				get {
					string qn = "";
					if(Enclosing != null)
						qn = Enclosing.ToString();
					return qn + Name;
				}
			}

			public Scope Enclosing {
				get;
				private set;
			}

			public string Header {
				get;
				set;
			}

			public List<Param> Parameters {
				get;
				private set;
			}

			public void AddParam(Param p) {
				Parameters.Add(p);
			}

			public virtual string Signature {
				get {
					string s = this.QualifiedName + "(";
					foreach(var p in Parameters) {
						s += p.Type + " " + p.Name + ", ";
					}
					if(s.EndsWith(", ")) {
						s = s.Substring(0, s.Length - 2);
					}
					s += ")";

					return s;
				}
			}

			public static string ParameterPattern {
				get {
					return @"\((?<parameters>([^)]*\))*)";
				}
			}
			
			public static string StaticInlinePattern {
				get {
					return "((?<static>((static)?)) ?(inline)?) ";
				}
			}

			public static string FunctionPattern {
				get {
					return @"^(" + Function.StaticInlinePattern + " )?" + Type.RegexPattern + @" ?" + Parser.NamePattern + " ?" + Function.ParameterPattern;// + Parser.DeclarationEndPattern;
				}
			}

			public static System.Text.RegularExpressions.Regex Regex {
				get {
					return new System.Text.RegularExpressions.Regex(FunctionPattern);
				}
			}
		}

		public class Method : Function {
			public Method(Type classType, Type returnType, string name, string attrib, bool template) : base(returnType, Scope.MakeFromClass(classType), name, template) {
				Attributes = attrib;
				Class = classType;
			}

			public string Attributes {
				get;
				private set;
			}

			public Type Class {
				get;
				private set;
			}

			public override string Signature {
				get {
					string sig = base.Signature;
					return sig + (this.Attributes.Length > 0 ? " " + this.Attributes : "");
				}
			}
			
			public static string AttributesPattern {
				get {
					return "(?<attributes>(const|volatile|override|final|nothrow|= ?(default|0|delete)| )*)";
				}
			}

			public static string MethodPattern {
				get {
					return @"^(" + Function.StaticInlinePattern + " )?" + Type.RegexPattern + @" " + Parser.NamePattern + " ?" + Function.ParameterPattern + " ?" + AttributesPattern + " ?";// + Parser.DeclarationEndPattern;
				}
			}

			public new static System.Text.RegularExpressions.Regex Regex {
				get {
					return new System.Text.RegularExpressions.Regex(MethodPattern);
				}
			}
		}
	}
}

