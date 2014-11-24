using System;
using Gtk;

namespace Petri
{
	public class DebugGui : Gui
	{
		public DebugGui(Document doc) {
			document = doc;

			toolbar = new HBox(false, 20);
			this.PackStart(toolbar, false, false, 0);
			toolbar.HeightRequest = 40;

			startStopSession = new Button(new Label("Démarrer session"));
			startStopPetri = new Button(new Label("Exécuter"));
			reload = new Button(new Label("Fix"));
			switchToEditor = new Button(new Label("Éditeur"));
			startStopSession.Clicked += this.OnClick;
			startStopPetri.Clicked += this.OnClick;
			reload.Clicked += this.OnClick;
			switchToEditor.Clicked += this.OnClick;
			toolbar.PackStart(startStopSession, false, false, 0);
			toolbar.PackStart(startStopPetri, false, false, 0);
			toolbar.PackStart(reload, false, false, 0);
			toolbar.PackStart(switchToEditor, false, false, 0);

			view = new DebugView(doc);
			view.CanFocus = true;
			view.CanDefault = true;
			view.AddEvents ((int) 
				(Gdk.EventMask.ButtonPressMask    
					|Gdk.EventMask.ButtonReleaseMask    
					|Gdk.EventMask.KeyPressMask    
					|Gdk.EventMask.PointerMotionMask));

			ScrolledWindow scrolledWindow = new ScrolledWindow();
			scrolledWindow.SetPolicy(PolicyType.Never, PolicyType.Automatic);

			Viewport viewport = new Viewport();

			viewport.Add(view);

			view.SizeRequested += (o, args) => {
				viewport.WidthRequest = viewport.Child.Requisition.Width;
			};

			scrolledWindow.Add(viewport);

			this.PackStart(scrolledWindow, true, true, 0);
		}

		public DebugView View {
			get {
				return view;
			}
		}

		public override void FocusIn() {
			view.FocusIn();
		}

		public override void FocusOut() {
			view.FocusOut();
		}

		public override void Redraw() {
			view.Redraw();
		}

		public override void UpdateToolbar() {
			if(document.DebugController.Server.SessionRunning) {
				startStopPetri.Sensitive = true;
				reload.Sensitive = true;

				((Label)startStopSession.Child).Text = "Stopper la session";

				if(document.DebugController.Server.PetriRunning) {
					((Label)startStopPetri.Child).Text = "Stopper";
				}
				else {
					((Label)startStopPetri.Child).Text = "Exécuter";
				}
			}
			else {
				((Label)startStopSession.Child).Text = "Démarrer la session";
				startStopPetri.Sensitive = false;
				reload.Sensitive = false;
			}
		}

		protected void OnClick(object sender, EventArgs e) {
			if(sender == this.startStopSession) {
				if(Server.SessionRunning) {
					Server.StopSession();
				}
				else {
					Server.StartSession();
				}
			}
			else if(sender == this.startStopPetri) {
				if(Server.PetriRunning) {
					Server.StopPetri();
				}
				else {
					Server.StartPetri();
				}
			}
			else if(sender == this.reload) {
				Server.ReloadPetri();
			}
			else if(sender == this.switchToEditor) {
				document.SwitchToEditor();
			}
		}

		protected DebugServer Server {
			get {
				if(server == null)
					server = document.DebugController.Server;

				return server;
			}
		}

		DebugServer server;
		DebugView view;
		Document document;
		HBox toolbar;
		Button startStopSession, startStopPetri, reload, switchToEditor;
	}
}

