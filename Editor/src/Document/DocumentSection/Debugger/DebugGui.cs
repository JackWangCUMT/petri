using System;
using Gtk;
using Gdk;

namespace Petri
{
	public class DebugGui : Gui
	{
		public DebugGui(Document doc) {
			_document = doc;

			_toolbar = new Toolbar();
			_toolbar.ToolbarStyle = ToolbarStyle.Both;
			this.PackStart(_toolbar, false, false, 0);

			_attachDetach = new ToolButton(Stock.Network);

			_startStopPetri = new ToolButton(Stock.MediaPlay);
			_playPause = new ToolButton(Stock.MediaPause);
			Pixbuf buf = Pixbuf.LoadFromResource("fix");
			IconTheme.AddBuiltinIcon("Fix", buf.Width, buf);
			_reload = new ToolButton("Fix");
			_reload.IconName = "Fix";
			_reload.Label = "Fix";
			_exit = new ToolButton(Stock.Quit);
			_exit.Label = "Terminer la session";
			_switchToEditor = new ToolButton(Stock.Edit);
			_switchToEditor.Label = "Éditeur";
			_zoomIn = new ToolButton(Stock.ZoomIn);
			_zoomIn.Label = "Agrandir";
			_zoomOut = new ToolButton(Stock.ZoomOut);
			_zoomOut.Label = "Réduire";

			_attachDetach.Clicked += this.OnClick;
			_startStopPetri.Clicked += this.OnClick;
			_playPause.Clicked += this.OnClick;
			_reload.Clicked += this.OnClick;
			_exit.Clicked += this.OnClick;
			_switchToEditor.Clicked += this.OnClick;
			_zoomIn.Clicked += OnClick;
			_zoomOut.Clicked += OnClick;

			_toolbar.Insert(_attachDetach, -1);
			_toolbar.Insert(_exit, -1);

			_toolbar.Insert(new SeparatorToolItem(), -1);

			_toolbar.Insert(_startStopPetri, -1);
			_toolbar.Insert(_playPause, -1);
			_toolbar.Insert(_reload, -1);

			_toolbar.Insert(new SeparatorToolItem(), -1);

			_toolbar.Insert(_zoomIn, -1);
			_toolbar.Insert(_zoomOut, -1);

			_toolbar.Insert(new SeparatorToolItem(), -1);

			_toolbar.Insert(_switchToEditor, -1);

			_paned = new HPaned();
			this.PackStart(_paned, true, true, 0);

			_paned.SizeRequested += (object o, SizeRequestedArgs args) => {
				HPaned p = (HPaned)o;
				if(p.Child2.Allocation.Width != 1) {
					_document.DebugController.DebugEditor.Resize(p.Child2.Allocation.Width);
				}
				else {
					int x, y;
					p.RootWindow.GetSize(out x, out y);
					p.Position = x - 250;
					_document.DebugController.DebugEditor.Resize(200);
				}
			};

			_view = new DebugView(doc);
			_view.CanFocus = true;
			_view.CanDefault = true;
			_view.AddEvents ((int) 
				(Gdk.EventMask.ButtonPressMask    
					|Gdk.EventMask.ButtonReleaseMask    
					|Gdk.EventMask.KeyPressMask    
					|Gdk.EventMask.PointerMotionMask));

			_scroll = new ScrolledWindow();
			_scroll.SetPolicy(PolicyType.Never, PolicyType.Automatic);

			Viewport viewport = new Viewport();

			viewport.Add(_view);

			_view.SizeRequested += (o, args) => {
				viewport.WidthRequest = viewport.Child.Requisition.Width;
			};

			_scroll.Add(viewport);

			_paned.Pack1(_scroll, true, true);
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
			GLib.Timeout.Add(0, () => {
				if(_document.DebugController.Server.SessionRunning) {
					_startStopPetri.Sensitive = true;
					_reload.Sensitive = true;
					_exit.Sensitive = true;

					_attachDetach.Label = "Déconnexion";

					if(_document.DebugController.Server.PetriRunning) {
						_startStopPetri.Label = "Stopper";
						_startStopPetri.StockId = Stock.Stop;
						_playPause.Sensitive = true;
						_document.DebugController.DebugEditor.Evaluate.Sensitive = false;
						if(_document.DebugController.Server.Pause) {
							_playPause.Label = "Continuer";
							_playPause.StockId = Stock.MediaPlay;
							_document.DebugController.DebugEditor.Evaluate.Sensitive = true;
						}
						else {
							_playPause.Label = "Pause";
							_playPause.StockId = Stock.MediaPause;
							_document.DebugController.DebugEditor.Evaluate.Sensitive = false;
						}
					}
					else {
						_startStopPetri.Label = "Exécuter";
						_startStopPetri.StockId = Stock.MediaPlay;
						_playPause.Sensitive = false;
						_document.DebugController.DebugEditor.Evaluate.Sensitive = true;
						_playPause.Label = "Pause";
						_playPause.StockId = Stock.MediaPause;
					}
				}
				else {
					_attachDetach.Label = "Connexion";
					_startStopPetri.Label = "Exécuter";
					_startStopPetri.StockId = Stock.MediaPlay;
					_startStopPetri.Sensitive = false;
					_reload.Sensitive = false;
					_playPause.Sensitive = false;
					_exit.Sensitive = false;
					_document.DebugController.DebugEditor.Evaluate.Sensitive = false;
					_playPause.Label = "Pause";
					_playPause.StockId = Stock.MediaPause;
				}

				return false;
			});
		}

		public void OnEvaluate(string result) {
			_document.DebugController.DebugEditor.OnEvaluate(result);
		}

		public override Fixed Editor {
			get {
				return _editor;
			}
		}

		public override ScrolledWindow ScrolledWindow {
			get {
				return _scroll;
			}
		}

		protected void OnClick(object sender, EventArgs e) {
			if(sender == _attachDetach) {
				if(Server.SessionRunning) {
					Server.Detach();
				}
				else {
					Server.Attach();
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
			else if(sender == _exit) {
				Server.StopSession();
			}
			else if(sender == _zoomIn) {
				_view.Zoom /= 0.8f;
				if(_view.Zoom > 8f) {
					_view.Zoom = 8f;
				}
				_view.Redraw();
			}
			else if(sender == _zoomOut) {
				_view.Zoom *= 0.8f;
				if(_view.Zoom < 0.01f) {
					_view.Zoom = 0.01f;
				}
				_view.Redraw();
			}
		}

		protected DebugServer Server {
			get {
				return _document.DebugController.Server;
			}
		}

		public bool Compilation {
			set {
				GLib.Timeout.Add(0, () => { 
					if(value) {
						_reload.Sensitive = false;
					}
					else {
						_reload.Sensitive = true;
					}

					return false;
				});
			}
		}

		Fixed _editor;
		ScrolledWindow _scroll;

		DebugView _view;
		Document _document;
		ToolButton _attachDetach, _startStopPetri, _playPause, _reload, _exit, _zoomIn, _zoomOut, _switchToEditor;
		Toolbar _toolbar;
	}
}

