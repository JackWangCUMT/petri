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
					this.Arguments.Add(arg);
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

			public override string MakeCpp() {
				string args = "";
				foreach(var arg in Arguments) {
					args += ", ";
					args += arg.MakeCpp();
				}

				return "make_callable_ptr(" + Function.QualifiedName + args + ")";
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

			public override string MakeCpp() {
				string args = "";
				foreach(var arg in Arguments) {
					args += ", ";
					args += arg.MakeCpp();
				}
				return "make_callable_ptr(&" + Function.QualifiedName + ", " + This.MakeCpp() + args + ")";
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
		}
	}
}

