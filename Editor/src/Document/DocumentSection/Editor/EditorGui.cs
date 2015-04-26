using System;
using Gtk;
using Gdk;

namespace Petri
{
	public class EditorGui : Gui
	{
		public EditorGui(Document doc) {
			_document = doc;

			_toolbar = new Toolbar();
			_toolbar.ToolbarStyle = ToolbarStyle.Both;
			this.PackStart(_toolbar, false, false, 0);

			_save = new ToolButton(Stock.Save);
			_save.Label = "Enregistrer";

			Pixbuf buf = Pixbuf.LoadFromResource("cpp");
			IconTheme.AddBuiltinIcon("CppGen", buf.Width, buf);
			_cpp = new ToolButton("CppGen");
			_cpp.IconName = "CppGen";
			_cpp.Label = "Générer C++";

			buf = Pixbuf.LoadFromResource("build");
			IconTheme.AddBuiltinIcon("Build", (int)(buf.Width / 0.8), buf);
			_compile = new ToolButton("Build");
			_compile.IconName = "Build";
			_compile.Label = "Compiler";

			buf = Pixbuf.LoadFromResource("bug");
			IconTheme.AddBuiltinIcon("Debug", (int)(buf.Width / 0.8), buf);
			_switchToDebug = new ToolButton("Debug");
			_switchToDebug.IconName = "Debug";
			_switchToDebug.Label = "Mode debug";

			_zoomIn = new ToolButton(Stock.ZoomIn);
			_zoomIn.Label = "Agrandir";
			_zoomOut = new ToolButton(Stock.ZoomOut);
			_zoomOut.Label = "Réduire";

			_save.Clicked += OnClick;
			_cpp.Clicked += OnClick;
			_compile.Clicked += OnClick;
			_switchToDebug.Clicked += OnClick;
			_zoomIn.Clicked += OnClick;
			_zoomOut.Clicked += OnClick;

			_toolbar.Insert(_save, -1);
			_toolbar.Insert(_cpp, -1);
			_toolbar.Insert(_compile, -1);
			_toolbar.Insert(_zoomIn, -1);
			_toolbar.Insert(_zoomOut, -1);
			_toolbar.Insert(_switchToDebug, -1);

			_paned = new HPaned();
			this.PackStart(_paned, true, true, 0);

			_paned.SizeRequested += (object o, SizeRequestedArgs args) => {
				_document.EditorController.EntityEditor.Resize((o as HPaned).Child2.Allocation.Width);
			};

			_petriView = new EditorView(doc);
			_petriView.CanFocus = true;
			_petriView.CanDefault = true;
			_petriView.AddEvents ((int) 
				(Gdk.EventMask.ButtonPressMask    
					|Gdk.EventMask.ButtonReleaseMask    
					|Gdk.EventMask.KeyPressMask    
					|Gdk.EventMask.PointerMotionMask));

			_scroll = new ScrolledWindow();
			_scroll.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);

			Viewport viewport = new Viewport();

			viewport.Add(_petriView);

			_petriView.SizeRequested += (o, args) => {
				viewport.WidthRequest = viewport.Child.Requisition.Width;
				viewport.HeightRequest = viewport.Child.Requisition.Height;
			};

			_scroll.Add(viewport);

			_paned.Pack1(_scroll, true, true);
			_editor = new Fixed();
			_paned.Pack2(_editor, false, true);
		}

		protected void OnClick(object sender, EventArgs e)
		{
			if(sender == _save) {
				_document.Save();
			}
			else if(sender == _cpp) {
				_document.SaveCpp();
			}
			else if(sender == _compile) {
				_document.Compile(false);
			}
			else if(sender == _switchToDebug) {
				_document.SwitchToDebug();
			}
			else if(sender == _zoomIn) {
				_petriView.Zoom /= 0.8f;
				if(_petriView.Zoom > 8f) {
					_petriView.Zoom = 8f;
				}
				_petriView.Redraw();
			}
			else if(sender == _zoomOut) {
				_petriView.Zoom *= 0.8f;
				if(_petriView.Zoom < 0.01f) {
					_petriView.Zoom = 0.01f;
				}
				_petriView.Redraw();
			}
		}

		public override void UpdateToolbar() {}

		public EditorView View {
			get {
				return _petriView;
			}
		}

		public override PetriView BaseView {
			get {
				return View;
			}
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

		public override void FocusIn() {
			_petriView.FocusIn();
		}

		public override void FocusOut() {
			_petriView.FocusOut();
		}

		public override void Redraw() {
			_petriView.Redraw();
		}

		public bool Compilation {
			set {
				GLib.Timeout.Add(0, () => { 
					if(value) {
						_compile.Sensitive = false;
					}
					else {
						_compile.Sensitive = true;
					}

					return false;
				});
			}
		}

		EditorView _petriView;
		Fixed _editor;

		ScrolledWindow _scroll;

		Toolbar _toolbar;
		ToolButton _save, _cpp, _compile, _switchToDebug, _zoomIn, _zoomOut;

		Document _document;
	}
}

