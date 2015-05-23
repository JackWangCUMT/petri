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

namespace Petri
{
	public class RenderView : PetriView {
		public RenderView(Document doc) : base(doc) {
			this.CurrentPetriNet = doc.PetriNet;
			this.EntityDraw = new RenderEntityDraw();
		}

		public void Render(string exportPath) {
			using(Cairo.PdfSurface surf = new Cairo.PdfSurface(exportPath, 0, 0)) {
				using(var context = new Cairo.Context(surf)) {
					this.RenderPetriNet(surf, context, CurrentPetriNet);
				}

				surf.Finish();
			}
		}

		protected void RenderPetriNet(Cairo.PdfSurface surface, Cairo.Context context, PetriNet petriNet) {
			this.RenderInternal(context, petriNet);
			surface.SetSize(petriNet.Size.X, petriNet.Size.Y);
			this.RenderInternal(context, petriNet);

			context.ShowPage();
			foreach(State s in petriNet.States) {
				if(s is PetriNet) {
					this.RenderPetriNet(surface, context, s as PetriNet);
				}
			}
		}

		protected override EntityDraw EntityDraw {
			get;
			set;
		}

		protected override void SpecializedDrawing(Cairo.Context context) {

		}
	}
}

