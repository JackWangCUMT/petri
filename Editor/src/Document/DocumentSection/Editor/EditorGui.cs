using System;
using Gtk;

namespace Petri
{
	public class EditorGui : Gui
	{
		public EditorGui(Document doc) {
			_document = doc;

			_toolbar = new HBox(false, 20);
			this.PackStart(_toolbar, false, false, 0);
			_toolbar.HeightRequest = 40;

			_cpp = new Button(new Label("Générer C++…"));
			_manageHeaders = new Button(new Label("Ouvrir un .h…"));
			_compile = new Button(new Label("Compiler le code généré…"));
			_switchToDebug = new Button(new Label("Debug"));
			_cpp.Clicked += this.OnClick;
			_manageHeaders.Clicked += this.OnClick;
			_compile.Clicked += this.OnClick;
			_switchToDebug.Clicked += this.OnClick;
			_toolbar.PackStart(_cpp, false, false, 0);
			_toolbar.PackStart(_manageHeaders, false, false, 0);
			_toolbar.PackStart(_compile, false, false, 0);
			_toolbar.PackStart(_switchToDebug, false, false, 0);

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

			ScrolledWindow scrolledWindow = new ScrolledWindow();
			scrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);

			Viewport viewport = new Viewport();

			viewport.Add(_petriView);

			_petriView.SizeRequested += (o, args) => {
				viewport.WidthRequest = viewport.Child.Requisition.Width;
				viewport.HeightRequest = viewport.Child.Requisition.Height;
			};

			scrolledWindow.Add(viewport);

			_paned.Pack1(scrolledWindow, true, true);
			_editor = new Fixed();
			_paned.Pack2(_editor, false, true);
		}

		protected void OnClick(object sender, EventArgs e)
		{
			if(sender == _cpp) {
				_document.SaveCpp();
			}
			else if(sender == _manageHeaders) {
				_document.ManageHeaders();
			}
			else if(sender == _compile) {
				_document.Compile();
			}
			else if(sender == _switchToDebug) {
				_document.SwitchToDebug();
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

		public override void FocusIn() {
			_petriView.FocusIn();
		}

		public override void FocusOut() {
			_petriView.FocusOut();
		}

		public override void Redraw() {
			_petriView.Redraw();
		}

		HBox _toolbar;
		EditorView _petriView;
		Fixed _editor;
		Button _manageHeaders, _cpp, _compile, _switchToDebug;

		Document _document;
	}
}

