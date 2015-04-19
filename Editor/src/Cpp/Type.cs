using System;
using System.Collections.Generic;
using System.Linq;

namespace Petri
{
	namespace Cpp {
		public enum CVQualifier {None = 0, Const = 1, Volatile = 2};

		public class Type : IEquatable<Type>, IEquatable<string> {
			public Type(string s, Scope enclosing) {
				s.Replace("*", " * ");
				s = s.TrimSpaces();

				s = s.TrimStart(new char[]{' '}).TrimEnd(new char[]{' '});

				if(s.EndsWith("&")) {
					isReference = true;
					s = s.Substring(0, s.Length - 1);
				}

				CVQualifier nameQualifier = CVQualifier.None;

				// Parse right-associative cv-qualifiers, as in const int
				if(s.StartsWith("const volatile ") || s.StartsWith("volatile const ")) {
					s = s.Substring("const volatile ".Length);
					nameQualifier = CVQualifier.Const | CVQualifier.Volatile;
				}
				else if(s.StartsWith("const ")) {
					s = s.Substring("const ".Length);
					nameQualifier = CVQualifier.Const;
				}
				else if(s.StartsWith("volatile ")) {
					s = s.Substring("volatile ".Length);
					nameQualifier = CVQualifier.Volatile;
				}

				var q = new List<CVQualifier>();

				s = this.ParseLeftAssociativeQualifiers(s, q).TrimEnd(new char[]{' '});

				if(s.EndsWith(" const volatile") || s.EndsWith(" volatile const")) {
					s = s.Substring(0, s.Length - " const volatile".Length);
					nameQualifier = CVQualifier.Const | CVQualifier.Volatile;
				}
				else if(s.EndsWith(" const")) {
					s = s.Substring(0, s.Length - " const".Length);
					nameQualifier |= CVQualifier.Const;
				}
				else if(s.EndsWith(" volatile")) {
					s = s.Substring(0, s.Length - " volatile".Length);
					nameQualifier |= CVQualifier.Volatile;
				}

				q.Insert(0, nameQualifier);

				int index = s.IndexOf('<');
				if(index != -1) {
					/*var t = Parser.SyntacticSplit(s.Substring(index + 1, s.Length - index - 2));
					foreach(var e in t) {
						template.Add(Expression.CreateFromString<Expression>(e, null, new List<Function>()));
					}*/
					template = s.Substring(index + 1, s.Length - index - 2);
				}
				else {
					template = "";
					index = s.Length;
				}

				var tup = Parser.ExtractScope(s.Substring(0, index).Trim());
				name = tup.Item2;
				Enclosing = Scope.MakeFromScopes(enclosing, tup.Item1);

				cvQualifiers = q.ToArray();
			}

			public override string ToString() {
				string val = Name;
				if(Template.Length > 0) {
					/*string v = "";
					foreach(var e in Template) {
						v += e.MakeUserReadable();
					}
					val += "<" + val + ">";*/
					val += "<" + template + ">";
				}

				for(int i = 0; i < cvQualifiers.Length; ++i) {
					if(i != 0)
						val += " *";

					if((cvQualifiers[i] | CVQualifier.Volatile) == cvQualifiers[i])
						val += " volatile";
					if((cvQualifiers[i] | CVQualifier.Const) == cvQualifiers[i])
						val += " const";
				}

				if(this.IsReference)
					val += " &";

				return val;
			}

			public string Name {
				get {
					return name;
				}
			}

			public Scope Enclosing {
				get;
				private set;
			}

			public /*List<Expression>*/string Template {
				get {
					return template;
				}
			}

			public IEnumerable<CVQualifier> CVQualifiers {
				get {
					return cvQualifiers;
				}
			}

			public bool IsReference {
				get {
					return isReference;
				}
			}

			public bool Equals(Type type) {
				if(Name != type.Name || Template != type.Template || IsReference != type.IsReference)
					return false;

				if(cvQualifiers.Length != type.cvQualifiers.Length)
					return false;

				for(int i = 0; i < cvQualifiers.Length; ++i) {
					if(cvQualifiers[i] != type.cvQualifiers[i])
						return false;
				}

				if(!Enclosing.Equals(type.Enclosing))
					return false;

				return true;
			}

			public bool Equals(string type) {
				return this.Equals(new Type(type, Enclosing));
			}

			// Is var1 = var2 a valid expression, where var1's type is 'type' and var2's type is 'this' ?
			public bool ConvertibleTo(Type type) {
				if(this.Name != type.Name || this.Template != this.Template || this.cvQualifiers.Length != type.cvQualifiers.Length)
					return false;

				int cvCount = this.cvQualifiers.Length;

				int lastTest = (!type.IsReference) ? this.cvQualifiers.Length - 1 : this.cvQualifiers.Length;
				for(int i = 0; i < lastTest; ++i) {
					if(!ConvertibleTo(this.cvQualifiers[i], type.cvQualifiers[i]))
						return false;
				}

				return true;
			}

			// Is var1 = var2 a valid expression, where var1's type is 'this' and var2's type is 'type' ?
			public bool AssignableFrom(Type type) {
				return type.ConvertibleTo(this);
			}

			public static bool ConvertibleTo(CVQualifier sourceCV, CVQualifier destCV) {
				if((sourceCV | CVQualifier.Const) == sourceCV && (destCV | CVQualifier.Const) != destCV)
					return false;
				if((sourceCV | CVQualifier.Volatile) == sourceCV && (destCV | CVQualifier.Volatile) != destCV)
					return false;

				return true;
			}

			public static string RegexPattern {
				get {
					// Matches const and volatile qualifiers as well as pointer and reference specifiers
					string constVolatilePattern = @"(((const|volatile))*[ ]*[&\* ?]*)*";

					// Matches the cv-qualifiers, the potential virtual, the type name, its template specialization, again cv-qualifiers (may be found before or after the type…)
					string typePattern = @"[ ]*(?<type>(" + constVolatilePattern + @"(virtual[ ]+)?[:a-zA-Z_][:a-zA-Z0-9_]*" + Parser.TemplatePattern + constVolatilePattern + @"))";
					return typePattern;
				}
			}

			string ParseLeftAssociativeQualifiers(string s, List<CVQualifier> qualifiers) {
				s = s.TrimEnd(new char[]{' '});

				if(s.EndsWith(" * const volatile") || s.EndsWith(" * volatile const")) {
					s = ParseLeftAssociativeQualifiers(s.Substring(0, s.Length - " * const volatile".Length), qualifiers);
					qualifiers.Add(CVQualifier.Const | CVQualifier.Volatile);
				}
				else if(s.EndsWith(" * const")) {
					s = ParseLeftAssociativeQualifiers(s.Substring(0, s.Length - " * const".Length), qualifiers);
					qualifiers.Add(CVQualifier.Const);
				}
				else if(s.EndsWith(" * volatile")) {
					s = ParseLeftAssociativeQualifiers(s.Substring(0, s.Length - " * volatile".Length), qualifiers);
					qualifiers.Add(CVQualifier.Volatile);
				}
				else if(s.EndsWith(" *")) {
					s = ParseLeftAssociativeQualifiers(s.Substring(0, s.Length - " *".Length), qualifiers);
					qualifiers.Add(CVQualifier.None);
				}

				return s;
			}

			CVQualifier[] cvQualifiers;
			bool isReference;
			string name;
			//List<Expression> template;
			string template;
		}

		public class Scope : IEquatable<Scope> {
			public static Scope MakeFromNamespace(string ns, Scope enclosing) {
				Scope s = new Scope();
				s.Class = null;
				s.Namespace = ns;
				s.Enclosing = enclosing; 

				return s;
			}

			public static Scope MakeFromClass(Type classType) {
				Scope s = new Scope();
				s.Class = classType;
				s.Namespace = null;
				s.Enclosing = classType.Enclosing; 

				return s;
			}

			public static Scope MakeFromScopes(Scope enclosing, Scope inner) {
				inner.Enclosing = enclosing;
				return inner;
			}

			public static Scope EmptyScope {
				get {
					return MakeFromNamespace("", null);
				}
			}

			public bool IsClass {
				get {
					return Class != null;
				}
			}

			public bool IsNamespace {
				get {
					return Namespace != null;
				}
			}

			public Type Class {
				get;
				private set;
			}

			public string Namespace {
				get;
				private set;
			}

			public override string ToString() {
				string enclosing = "";
				if(Enclosing != null)
					enclosing = Enclosing.ToString();

				if(IsNamespace)
					return enclosing + (Namespace.Length > 0 ? Namespace + "::" : "");
				else
					return enclosing + Class.ToString() + "::";
			}

			public Scope Enclosing {
				get;
				private set;
			}

			public bool Equals(Scope s) {
				return ToString() == s.ToString();
			}
		}
	}
}

