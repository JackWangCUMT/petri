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

