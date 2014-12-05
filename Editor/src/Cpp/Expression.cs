using System;
using System.Collections.Generic;
using System.Linq;

namespace Petri {
	namespace Cpp
	{
		public abstract class Expression
		{
			private enum ExprType {Parenthesis, Invocation, Subscript, Template, Quote, DoubleQuote};

			protected Expression(Operator.Name op) {
				this.Operator = op;
			}

			public abstract string MakeCpp();
			public abstract string MakeUserReadable();

			public static ExpressionType CreateFromString<ExpressionType>(string s, Entity entity, IEnumerable<Function> funcList) where ExpressionType : Expression {
				var tup = Expression.Preprocess(s);

				Expression result = Expression.CreateFromPreprocessedString(tup.Item1, entity, funcList, tup.Item2);

				if(!(result is ExpressionType))
					throw new Exception("Unable to get a valid expression");

				return (ExpressionType)result;

			}

			public Operator.Name Operator {
				get;
				private set;
			}

			private static Tuple<string, Dictionary<int, Tuple<ExprType, string>>> Preprocess(string s) {
				s.Replace("\t", " ");
				s.Replace("  ", " ");
				s.Replace(" (", "(");
				s = Parser.RemoveParenthesis(s.Trim()).Trim();
				var subexprs = new Dictionary<int, Tuple<ExprType, string>>();

				var nesting = new Stack<Tuple<ExprType, int>>();
				for(int i = 0; i < s.Length; ++i) {
					char cc = s[i];
					switch(s[i]) {
					case '(':
						bool special = false;
						if(i > 0 && (char.IsLetterOrDigit(s[i - 1]) || s[i - 1] == '_')) {
							// It is a function call
							special = true;
						}
						nesting.Push(Tuple.Create(special ? ExprType.Invocation : ExprType.Parenthesis, i));
						break;
					case ')':
						if(nesting.Peek().Item1 == ExprType.Invocation || nesting.Peek().Item1 == ExprType.Parenthesis) {
							subexprs.Add(subexprs.Count, Tuple.Create(nesting.Peek().Item1, s.Substring(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1)));
							int oldSize = s.Length;
							s = s.Remove(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1).Insert(nesting.Peek().Item2, "@" + subexprs.Count.ToString() + "@" + (nesting.Peek().Item1 == ExprType.Invocation ? "()" : ""));
							i += s.Length - oldSize;
							nesting.Pop();
						}
						else
							throw new Exception("Unexpected closing parenthesis found!");
						break;
					case '[':
						nesting.Push(Tuple.Create(ExprType.Subscript, i));
						break;
					case ']':
						if(nesting.Peek().Item1 == ExprType.Subscript) {
							subexprs.Add(subexprs.Count, Tuple.Create(nesting.Peek().Item1, s.Substring(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1)));
							int oldSize = s.Length;
							s = s.Remove(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1).Insert(nesting.Peek().Item2, "@" + subexprs.Count.ToString() + "@");
							i += s.Length - oldSize;
							nesting.Pop();
						}
						else
							throw new Exception("Unexpected closing bracket found!");
						break;
					case '"':
						// First quote
						if(nesting.Count == 0 || (nesting.Peek().Item1 != ExprType.DoubleQuote && nesting.Peek().Item1 != ExprType.Quote)) {
							nesting.Push(Tuple.Create(ExprType.DoubleQuote, i));
						}
						// Second quote
						else if(nesting.Peek().Item1 == ExprType.DoubleQuote && s[i - 1] != '\\') {
							subexprs.Add(subexprs.Count, Tuple.Create(ExprType.DoubleQuote, s.Substring(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1)));
							int oldSize = s.Length;
							s = s.Remove(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1).Insert(nesting.Peek().Item2, "@" + subexprs.Count.ToString() + "@");
							i += s.Length - oldSize;
							nesting.Pop();
						}
						else
							throw new Exception(nesting.Peek().Item1 + " expected, but \" found!");
						break;
					case '\'':
						// First quote
						if(nesting.Count == 0 || (nesting.Peek().Item1 != ExprType.Quote && nesting.Peek().Item1 != ExprType.DoubleQuote)) {
							nesting.Push(Tuple.Create(ExprType.Quote, i));
						}
						// Second quote
						else if(nesting.Peek().Item1 == ExprType.Quote && s[i - 1] != '\\') {
							subexprs.Add(subexprs.Count, Tuple.Create(ExprType.Quote, s.Substring(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1)));
							int oldSize = s.Length;
							s = s.Remove(nesting.Peek().Item2, i - nesting.Peek().Item2 + 1).Insert(nesting.Peek().Item2, "@" + subexprs.Count.ToString() + "@");
							i += s.Length - oldSize;
							nesting.Pop();
						}
						else
							throw new Exception(nesting.Peek().Item1 + " expected, but \' found!");
						break;
					}
				}

				return Tuple.Create(s, subexprs);
			}

			private static Expression CreateFromPreprocessedString(string s, Entity entity, IEnumerable<Function> funcList, Dictionary<int, Tuple<ExprType, string>> subexprs) {
				for(int i = 17; i >= 0; --i) {
					int bound;
					int direction;
					if(Cpp.Operator.Properties[Cpp.Operator.ByPrecedence[i][0]].associativity == Petri.Cpp.Operator.Associativity.LeftToRight) {
						bound = s.Length;
						direction = 1;
					}
					else {
						bound = -1;
						direction = -1;
					}
					int index = bound;
					var foundOperator = Petri.Cpp.Operator.Name.None;
					foreach(var op in Cpp.Operator.ByPrecedence[i]) {
						var prop = Cpp.Operator.Properties[op];
						if(prop.implemented) {
							int opIndex = s.IndexOf(prop.cpp);
							if(opIndex == -1)
								opIndex = bound;
							// If we have found an operator closer to the end of the string (in relation to the operator associativity)
							if(opIndex.CompareTo(index) == direction) {
								// The operator is ambiguous. We assume the ambiguity is that there exist a binary op with the same representation
								// So we have to check if we have found a unary version
								if(prop.ambiguous) {
									if(prop.type == Petri.Cpp.Operator.Type.PrefixUnary) {
										// 2 cases where the operator is actually unary: we can find a binary operator before, or nothing at all.
										if(opIndex == 1)
											continue;
										foreach(var op2 in Enum.GetValues(typeof(Cpp.Operator.Name)).Cast<Cpp.Operator.Name>()) {
											if(Cpp.Operator.Properties[op2].type == Petri.Cpp.Operator.Type.Binary) {
												int opIndex2 = s.Substring(0, opIndex).IndexOf(Cpp.Operator.Properties[op2].cpp);
												if(opIndex2 == -1) {
													continue;
												}
											}
										}
									}
								}
								index = opIndex;
								foundOperator = op;
							}
						}
					}

					if(index != bound) {
						var prop = Cpp.Operator.Properties[foundOperator];

						if(prop.type == Petri.Cpp.Operator.Type.Binary) {
							string e1 = s.Substring(0, index);
							string e2 = s.Substring(index + prop.cpp.Length);

							// Method call
							if(foundOperator == Petri.Cpp.Operator.Name.SelectionRef || foundOperator == Petri.Cpp.Operator.Name.SelectionPtr) {
								int paramIndex = e2.Substring(0, e2.LastIndexOf("@")).LastIndexOf("@");
								var args = Parser.RemoveParenthesis(Expression.GetStringFromPreprocessed(e2.Substring(paramIndex, e2.Length - paramIndex - 2) + "()", subexprs));
								var tup = Expression.Preprocess(args);
								var argsList = tup.Item1.Split(new char[]{ ',' }, StringSplitOptions.None);
								var param = new List<Expression>();
								if(tup.Item1.Length > 0) {
									foreach(var a in argsList) {
										param.Add(Expression.CreateFromPreprocessedString(a, entity, funcList, tup.Item2));
									}
								}

								var func = Expression.GetStringFromPreprocessed(e2.Substring(0, paramIndex), subexprs);
								tup = Expression.Preprocess(func);
								var funcScope = Parser.ExtractScope(Expression.GetStringFromPreprocessed(tup.Item1, subexprs));
								Cpp.Method m = (funcList.FirstOrDefault(delegate(Cpp.Function ff) {
									return (ff is Cpp.Method) && ff.Parameters.Count == param.Count && funcScope.Item2 == ff.Name && funcScope.Item1.Equals(ff.Enclosing);
								})) as Cpp.Method;

								if(m == null) {
									throw new Exception("Aucune méthode ne correspond à l'expression demandée");
								}

								return new MethodInvocation(m, Expression.CreateFromPreprocessedString(e1, entity, funcList, subexprs), foundOperator == Cpp.Operator.Name.SelectionPtr, param.ToArray());
							}

							e1 = Expression.GetStringFromPreprocessed(e1, subexprs);
							e2 = Expression.GetStringFromPreprocessed(e2, subexprs);

							return new BinaryExpression(foundOperator, Expression.CreateFromString<Expression>(e1, entity, funcList), Expression.CreateFromString<Expression>(e2, entity, funcList));
						}
						else if(prop.type == Petri.Cpp.Operator.Type.PrefixUnary) {
							return new UnaryExpression(foundOperator, Expression.CreateFromString<Expression>(Expression.GetStringFromPreprocessed(s.Substring(index + prop.cpp.Length), subexprs), entity, funcList));
						}
						else if(prop.type == Petri.Cpp.Operator.Type.SuffixUnary) {
							if(foundOperator == Petri.Cpp.Operator.Name.FunCall) {
								int paramIndex = s.Substring(0, s.Substring(0, index).LastIndexOf("@")).LastIndexOf("@");
								var args = Parser.RemoveParenthesis(Expression.GetStringFromPreprocessed(s.Substring(paramIndex, index - paramIndex) + "()", subexprs));
								var tup = Expression.Preprocess(args);
								var argsList = tup.Item1.Split(new char[]{ ',' }, StringSplitOptions.None);
								var param = new List<Expression>();
								if(tup.Item1.Length > 0) {
									foreach(var a in argsList) {
										param.Add(Expression.CreateFromPreprocessedString(a, entity, funcList, tup.Item2));
									}
								}

								var func = Expression.GetStringFromPreprocessed(s.Substring(0, paramIndex), subexprs);
								tup = Expression.Preprocess(func);
								var funcScope = Parser.ExtractScope(Expression.GetStringFromPreprocessed(tup.Item1, subexprs));
								Cpp.Function f = funcList.FirstOrDefault(delegate(Cpp.Function ff) {
									return ff.Parameters.Count == param.Count && funcScope.Item2 == ff.Name && funcScope.Item1.Equals(ff.Enclosing);
								});
							
								if(f == null) {
									throw new Exception("Aucune fonction ne correspond à l'expression demandée");
								}
							
								return new FunctionInvocation(f, param.ToArray());
							}
							return new UnaryExpression(foundOperator, Expression.CreateFromString<Expression>(Expression.GetStringFromPreprocessed(s.Substring(0, index), subexprs), entity, funcList));
						}
					}
				}

				return new LitteralExpression(GetStringFromPreprocessed(s, subexprs));
			}

			private static string GetStringFromPreprocessed(string prep, Dictionary<int, Tuple<ExprType, string>> subexprs) {
				int index;
				while(true) {
					index = prep.IndexOf("@");
					if(index == -1)
						break;

					int lastIndex = prep.Substring(index + 1).IndexOf("@") + index + 1;
					int expr = int.Parse(prep.Substring(index + 1, lastIndex - (index + 1))) - 1;
					switch(subexprs[expr].Item1) {
					case ExprType.DoubleQuote:
					case ExprType.Quote:
					case ExprType.Parenthesis:
					case ExprType.Subscript:
						prep = prep.Remove(index, lastIndex - index + 1).Insert(index, subexprs[expr].Item2);
						break;
					case ExprType.Invocation:
						prep = prep.Remove(index, lastIndex - index + 3).Insert(index, subexprs[expr].Item2);
						break;
					}
				} 

				return prep;
			}

			private static MethodInvocation CreateInvocation(bool indirection, List<string> invocation, IEnumerable<Function> funcList, Entity entity) {
				var func = Parser.RemoveParenthesis(invocation[1]);
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

				return new MethodInvocation(m, Expression.CreateFromString<Expression>(invocation[0], entity, funcList), indirection, exprList.ToArray());
			}

			protected static string Parenthesize(Expression parent, Expression child, string representation) {
				bool parenthesize = false;

				if(child.Operator != Cpp.Operator.Name.None) {
					var parentProperties = Cpp.Operator.Properties[parent.Operator];
					var childProperties = Cpp.Operator.Properties[child.Operator];
					if(parentProperties.precedence < childProperties.precedence) {
						parenthesize = true;
					}
					else if(parentProperties.precedence == childProperties.precedence) {
						// No need to manage unary operators
						// We assume the ternary conditional operator does not need to be parenthesized either
						if(parentProperties.type == Petri.Cpp.Operator.Type.Binary) {
							var castedParent = parent as BinaryExpression;
							// We can assume the associativity is the same for both operators as they have the same precedence

							// If the operator is left-associative, but the expression was parenthesize from right to left
							// eg. a + (b + c) (do not forget that IEEE floating point values are not communtative with regards to addition, among others).
							// So we need to preserve the associativity the user gave at first.
							if(parentProperties.associativity == Petri.Cpp.Operator.Associativity.LeftToRight && castedParent.Expression2 == child) {
								parenthesize = true;
							}
							// If the operator is right-associative, but the expression was parenthesize from left to right
							else if(parentProperties.associativity == Petri.Cpp.Operator.Associativity.RightToLeft && castedParent.Expression1 == child) {
								parenthesize = true;
							}
						}
					}
				}

				if(parenthesize)
					return "(" + representation + ")";
				else
					return representation;
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
		
		public class UnaryExpression : Expression {
			public UnaryExpression(Operator.Name o, Expression expr) : base(o) {
				this.Expression = expr;
			}

			public Expression Expression {
				get;
				private set;
			}

			public override string MakeCpp() {
				switch(this.Operator) {
				case Cpp.Operator.Name.FunCall:
					throw new Exception("Already managed in FunctionInvocation class!");
				case Cpp.Operator.Name.UnaryPlus:
					return this.Expression.MakeCpp();
				case Cpp.Operator.Name.UnaryMinus:
					return "make_callable_ptr(std::negate<void>(), " + this.Expression.MakeCpp() + ")";
				case Cpp.Operator.Name.LogicalNot:
					return "make_callable_ptr(std::logical_not<void>(), " + this.Expression.MakeCpp() + ")";
				case Cpp.Operator.Name.BitwiseNot:
					return "make_callable_ptr(std::bit_not<void>(), " + this.Expression.MakeCpp() + ")";
				case Cpp.Operator.Name.Indirection:
					return "make_callable_ptr(PetriUtils::indirect<void>(), " + this.Expression.MakeCpp() + ")";
				case Cpp.Operator.Name.AddressOf:
					return "make_callable_ptr(PetriUtils::addressof<void>(), " + this.Expression.MakeCpp() + ")";
				case Cpp.Operator.Name.PreIncr:
					return "make_callable_ptr(PetriUtils::preincr<void>(), " + this.Expression.MakeCpp() + ")";
				case Cpp.Operator.Name.PreDecr:
					return "make_callable_ptr(PetriUtils::predecr<void>(), " + this.Expression.MakeCpp() + ")";
				case Cpp.Operator.Name.PostIncr:
					return "make_callable_ptr(PetriUtils::postincr<void>(), " + this.Expression.MakeCpp() + ")";
				case Cpp.Operator.Name.PostDecr:
					return "make_callable_ptr(PetriUtils::postdecr<void>(), " + this.Expression.MakeCpp() + ")";
				}
				throw new Exception("Operator not implemented!");
			}

			public override string MakeUserReadable() {
				string parenthesized = Expression.Parenthesize(this, this.Expression, this.Expression.MakeUserReadable());
				switch(this.Operator) {
				case Cpp.Operator.Name.FunCall:
					throw new Exception("Already managed in FunctionInvocation class!");
				case Cpp.Operator.Name.UnaryPlus:
					return "+" + parenthesized;
				case Cpp.Operator.Name.UnaryMinus:
					return "-" + parenthesized;
				case Cpp.Operator.Name.LogicalNot:
					return "!" + parenthesized;
				case Cpp.Operator.Name.BitwiseNot:
					return "~" + parenthesized;
				case Cpp.Operator.Name.Indirection:
					return "*" + parenthesized;
				case Cpp.Operator.Name.AddressOf:
					return "&" + parenthesized;
				case Cpp.Operator.Name.PreIncr:
					return "++" + parenthesized;
				case Cpp.Operator.Name.PreDecr:
					return "--" + parenthesized;
				case Cpp.Operator.Name.PostIncr:
					return parenthesized + "++";
				case Cpp.Operator.Name.PostDecr:
					return parenthesized + "--";
				}

				throw new Exception("Operator not implemented!");
			}
		}

		public class BinaryExpression : Expression {
			public BinaryExpression(Operator.Name o, Expression expr1, Expression expr2) : base(o) {
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

			public override string MakeCpp() {
				string e1 = Expression1.MakeCpp();
				string e2 = Expression2.MakeCpp();

				switch(this.Operator) {
				case Cpp.Operator.Name.Mult:
					return "make_callable_ptr(std::multiplies<void>(), " + e1 + ", " + e2 + ")";
				case Cpp.Operator.Name.Div:
					return "make_callable_ptr(std::divides<void>(), " + e1 + ", " + e2 + ")";
				case Cpp.Operator.Name.Mod:
					return "make_callable_ptr(std::modulus<void>(), " + e1 + ", " + e2 + ")";
				case Cpp.Operator.Name.Plus:
					return "make_callable_ptr(std::plus<void>(), " + e1 + ", " + e2 + ")";
				case Cpp.Operator.Name.Minus:
					return "make_callable_ptr(std::minus<void>(), " + e1 + ", " + e2 + ")";
				case Cpp.Operator.Name.ShiftLeft:
					return "make_callable_ptr(PetriUtils::shift_left<void>(), " + e1 + ", " + e2 + ")";
				case Cpp.Operator.Name.ShiftRight:
					return "make_callable_ptr(PetriUtils::shift_right<void>(), " + e1 + ", " + e2 + ")";
				case Cpp.Operator.Name.Less:
					return "make_callable_ptr(std::less<void>(), " + e1 + ", " + e2 + ")";
				case Cpp.Operator.Name.LessEqual:
					return "make_callable_ptr(std::less_equal<void>(), " + e1 + ", " + e2 + ")";
				case Cpp.Operator.Name.Greater:
					return "make_callable_ptr(std::greater<void>(), " + e1 + ", " + e2 + ")";
				case Cpp.Operator.Name.GreaterEqual:
					return "make_callable_ptr(std::greater_equal<void>(), " + e1 + ", " + e2 + ")";
				case Cpp.Operator.Name.Equal:
					return "make_callable_ptr(std::equal_to<void>(), " + e1 + ", " + e2 + ")";
				case Cpp.Operator.Name.NotEqual:
					return "make_callable_ptr(std::not_equal_to<void>(), " + e1 + ", " + e2 + ")";
				case Cpp.Operator.Name.BitwiseAnd:
					return "make_callable_ptr(std::bit_and<void>(), " + e1 + ", " + e2 + ")";
				case Cpp.Operator.Name.BitwiseXor:
					return "make_callable_ptr(std::bit_xor<void>(), " + e1 + ", " + e2 + ")";
				case Cpp.Operator.Name.BitwiseOr:
					return "make_callable_ptr(std::bit_or<void>(), " + e1 + ", " + e2 + ")";
				case Cpp.Operator.Name.LogicalAnd:
					return "make_callable_ptr(std::logical_and<void>(), " + e1 + ", " + e2 + ")";
				case Cpp.Operator.Name.LogicalOr:
					return "make_callable_ptr(std::logical_or<void>(), " + e1 + ", " + e2 + ")";
				}

				throw new Exception("Operator not implemented!");
			}

			public override string MakeUserReadable() {
				string p1 = Expression.Parenthesize(this, this.Expression1, this.Expression1.MakeUserReadable());
				string p2 = Expression.Parenthesize(this, this.Expression2, this.Expression2.MakeUserReadable());
				switch(this.Operator) {
				case Cpp.Operator.Name.Mult:
					return p1 + " * " + p2;
				case Cpp.Operator.Name.Div:
					return p1 + " / " + p2;
				case Cpp.Operator.Name.Mod:
					return p1 + " % " + p2;
				case Cpp.Operator.Name.Plus:
					return p1 + " + " + p2;
				case Cpp.Operator.Name.Minus:
					return p1 + " - " + p2;
				case Cpp.Operator.Name.ShiftLeft:
					return p1 + " << " + p2;
				case Cpp.Operator.Name.ShiftRight:
					return p1 + " >> " + p2;
				case Cpp.Operator.Name.Less:
					return p1 + " < " + p2;
				case Cpp.Operator.Name.LessEqual:
					return p1 + " <= " + p2;
				case Cpp.Operator.Name.Greater:
					return p1 + " > " + p2;
				case Cpp.Operator.Name.GreaterEqual:
					return p1 + " >= " + p2;
				case Cpp.Operator.Name.Equal:
					return p1 + " == " + p2;
				case Cpp.Operator.Name.NotEqual:
					return p1 + " != " + p2;
				case Cpp.Operator.Name.BitwiseAnd:
					return p1 + " & " + p2;
				case Cpp.Operator.Name.BitwiseXor:
					return p1 + " ^ " + p2;
				case Cpp.Operator.Name.BitwiseOr:
					return p1 + " | " + p2;
				case Cpp.Operator.Name.LogicalAnd:
					return p1 + " && " + p2;
				case Cpp.Operator.Name.LogicalOr:
					return p1 + " || " + p2;
				case Cpp.Operator.Name.Assignment:
					return p1 + " = " + p2;
				case Cpp.Operator.Name.PlusAssign:
					return p1 + " += " + p2;
				case Cpp.Operator.Name.MinusAssign:
					return p1 + " -= " + p2;
				case Cpp.Operator.Name.MultAssign:
					return p1 + " *= " + p2;
				case Cpp.Operator.Name.DivAssign:
					return p1 + " /= " + p2;
				case Cpp.Operator.Name.ModAssign:
					return p1 + " %= " + p2;
				case Cpp.Operator.Name.ShiftLeftAssign:
					return p1 + " <<= " + p2;
				case Cpp.Operator.Name.ShiftRightAssign:
					return p1 + " >>= " + p2;
				case Cpp.Operator.Name.BitwiseAndAssig:
					return p1 + " &= " + p2;
				case Cpp.Operator.Name.BitwiseXorAssign:
					return p1 + " ^= " + p2;
				case Cpp.Operator.Name.BitwiseOrAssign:
					return p1 + " |= " + p2;
				case Cpp.Operator.Name.Comma:
					return p1 + ", " + p2;
				}

				throw new Exception("Operator not implemented!");
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

			public override string MakeCpp() {
				throw new Exception("Operator not implemented!");
			}

			public override string MakeUserReadable() {
				throw new Exception("Operator not implemented!");
			}
		}
	}
}

