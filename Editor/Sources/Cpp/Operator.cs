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

namespace Petri.Editor
{
    namespace Cpp
    {
        public static class Operator
        {
            public enum Name
            {
                ScopeResolution,
                // ::
                PostIncr,
                // ++
                PostDecr,
                // --
                FunctionalCast,
                //type(expr)
                FunCall,
                // ()
                Subscript,
                // []
                SelectionRef,
                // .
                SelectionPtr,
                // ->

                PreIncr,
                // ++
                PreDecr,
                // --
                UnaryPlus,
                // +
                UnaryMinus,
                // -
                LogicalNot,
                // !
                BitwiseNot,
                // ~
                CCast,
                // (type)expr
                Indirection,
                // *
                AddressOf,
                // &
                Sizeof,
                // sizeof
                New,
                // new
                NewTab,
                // new[]
                Delete,
                // delete
                DeleteTab,
                // delete[]

                PtrToMemRef,
                // .*
                PtrToMemPtr,
                // .->
                Mult,
                Div,
                Mod,
                Plus,
                Minus,
                ShiftLeft,
                // <<
                ShiftRight,
                // >>
                LessEqual,
                Less,
                GreaterEqual,
                Greater,
                Equal,
                NotEqual,
                BitwiseAnd,
                // &
                BitwiseXor,
                // ^
                BitwiseOr,
                // |
                LogicalAnd,
                // &&
                LogicalOr,
                // ||

                TernaryConditional,
                // ?:
                Assignment,
                // =
                PlusAssign,
                // +=
                MinusAssign,
                // -=
                MultAssign,
                // *=
                DivAssign,
                // /=
                ModAssign,
                // %=
                ShiftLeftAssign,
                // <<=
                ShiftRightAssign,
                // >>=
                BitwiseAndAssig,
                // &=
                BitwiseXorAssign,
                // ^=
                BitwiseOrAssign,
                // |=
                Throw,
                // throw
                Comma,
                // ,
                None
            }

            public enum Associativity
            {
LeftToRight,
                RightToLeft}

            ;

            public enum Type
            {
                Binary,
                PrefixUnary,
                SuffixUnary,
                Ternary
            }

            public struct Op
            {
                public Op(Associativity a, Type t, string c, int p, bool impl)
                {
                    associativity = a;
                    type = t;
                    precedence = p;
                    implemented = impl;
                    cpp = c;
                    id = _id++;
                    if(!_lexed.ContainsKey(cpp)) {
                        _lexed[cpp] = id;
                    }
                    lexed = "#" + _lexed[cpp].ToString() + "#";
                }

                public string cpp;
                public bool implemented;
                public Associativity associativity;
                public Type type;
                public int precedence;
                public int id;
                public string lexed;

                private static int _id = 0;
                private static Dictionary<string, int> _lexed = new Dictionary<string, int>();
            }

            public static Dictionary<Name, Op> Properties {
                get;
                private set;
            }

            public static Dictionary<int, List<Name>> ByPrecedence {
                get;
                private set;
            }

            public static Dictionary<int, Name> ByID {
                get;
                private set;
            }

            public static List<Name> Lex {
                get;
                private set;
            }

            static Operator()
            {
                Properties = new Dictionary<Name, Op>();

                Properties[Name.ScopeResolution] = new Op(Associativity.LeftToRight, Type.Binary, "::", 0, false);

                Properties[Name.PostIncr] = new Op(Associativity.LeftToRight, Type.SuffixUnary, "++", 1, true);
                Properties[Name.PostDecr] = new Op(Associativity.LeftToRight, Type.SuffixUnary, "--", 1, true);
                Properties[Name.FunctionalCast] = new Op(Associativity.LeftToRight, Type.PrefixUnary, "", 1, false);
                Properties[Name.FunCall] = new Op(Associativity.LeftToRight, Type.SuffixUnary, "()", 1, true);
                Properties[Name.Subscript] = new Op(Associativity.LeftToRight, Type.SuffixUnary, "", 1, false);

                // Actual precedence is the same as function call for . and ->, but we cheat for f().X being parsed
                Properties[Name.SelectionRef] = new Op(Associativity.LeftToRight, Type.Binary, ".", 2, true);
                Properties[Name.SelectionPtr] = new Op(Associativity.LeftToRight, Type.Binary, "->", 2, true);

                Properties[Name.PreIncr] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "++", 3, true);
                Properties[Name.PreDecr] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "--", 3, true);
                Properties[Name.UnaryPlus] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "+", 3, true);
                Properties[Name.UnaryMinus] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "-", 3, true);
                Properties[Name.LogicalNot] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "!", 3, true);
                Properties[Name.BitwiseNot] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "~", 3, true);
                Properties[Name.CCast] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false);
                Properties[Name.Indirection] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "*", 3, false);
                Properties[Name.AddressOf] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "&", 3, true);
                Properties[Name.Sizeof] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false);
                Properties[Name.New] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false);
                Properties[Name.NewTab] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false);
                Properties[Name.Delete] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false);
                Properties[Name.DeleteTab] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false);

                Properties[Name.PtrToMemRef] = new Op(Associativity.LeftToRight, Type.Binary, ".*", 4, false);
                Properties[Name.PtrToMemPtr] = new Op(Associativity.LeftToRight, Type.Binary, "->*", 4, false);

                Properties[Name.Mult] = new Op(Associativity.LeftToRight, Type.Binary, "*", 5, true);
                Properties[Name.Div] = new Op(Associativity.LeftToRight, Type.Binary, "/", 5, true);
                Properties[Name.Mod] = new Op(Associativity.LeftToRight, Type.Binary, "%", 5, true);

                Properties[Name.Plus] = new Op(Associativity.LeftToRight, Type.Binary, "+", 6, true);
                Properties[Name.Minus] = new Op(Associativity.LeftToRight, Type.Binary, "-", 6, true);

                Properties[Name.ShiftLeft] = new Op(Associativity.LeftToRight, Type.Binary, "<<", 7, true);
                Properties[Name.ShiftRight] = new Op(Associativity.LeftToRight, Type.Binary, ">>", 7, true);

                Properties[Name.LessEqual] = new Op(Associativity.LeftToRight, Type.Binary, "<=", 8, true);
                Properties[Name.Less] = new Op(Associativity.LeftToRight, Type.Binary, "<", 8, true);
                Properties[Name.GreaterEqual] = new Op(Associativity.LeftToRight, Type.Binary, ">=", 8, true);
                Properties[Name.Greater] = new Op(Associativity.LeftToRight, Type.Binary, ">", 8, true);

                Properties[Name.Equal] = new Op(Associativity.LeftToRight, Type.Binary, "==", 9, true);
                Properties[Name.NotEqual] = new Op(Associativity.LeftToRight, Type.Binary, "!=", 9, true);

                Properties[Name.BitwiseAnd] = new Op(Associativity.LeftToRight, Type.Binary, "&", 10, false);
                Properties[Name.BitwiseXor] = new Op(Associativity.LeftToRight, Type.Binary, "^", 11, false);
                Properties[Name.BitwiseOr] = new Op(Associativity.LeftToRight, Type.Binary, "|", 12, false);
                Properties[Name.LogicalAnd] = new Op(Associativity.LeftToRight, Type.Binary, "&&", 13, true);
                Properties[Name.LogicalOr] = new Op(Associativity.LeftToRight, Type.Binary, "||", 14, true);

                Properties[Name.TernaryConditional] = new Op(Associativity.RightToLeft, Type.Ternary, "?", 15, false);
                Properties[Name.Assignment] = new Op(Associativity.RightToLeft, Type.Binary, "=", 15, true);
                Properties[Name.PlusAssign] = new Op(Associativity.RightToLeft, Type.Binary, "+=", 15, false);
                Properties[Name.MinusAssign] = new Op(Associativity.RightToLeft, Type.Binary, "-=", 15, false);
                Properties[Name.MultAssign] = new Op(Associativity.RightToLeft, Type.Binary, "*=", 15, false);
                Properties[Name.DivAssign] = new Op(Associativity.RightToLeft, Type.Binary, "/=", 15, false);
                Properties[Name.ModAssign] = new Op(Associativity.RightToLeft, Type.Binary, "%=", 15, false);
                Properties[Name.ShiftLeftAssign] = new Op(Associativity.RightToLeft, Type.Binary, "<<=", 15, false);
                Properties[Name.ShiftRightAssign] = new Op(Associativity.RightToLeft, Type.Binary, ">>=", 15, false);
                Properties[Name.BitwiseAndAssig] = new Op(Associativity.RightToLeft, Type.Binary, "&=", 15, false);
                Properties[Name.BitwiseXorAssign] = new Op(Associativity.RightToLeft, Type.Binary, "^=", 15, false);
                Properties[Name.BitwiseOrAssign] = new Op(Associativity.RightToLeft, Type.Binary, "|=", 15, false);

                Properties[Name.Throw] = new Op(Associativity.LeftToRight, Type.PrefixUnary, "", 16, false);

                Properties[Name.Comma] = new Op(Associativity.LeftToRight, Type.Binary, ",", 17, true);

                ByID = Properties.ToDictionary(x => x.Value.id, x => x.Key);
                ByPrecedence = new Dictionary<int, List<Name>>();
                for(int i = 0; i <= 17; ++i) {
                    ByPrecedence[i] = new List<Name>();
                    ByPrecedence[i].AddRange(from prop in Properties
                                             where prop.Value.precedence == i
                                             select prop.Key);
                }

                Lex = new List<Name>();
                foreach(var tup in Properties) {
                    if(tup.Value.implemented)
                        Lex.Add(tup.Key);
                }
                Lex.Sort((x, y) => {
                    return -Properties[x].cpp.Length.CompareTo(Properties[y].cpp.Length);
                });
                for(int i = 1; i < Lex.Count;) {
                    if(Properties[Lex[i]].cpp == Properties[Lex[i - 1]].cpp) {
                        Lex.RemoveAt(i);
                    }
                    else {
                        ++i;
                    }
                }
            }
        }
    }
}

