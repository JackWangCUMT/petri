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
						a = LitteralExpression.CreateFromString("void", null);

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
				foreach(var arg in Arguments) {
					args += ", ";
					args += arg.MakeCpp();
				}

				string template = "";
				if(Function.Template) {
					template = "<" + Function.TemplateArguments + ">";
				}

				return "make_callable_ptr(&" + Function.QualifiedName + template + args + ")";
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

			public override List<LitteralExpression> GetLiterals() {
				var l1 = new List<LitteralExpression>();
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

			public override List<LitteralExpression> GetLiterals() {
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
				return "make_callable_ptr([&petriNet]() -> " + Function.ReturnType.Name + " { (*" + Arguments[0].MakeCpp() + ")(); return {}; })";
			}

			public override string MakeUserReadable() {
				return Arguments[0].MakeUserReadable();
			}
		}
	}
}

