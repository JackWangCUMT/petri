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
using System.Linq;

namespace Petri
{
	namespace Cpp {
		public static class Operator {
			public enum Name {
				ScopeResolution, // ::
				PostIncr, // ++
				PostDecr, // --
				FunctionalCast, //type(expr)
				FunCall, // ()
				Subscript, // []
				SelectionRef, // .
				SelectionPtr, // ->

				PreIncr, // ++
				PreDecr, // --
				UnaryPlus, // +
				UnaryMinus, // -
				LogicalNot, // !
				BitwiseNot, // ~
				CCast, // (type)expr
				Indirection, // *
				AddressOf, // &
				Sizeof, // sizeof
				New, // new
				NewTab, // new[]
				Delete, // delete
				DeleteTab, // delete[]

				PtrToMemRef, // .*
				PtrToMemPtr, // .->
				Mult,
				Div,
				Mod,
				Plus,
				Minus,
				ShiftLeft, // <<
				ShiftRight, // >>
				LessEqual,
				Less,
				GreaterEqual,
				Greater,
				Equal,
				NotEqual,
				BitwiseAnd, // &
				BitwiseXor, // ^
				BitwiseOr, // |
				LogicalAnd, // &&
				LogicalOr, // ||

				TernaryConditional, // ?:
				Assignment, // =
				PlusAssign, // +=
				MinusAssign, // -=
				MultAssign, // *=
				DivAssign, // /=
				ModAssign, // %=
				ShiftLeftAssign, // <<=
				ShiftRightAssign, // >>=
				BitwiseAndAssig, // &=
				BitwiseXorAssign, // ^=
				BitwiseOrAssign, // |=
				Throw, // throw
				Comma, // ,
				None
			}

			public enum Associativity {LeftToRight, RightToLeft};

			public enum Type {
				Binary,
				PrefixUnary,
				SuffixUnary,
				Ternary
			}

			public struct Op {
				public Op(Associativity a, Type t, string c, int p, bool impl, Name[] ambig) {
					associativity = a;
					type = t;
					precedence = p;
					implemented = impl;
					cpp = c;
					ambiguities = ambig;
				}

				public Name[] ambiguities;
				public string cpp;
				public bool implemented;
				public Associativity associativity;
				public Type type;
				public int precedence;
			}

			public static Dictionary<Name, Op> Properties {
				get;
				private set;
			}

			public static Dictionary<int, List<Name>> ByPrecedence {
				get;
				private set;
			}

			static Operator() {
				Properties = new Dictionary<Name, Op>();

				Properties[Name.ScopeResolution] = new Op(Associativity.LeftToRight, Type.Binary, "::", 0, false, new Name[]{});

				Properties[Name.PostIncr] = new Op(Associativity.LeftToRight, Type.SuffixUnary, "++", 1, true, new Name[]{});
				Properties[Name.PostDecr] = new Op(Associativity.LeftToRight, Type.SuffixUnary, "--", 1, true, new Name[]{});
				Properties[Name.FunctionalCast] = new Op(Associativity.LeftToRight, Type.PrefixUnary, "", 1, false, new Name[]{});
				Properties[Name.FunCall] = new Op(Associativity.LeftToRight, Type.SuffixUnary, "()", 1, true, new Name[]{});
				Properties[Name.Subscript] = new Op(Associativity.LeftToRight, Type.SuffixUnary, "", 1, false, new Name[]{});

				// Actual precedence is the same as function call for . and ->, but we cheat for f().X being parsed
				Properties[Name.SelectionRef] = new Op(Associativity.LeftToRight, Type.Binary, ".", 2, true, new Name[]{});
				Properties[Name.SelectionPtr] = new Op(Associativity.LeftToRight, Type.Binary, "->", 2, true, new Name[]{});

				Properties[Name.PreIncr] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "++", 3, true, new Name[]{Name.PostIncr});
				Properties[Name.PreDecr] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "--", 3, true, new Name[]{Name.PostDecr});
				Properties[Name.UnaryPlus] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "+", 3, true, new Name[]{Name.PreIncr, Name.PostIncr});
				Properties[Name.UnaryMinus] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "-", 3, true, new Name[]{Name.PreDecr, Name.PostDecr});
				Properties[Name.LogicalNot] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "!", 3, true, new Name[]{});
				Properties[Name.BitwiseNot] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "~", 3, true, new Name[]{});
				Properties[Name.CCast] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false, new Name[]{});
				Properties[Name.Indirection] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "*", 3, false, new Name[]{});
				Properties[Name.AddressOf] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "&", 3, false, new Name[]{});
				Properties[Name.Sizeof] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false, new Name[]{});
				Properties[Name.New] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false, new Name[]{});
				Properties[Name.NewTab] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false, new Name[]{});
				Properties[Name.Delete] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false, new Name[]{});
				Properties[Name.DeleteTab] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false, new Name[]{});

				Properties[Name.PtrToMemRef] = new Op(Associativity.LeftToRight, Type.Binary, ".*", 4, false, new Name[]{});
				Properties[Name.PtrToMemPtr] = new Op(Associativity.LeftToRight, Type.Binary, "->*", 4, false, new Name[]{});

				Properties[Name.Mult] = new Op(Associativity.LeftToRight, Type.Binary, "*", 5, true, new Name[]{});
				Properties[Name.Div] = new Op(Associativity.LeftToRight, Type.Binary, "/", 5, true, new Name[]{});
				Properties[Name.Mod] = new Op(Associativity.LeftToRight, Type.Binary, "%", 5, true, new Name[]{});

				Properties[Name.Plus] = new Op(Associativity.LeftToRight, Type.Binary, "+", 6, true, new Name[]{Name.UnaryPlus, Name.PreIncr, Name.PostIncr});
				Properties[Name.Minus] = new Op(Associativity.LeftToRight, Type.Binary, "-", 6, true, new Name[]{Name.UnaryMinus, Name.PreDecr, Name.PostDecr});

				Properties[Name.ShiftLeft] = new Op(Associativity.LeftToRight, Type.Binary, "<<", 7, true, new Name[]{});
				Properties[Name.ShiftRight] = new Op(Associativity.LeftToRight, Type.Binary, ">>", 7, true, new Name[]{});

				Properties[Name.LessEqual] = new Op(Associativity.LeftToRight, Type.Binary, "<=", 8, true, new Name[]{});
				Properties[Name.Less] = new Op(Associativity.LeftToRight, Type.Binary, "<", 8, true, new Name[]{});
				Properties[Name.GreaterEqual] = new Op(Associativity.LeftToRight, Type.Binary, ">=", 8, true, new Name[]{});
				Properties[Name.Greater] = new Op(Associativity.LeftToRight, Type.Binary, ">", 8, true, new Name[]{});

				Properties[Name.Equal] = new Op(Associativity.LeftToRight, Type.Binary, "==", 9, true, new Name[]{});
				Properties[Name.NotEqual] = new Op(Associativity.LeftToRight, Type.Binary, "!=", 9, true, new Name[]{});

				Properties[Name.BitwiseAnd] = new Op(Associativity.LeftToRight, Type.Binary, "&", 10, false, new Name[]{});
				Properties[Name.BitwiseXor] = new Op(Associativity.LeftToRight, Type.Binary, "^", 11, false, new Name[]{});
				Properties[Name.BitwiseOr] = new Op(Associativity.LeftToRight, Type.Binary, "|", 12, false, new Name[]{});
				Properties[Name.LogicalAnd] = new Op(Associativity.LeftToRight, Type.Binary, "&&", 13, true, new Name[]{});
				Properties[Name.LogicalOr] = new Op(Associativity.LeftToRight, Type.Binary, "||", 14, true, new Name[]{});

				Properties[Name.TernaryConditional] = new Op(Associativity.RightToLeft, Type.Ternary, "?", 15, false, new Name[]{});
				Properties[Name.Assignment] = new Op(Associativity.RightToLeft, Type.Binary, ":=", 15, true, new Name[]{});
				Properties[Name.PlusAssign] = new Op(Associativity.RightToLeft, Type.Binary, "+=", 15, false, new Name[]{});
				Properties[Name.MinusAssign] = new Op(Associativity.RightToLeft, Type.Binary, "-=", 15, false, new Name[]{});
				Properties[Name.MultAssign] = new Op(Associativity.RightToLeft, Type.Binary, "*=", 15, false, new Name[]{});
				Properties[Name.DivAssign] = new Op(Associativity.RightToLeft, Type.Binary, "/=", 15, false, new Name[]{});
				Properties[Name.ModAssign] = new Op(Associativity.RightToLeft, Type.Binary, "%=", 15, false, new Name[]{});
				Properties[Name.ShiftLeftAssign] = new Op(Associativity.RightToLeft, Type.Binary, "<<=", 15, false, new Name[]{});
				Properties[Name.ShiftRightAssign] = new Op(Associativity.RightToLeft, Type.Binary, ">>=", 15, false, new Name[]{});
				Properties[Name.BitwiseAndAssig] = new Op(Associativity.RightToLeft, Type.Binary, "&=", 15, false, new Name[]{});
				Properties[Name.BitwiseXorAssign] = new Op(Associativity.RightToLeft, Type.Binary, "^=", 15, false, new Name[]{});
				Properties[Name.BitwiseOrAssign] = new Op(Associativity.RightToLeft, Type.Binary, "|=", 15, false, new Name[]{});

				Properties[Name.Throw] = new Op(Associativity.LeftToRight, Type.PrefixUnary, "", 16, false, new Name[]{});

				Properties[Name.Comma] = new Op(Associativity.LeftToRight, Type.Binary, ",", 17, true, new Name[]{});

				ByPrecedence = new Dictionary<int, List<Name>>();
				for(int i = 0; i <= 17; ++i) {
					ByPrecedence[i] = new List<Name>();
					ByPrecedence[i].AddRange(from prop in Properties
						where prop.Value.precedence == i
						select prop.Key);
				}
			}
		}
	}
}

