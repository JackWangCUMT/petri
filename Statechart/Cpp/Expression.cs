using System;
using System.Collections.Generic;
using System.Linq;

namespace Statechart {
	namespace Cpp
	{
		//enum ExpressionHint {
		//	TypeOrLitteral
		public abstract class Expression
		{
			protected Expression(Operator.Name op) {
				this.Operator = op;
			}

			public abstract string MakeCpp();
			public abstract string MakeUserReadable();
			// a(…)
			// a.b(…)
			// a(…).b(…)
			// (a(…)).b(…)
			// ((a(…).b(…)


			public static ExpressionType CreateFromString<ExpressionType>(string s, Entity entity, IEnumerable<Function> funcList) where ExpressionType : Expression {
				Expression result = null;

				s = Parser.RemoveParenthesis(s);

				//foreach(Operator.Name op in 

				var invokation = Parser.SyntacticSplit(s, ".");
				if(invokation.Count == 2) {
					result = Expression.CreateInvokation(false, invokation, funcList, entity);
				}
				else {
					invokation = Parser.SyntacticSplit(s, "->");
					if(invokation.Count == 2) {
						result = Expression.CreateInvokation(true, invokation, funcList, entity);
					}
					else {
						int index = s.IndexOf("(");
						if(index != -1) {
							var args = Parser.RemoveParenthesis(s.Substring(index));
							if(args == s.Substring(index)) {
								throw new Exception("Syntax error!");
							}
							var func = s.Substring(0, index);
							var tup = Parser.ExtractScope(func);
							var argsList = Parser.SyntacticSplit(args, ",");
							var exprList = new List<Expression>();
							foreach(var ss in argsList) {
								exprList.Add(Expression.CreateFromString<Expression>(Parser.RemoveParenthesis(ss), entity, funcList));
							}
							Cpp.Function ff = funcList.FirstOrDefault(delegate(Cpp.Function f) {
								return !(f is Method) && f.Parameters.Count == argsList.Count && tup.Item2 == f.Name && tup.Item1.Equals(f.Enclosing);
							});

							if(ff == null) {
								throw new Exception("Aucune fonction ne correspond à l'expression demandée");
							}

							result = new FunctionInvokation(ff, exprList.ToArray());
						}
						else {
							result = new LitteralExpression(s);
						}
					}
				}

				if(!(result is ExpressionType))
					throw new Exception("Unable to get valid expression");

				return (ExpressionType)result;
			}

			public Operator.Name Operator {
				get;
				private set;
			}

			private static MethodInvokation CreateInvokation(bool indirection, List<string> invokation, IEnumerable<Function> funcList, Entity entity) {
				var func = Parser.RemoveParenthesis(invokation[1]);
				int index = func.IndexOf("(");
				var args = Parser.RemoveParenthesis(func.Substring(index));
				func = func.Substring(0, index);
				var tup = Parser.ExtractScope(func);
				var argsList = Parser.SyntacticSplit(args, ",");
				var exprList = new List<Expression>();
				foreach(var ss in argsList) {
					exprList.Add(Expression.CreateFromString<Expression>(Parser.RemoveParenthesis(ss), entity, funcList));
				}
				Cpp.Method m = (funcList.FirstOrDefault(delegate(Cpp.Function ff) {
					return (ff is Method) && ff.Parameters.Count == argsList.Count && tup.Item2 == ff.Name && tup.Item1.Equals(ff.Enclosing);
				})) as Method;

				if(m == null) {
					throw new Exception("Aucune méthode ne correspond à l'expression demandée");
				}

				return new MethodInvokation(m, Expression.CreateFromString<Expression>(invokation[0], entity, funcList), indirection, exprList.ToArray());
			}
		}

		public class EmptyExpression : Expression {
			public EmptyExpression() : base(Cpp.Operator.Name.None) {}

			public override string MakeCpp() {
				throw new Exception("Expression vide !");
			}

			public override string MakeUserReadable() {
				return "";
			}
		}

		public class LitteralExpression : Expression {
			public LitteralExpression(string expr) : base(Cpp.Operator.Name.None) {
				Expression = expr;
			}

			public string Expression {
				get;
				private set;
			}

			public override string MakeCpp() {
				return Expression;
			}

			public override string MakeUserReadable() {
				return Expression;
			}
		}

		public class EntityExpression : Expression {
			public EntityExpression(Entity e, string readableName) : base(Cpp.Operator.Name.None) {
				this.Entity = e;
				this.ReadableName = readableName;
			}

			public Entity Entity {
				get;
				private set;
			}

			public string ReadableName {
				get;
				private set;
			}

			public override string MakeCpp() {
				return this.Entity.CppName + ".get()";
			}

			public override string MakeUserReadable() {
				return this.ReadableName;
			}
		}

		/*public class TypeExpression : Expression {
			public TypeExpression(Type y) {
				Type = y;
			}

			public Type Type {
				get;
				private set;
			}

			public override string MakeCpp() {
				return Type.ToString();
			}

			public override string MakeUserReadable() {
				return Type.ToString();
			}
		}*/

		public abstract class UnaryExpression : Expression {
			protected UnaryExpression(Expression expr, Operator.Name o) : base(o) {
				this.Expression = expr;
			}

			public Expression Expression {
				get;
				private set;
			}
		}

		public abstract class BinaryExpression : Expression {
			protected BinaryExpression(Expression expr1, Expression expr2, Operator.Name o) : base(o) {
				this.Expression1 = expr1;
				this.Expression2 = expr2;
			}

			public Expression Expression1 {
				get;
				private set;
			}

			public Expression Expression2 {
				get;
				private set;
			}
		}

		// Could have been TernaryExpression, but there is only one ternary operator in C++, so we already specialize it.
		public abstract class TernaryConditionExpression : Expression {
			protected TernaryConditionExpression(Expression expr1, Expression expr2, Expression expr3) : base(Cpp.Operator.Name.TernaryConditional) {
				this.Expression1 = expr1;
				this.Expression2 = expr2;
				this.Expression3 = expr3;
			}

			public Expression Expression1 {
				get;
				private set;
			}

			public Expression Expression2 {
				get;
				private set;
			}

			public Expression Expression3 {
				get;
				private set;
			}
		}
	}
}

