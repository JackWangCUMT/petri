using System;
using Gtk;

namespace Petri
{
	public class EditorGui : Gui
	{
		public EditorGui(Document doc) {
			document = doc;

			toolbar = new HBox(false, 20);
			this.PackStart(toolbar, false, false, 0);
			toolbar.HeightRequest = 40;

			cpp = new Button(new Label("Générer C++…"));
			manageHeaders = new Button(new Label("Ouvrir un .h…"));
			compile = new Button(new Label("Compiler le code généré…"));
			switchToDebug = new Button(new Label("Debug"));
			cpp.Clicked += this.OnClick;
			manageHeaders.Clicked += this.OnClick;
			compile.Clicked += this.OnClick;
			switchToDebug.Clicked += this.OnClick;
			toolbar.PackStart(cpp, false, false, 0);
			toolbar.PackStart(manageHeaders, false, false, 0);
			toolbar.PackStart(compile, false, false, 0);
			toolbar.PackStart(switchToDebug, false, false, 0);

			this.hbox = new HBox(false, 0);
			this.PackStart(hbox, true, true, 0);
			//this.paned = new HPaned();
			//vbox.PackStart(paned, true, true, 0);

			petriView = new EditorView(doc);
			petriView.CanFocus = true;
			petriView.CanDefault = true;
			petriView.AddEvents ((int) 
				(Gdk.EventMask.ButtonPressMask    
					|Gdk.EventMask.ButtonReleaseMask    
					|Gdk.EventMask.KeyPressMask    
					|Gdk.EventMask.PointerMotionMask));

			ScrolledWindow scrolledWindow = new ScrolledWindow();
			scrolledWindow.SetPolicy(PolicyType.Never, PolicyType.Automatic);

			Viewport viewport = new Viewport();

			viewport.Add(petriView);

			petriView.SizeRequested += (o, args) => {
				viewport.WidthRequest = viewport.Child.Requisition.Width;
			};

			scrolledWindow.Add(viewport);

			//paned.Position = Configuration.GraphWidth;
			//paned.Pack1(drawing, true, true);
			hbox.PackStart(scrolledWindow, true, true, 0);
			editor = new Fixed();
			//paned.Pack2(editor, true, true);
			hbox.PackEnd(editor, false, false, 0);
		}

		protected void OnClick(object sender, EventArgs e)
		{
			if(sender == this.cpp) {
				document.SaveCpp();
			}
			else if(sender == this.manageHeaders) {
				document.ManageHeaders();
			}
			else if(sender == this.compile) {
				document.Compile();
			}
			else if(sender == this.switchToDebug) {
				document.SwitchToDebug();
			}
		}

		public override void UpdateToolbar() {}

		public EditorView View {
			get {
				return petriView;
			}
		}

		public override PetriView BaseView {
			get {
				return View;
			}
		}

		public Fixed Editor {
			get {
				return editor;
			}
		}

		public override void FocusIn() {
			petriView.FocusIn();
		}

		public override void FocusOut() {
			petriView.FocusOut();
		}

		public override void Redraw() {
			petriView.Redraw();
		}

		HBox hbox;
		HBox toolbar;
		EditorView petriView;
		Fixed editor;
		Button manageHeaders, cpp, compile, switchToDebug;

		Document document;
	}
}

