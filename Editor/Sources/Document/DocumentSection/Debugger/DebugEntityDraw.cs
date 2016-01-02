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
using Cairo;

namespace Petri.Editor
{
    public class DebugEntityDraw : EntityDraw
    {
        public DebugEntityDraw(Document document)
        {
            _document = document;
        }

        protected override void InitContextForBackground(State s, Context context)
        {
            Color color = new Color(1, 1, 1, 1);

            int enableCount;
            lock(_document.DebugController.ActiveStates) {
                if(_document.DebugController.ActiveStates.TryGetValue(s as State, out enableCount) == true && enableCount > 0) {
                    if(_document.DebugController.Client.Pause) {
                        color.R = 0.4;
                        color.G = 0.7;
                        color.B = 0.4;
                    }
                    else {
                        color.R = 0.6;
                        color.G = 1;
                        color.B = 0.6;
                    }
                }
                else if(s is InnerPetriNet) {
                    foreach(var a in _document.DebugController.ActiveStates) {
                        if((s as InnerPetriNet).EntityFromID(a.Key.ID) != null) {
                            if(_document.DebugController.Client.Pause) {
                                color.R = 0.7;
                                color.G = 0.4;
                                color.B = 0.7;
                            }
                            else {
                                color.R = 1;
                                color.G = 0.7;
                                color.B = 1;
                            }
                            break;
                        }
                    }
                }
            }

            context.SetSourceRGBA(color.R, color.G, color.B, color.A);
        }

        protected override void InitContextForBorder(State s, Context context)
        {
            base.InitContextForBorder(s, context);

            if(s is Action) {
                if(_document.DebugController.Breakpoints.Contains(s as Action)) {
                    context.SetSourceRGBA(1, 0, 0, 1);
                    context.LineWidth = 4;
                }
            }
            if(s == _document.Window.DebugGui.View.SelectedEntity) {
                context.LineWidth += 1;
            }
        }

        protected override void InitContextForBorder(Transition t, Context context)
        {
            base.InitContextForBorder(t, context);
            if(t == _document.Window.DebugGui.View.SelectedEntity) {
                context.LineWidth += 1;
            }
        }

        protected override void InitContextForLine(Transition t, Context context)
        {
            base.InitContextForLine(t, context);
            if(t == _document.Window.DebugGui.View.SelectedEntity) {
                context.LineWidth += 1;
            }
        }

        Document _document;
    }
}

