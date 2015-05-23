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

namespace Petri
{
	namespace Cpp {
		public class FunctionInvocation : Expression {
			public FunctionInvocation(Function function, params Expression[] arguments) : base(Cpp.Operator.Name.FunCall) {
				if(arguments.Length != function.Parameters.Count) {
					throw new Exception("Invalid arguments count");
				}

				this.Arguments = new List<Expression>();
				foreach(var arg in arguments) {
					var a = arg;
					if(a.MakeUserReadable() == "")
						a = LiteralExpression.CreateFromString("void", null);

					this.Arguments.Add(a);
				}

				// TODO: Perform type verification here
				this.Function = function;
			}
				
			public List<Expression> Arguments {
				get;
				private set;
			}

			public Cpp.Function Function {
				get;
				private set;
			}

			public override bool UsesFunction(Function f) {
				bool res = false;
				res = res || Function == f;
				foreach(var e in Arguments) {
					res = res || e.UsesFunction(f);
				}

				return res;
			}

			public override string MakeCpp() {
				string args = "";
				for(int i = 0; i < Function.Parameters.Count; ++i) {
					if(i > 0) {
						args += ", ";
					}
					args += "static_cast<" + Function.Parameters[i].Type.ToString() + ">(" + Arguments[i].MakeCpp() + ")";
				}

				string template = "";
				if(Function.Template) {
					template = "<" + Function.TemplateArguments + ">";
				}

				return Function.QualifiedName + template + "(" + args + ")";
			}

			public override string MakeUserReadable() {
				string args = "";
				foreach(var arg in Arguments) {
					if(args.Length > 0)
						args += ", ";
					args += arg.MakeUserReadable();
				}

				return Function.QualifiedName + "(" + args + ")";
			}

			public override List<LiteralExpression> GetLiterals() {
				var l1 = new List<LiteralExpression>();
				foreach(var e in Arguments) {
					var l2 = e.GetLiterals();
					l1.AddRange(l2);
				}

				return l1;
			}
		}

		public class MethodInvocation : FunctionInvocation {
			public MethodInvocation(Method function, Expression that, bool indirection, params Expression[] arguments) : base(function, arguments) {
				this.This = that;
				this.Indirection = indirection;
			}

			public Expression This {
				get;
				private set;
			}

			public bool Indirection {
				get;
				private set;
			}

			public override string MakeUserReadable() {
				string args = "";
				foreach(var arg in Arguments) {
					if(args.Length > 0)
						args += ", ";
					args += arg.MakeUserReadable();
				}

				return This.MakeUserReadable() + (Indirection ? "->" : ".") + Function.QualifiedName + "(" + args + ")";
			}

			public override List<LiteralExpression> GetLiterals() {
				var l1 = base.GetLiterals();
				l1.AddRange(This.GetLiterals());

				return l1;
			}
		}

		public class ConflictFunctionInvocation : FunctionInvocation {
			public ConflictFunctionInvocation(string value) : base(Dummy) {
				_value = value;
			}

			public override string MakeCpp() {
				throw new InvalidOperationException("La fonction est en conflit");
			}

			public override string MakeUserReadable() {
				return _value;
			}

			static Function Dummy {
				get {
					if(_dummy == null) {
						_dummy = new Cpp.Function(new Type("void", Scope.EmptyScope), Scope.EmptyScope, "dummy", false);
					}
					return _dummy;
				}
			}

			string _value;
			static Cpp.Function _dummy;
		}

		public class WrapperFunctionInvocation : FunctionInvocation {
			public WrapperFunctionInvocation(Cpp.Type returnType, Expression expr) : base(GetWrapperFunction(returnType), expr) {
				
			}

			public static Cpp.Function GetWrapperFunction(Cpp.Type returnType) {
				var f = new Function(returnType, Scope.MakeFromNamespace("PetriUtils", null), "", false);
				f.AddParam(new Param(new Type("void", Scope.EmptyScope), "param"));
				return f;
			}

			public override string MakeCpp() {
				return "([&petriNet]() -> " + Function.ReturnType.Name + " { " + Arguments[0].MakeCpp() + "; return {}; })()";
			}

			public override string MakeUserReadable() {
				return Arguments[0].MakeUserReadable();
			}
		}
	}
}

