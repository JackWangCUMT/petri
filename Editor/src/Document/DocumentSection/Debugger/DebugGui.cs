using System;
using Gtk;

namespace Petri
{
	public class DebugGui : Gui
	{
		public DebugGui(Document doc) {
			_document = doc;

			_toolbar = new HBox(false, 20);
			this.PackStart(_toolbar, false, false, 0);
			_toolbar.HeightRequest = 40;

			_startStopSession = new Button(new Label("Démarrer session"));
			_startStopPetri = new Button(new Label("Exécuter"));
			_playPause = new Button(new Label("Pause"));
			_reload = new Button(new Label("Fix"));
			_switchToEditor = new Button(new Label("Éditeur"));
			_startStopSession.Clicked += this.OnClick;
			_startStopPetri.Clicked += this.OnClick;
			_playPause.Clicked += this.OnClick;
			_reload.Clicked += this.OnClick;
			_switchToEditor.Clicked += this.OnClick;
			_toolbar.PackStart(_startStopSession, false, false, 0);
			_toolbar.PackStart(_startStopPetri, false, false, 0);
			_toolbar.PackStart(_playPause, false, false, 0);
			_toolbar.PackStart(_reload, false, false, 0);
			_toolbar.PackStart(_switchToEditor, false, false, 0);

			_view = new DebugView(doc);
			_view.CanFocus = true;
			_view.CanDefault = true;
			_view.AddEvents ((int) 
				(Gdk.EventMask.ButtonPressMask    
					|Gdk.EventMask.ButtonReleaseMask    
					|Gdk.EventMask.KeyPressMask    
					|Gdk.EventMask.PointerMotionMask));

			ScrolledWindow scrolledWindow = new ScrolledWindow();
			scrolledWindow.SetPolicy(PolicyType.Never, PolicyType.Automatic);

			Viewport viewport = new Viewport();

			viewport.Add(_view);

			_view.SizeRequested += (o, args) => {
				viewport.WidthRequest = viewport.Child.Requisition.Width;
			};

			scrolledWindow.Add(viewport);

			this.PackStart(scrolledWindow, true, true, 0);
		}

		public DebugView View {
			get {
				return _view;
			}
		}

		public override PetriView BaseView {
			get {
				return View;
			}
		}

		public override void FocusIn() {
			_view.FocusIn();
		}

		public override void FocusOut() {
			_view.FocusOut();
		}

		public override void Redraw() {
			_view.Redraw();
		}

		public override void UpdateToolbar() {
			if(_document.DebugController.Server.SessionRunning) {
				_startStopPetri.Sensitive = true;
				_reload.Sensitive = true;


				((Label)_startStopSession.Child).Text = "Stopper la session";

				if(_document.DebugController.Server.PetriRunning) {
					((Label)_startStopPetri.Child).Text = "Stopper";
					_playPause.Sensitive = true;
					if(_document.DebugController.Server.Pause) {
						((Label)_playPause.Child).Text = "Continuer";
					}
					else {
						((Label)_playPause.Child).Text = "Pause";
					}
				}
				else {
					((Label)_startStopPetri.Child).Text = "Exécuter";
					_playPause.Sensitive = false;
					((Label)_playPause.Child).Text = "Pause";
				}
			}
			else {
				((Label)_startStopSession.Child).Text = "Démarrer la session";
				((Label)_startStopPetri.Child).Text = "Exécuter";
				_startStopPetri.Sensitive = false;
				_reload.Sensitive = false;
				_playPause.Sensitive = false;
				((Label)_playPause.Child).Text = "Pause";
			}
		}

		protected void OnClick(object sender, EventArgs e) {
			if(sender == _startStopSession) {
				if(Server.SessionRunning) {
					Server.StopSession();
				}
				else {
					Server.StartSession();
				}
			}
			else if(sender == _startStopPetri) {
				if(Server.PetriRunning) {
					Server.StopPetri();
				}
				else {
					Server.StartPetri();
				}
			}
			else if(sender == _playPause) {
				Server.Pause = !Server.Pause;
			}
			else if(sender == _reload) {
				Server.ReloadPetri();
			}
			else if(sender == _switchToEditor) {
				_document.SwitchToEditor();
			}
		}

		protected DebugServer Server {
			get {
				return _document.DebugController.Server;
			}
		}

		DebugView _view;
		Document _document;
		HBox _toolbar;
		Button _startStopSession, _startStopPetri, _playPause, _reload, _switchToEditor;
	}
}

