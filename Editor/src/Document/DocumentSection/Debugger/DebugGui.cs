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

			_paned = new HPaned();
			this.PackStart(_paned, true, true, 0);

			_paned.SizeRequested += (object o, SizeRequestedArgs args) => {
				_document.DebugController.DebugEditor.Resize((o as HPaned).Child2.Allocation.Width);
			};

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

			_paned.Pack1(scrolledWindow, true, true);
			_editor = new Fixed();
			_paned.Pack2(_editor, false, true);
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
					_document.DebugController.DebugEditor.Evaluate.Sensitive = false;
					if(_document.DebugController.Server.Pause) {
						((Label)_playPause.Child).Text = "Continuer";
						_document.DebugController.DebugEditor.Evaluate.Sensitive = true;
					}
					else {
						((Label)_playPause.Child).Text = "Pause";
						_document.DebugController.DebugEditor.Evaluate.Sensitive = false;
					}
				}
				else {
					((Label)_startStopPetri.Child).Text = "Exécuter";
					_playPause.Sensitive = false;
					_document.DebugController.DebugEditor.Evaluate.Sensitive = true;
					((Label)_playPause.Child).Text = "Pause";
				}
			}
			else {
				((Label)_startStopSession.Child).Text = "Démarrer la session";
				((Label)_startStopPetri.Child).Text = "Exécuter";
				_startStopPetri.Sensitive = false;
				_reload.Sensitive = false;
				_playPause.Sensitive = false;
				_document.DebugController.DebugEditor.Evaluate.Sensitive = false;
				((Label)_playPause.Child).Text = "Pause";
			}
		}

		public void OnEvaluate(string result) {
			_document.DebugController.DebugEditor.OnEvaluate(result);
		}

		public override Fixed Editor {
			get {
				return _editor;
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

		Fixed _editor;

		DebugView _view;
		Document _document;
		HBox _toolbar;
		Button _startStopSession, _startStopPetri, _playPause, _reload, _switchToEditor;
	}
}

