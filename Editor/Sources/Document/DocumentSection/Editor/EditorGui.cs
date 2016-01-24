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
using Gtk;
using Gdk;
using System.Collections.Generic;

namespace Petri.Editor
{
    public class EditorGui : Gui
    {
        public EditorGui(Document doc)
        {
            _document = doc;

            _find = new Find(doc);

            _toolbar = new Toolbar();
            _toolbar.ToolbarStyle = ToolbarStyle.Both;
            this.PackStart(_toolbar, false, false, 0);

            _save = new ToolButton(Stock.Save);
            _save.Label = Configuration.GetLocalized("Save");

            Pixbuf buf = Pixbuf.LoadFromResource("cpp");
            IconTheme.AddBuiltinIcon("CppGen", buf.Width, buf);
            _cpp = new ToolButton("CppGen");
            _cpp.IconName = "CppGen";
            _cpp.Label = Configuration.GetLocalized("Generate <language>", "C++");
            _document.LanguageChanged += (sender, e) => _cpp.Label = Configuration.GetLocalized("Generate <language>",
                                                                                                DocumentSettings.LanguageName(e.NewLanguage));

            buf = Pixbuf.LoadFromResource("build");
            IconTheme.AddBuiltinIcon("Build", (int)(buf.Width / 0.8), buf);
            _compile = new ToolButton("Build");
            _compile.IconName = "Build";
            _compile.Label = Configuration.GetLocalized("Compile");

            buf = Pixbuf.LoadFromResource("arrow");
            IconTheme.AddBuiltinIcon("Arrow", (int)(buf.Width / 0.8), buf);
            _arrow = new ToggleToolButton("Arrow");
            _arrow.Active = true;
            _arrow.IconName = "Arrow";
            _arrow.Label = Configuration.GetLocalized("Select");

            buf = Pixbuf.LoadFromResource("action");
            IconTheme.AddBuiltinIcon("Action", (int)(buf.Width / 0.8), buf);
            _action = new ToggleToolButton("Action");
            _action.IconName = "Action";
            _action.Label = Configuration.GetLocalized("State toolbar");

            buf = Pixbuf.LoadFromResource("transition");
            IconTheme.AddBuiltinIcon("Transition", (int)(buf.Width / 0.8), buf);
            _transition = new ToggleToolButton("Transition");
            _transition.IconName = "Transition";
            _transition.Label = Configuration.GetLocalized("Transition toolbar");

            buf = Pixbuf.LoadFromResource("comment");
            IconTheme.AddBuiltinIcon("Comment", (int)(buf.Width / 0.8), buf);
            _comment = new ToggleToolButton("Comment");
            _comment.IconName = "Comment";
            _comment.Label = Configuration.GetLocalized("Comment toolbar");

            buf = Pixbuf.LoadFromResource("bug");
            IconTheme.AddBuiltinIcon("Debug", (int)(buf.Width / 0.8), buf);
            _switchToDebug = new ToolButton("Debug");
            _switchToDebug.IconName = "Debug";
            _switchToDebug.Label = Configuration.GetLocalized("Debug");

            _zoomIn = new ToolButton(Stock.ZoomIn);
            _zoomIn.Label = Configuration.GetLocalized("Zoom In toolbar");
            _zoomOut = new ToolButton(Stock.ZoomOut);
            _zoomOut.Label = Configuration.GetLocalized("Zoom Out toolbar");

            _findTool = new ToolButton(Stock.Find);
            _findTool.Label = Configuration.GetLocalized("Find");

            _save.Clicked += OnClick;
            _cpp.Clicked += OnClick;
            _compile.Clicked += OnClick;
            _switchToDebug.Clicked += OnClick;
            _zoomIn.Clicked += OnClick;
            _zoomOut.Clicked += OnClick;
            _arrow.Clicked += OnClick;
            _action.Clicked += OnClick;
            _transition.Clicked += OnClick;
            _comment.Clicked += OnClick;
            _findTool.Clicked += OnClick;

            _toolbar.Insert(_save, -1);
            _toolbar.Insert(_cpp, -1);
            _toolbar.Insert(_compile, -1);

            _toolbar.Insert(new SeparatorToolItem(), -1);

            _toolbar.Insert(_arrow, -1);
            _toolbar.Insert(_action, -1);
            _toolbar.Insert(_transition, -1);
            _toolbar.Insert(_comment, -1);

            _toolbar.Insert(new SeparatorToolItem(), -1);

            _toolbar.Insert(_zoomIn, -1);
            _toolbar.Insert(_zoomOut, -1);

            _toolbar.Insert(new SeparatorToolItem(), -1);

            _toolbar.Insert(_findTool, -1);

            _toolbar.Insert(new SeparatorToolItem(), -1);

            _toolbar.Insert(_switchToDebug, -1);

            _paned = new HPaned();
            _vpaned = new VPaned();

            _findView = new TreeView();
            _findView.RowActivated += (o, args) => {
                _petriView.SelectedEntity = _findResults[int.Parse(args.Path.ToString())];
            };

            TreeViewColumn c0 = new TreeViewColumn();
            c0.Title = Configuration.GetLocalized("ID");
            var idCell = new Gtk.CellRendererText();
            c0.PackStart(idCell, true);
            c0.AddAttribute(idCell, "text", 0);

            TreeViewColumn c1 = new TreeViewColumn();
            c1.Title = Configuration.GetLocalized("Kind");
            var typeCell = new Gtk.CellRendererText();
            c1.PackStart(typeCell, true);
            c1.AddAttribute(typeCell, "text", 1);

            TreeViewColumn c2 = new TreeViewColumn();
            c2.Title = Configuration.GetLocalized("Name");
            var nameCell = new Gtk.CellRendererText();
            c2.PackStart(nameCell, true);
            c2.AddAttribute(nameCell, "text", 2);

            TreeViewColumn c3 = new TreeViewColumn();
            c2.Title = Configuration.GetLocalized("Value");
            var valueCell = new Gtk.CellRendererText();
            c2.PackStart(valueCell, true);
            c2.AddAttribute(valueCell, "text", 3);

            _findView.AppendColumn(c0);
            _findView.AppendColumn(c1);
            _findView.AppendColumn(c2);
            _findView.AppendColumn(c3);
            _findStore = new Gtk.ListStore(typeof(string),
                                           typeof(string),
                                           typeof(string),
                                           typeof(string));
            _findView.Model = _findStore;

            var scroll = new ScrolledWindow();
            scroll.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);

            Viewport viewport1 = new Viewport();

            viewport1.Add(_findView);

            _findView.SizeRequested += (o, args) => {
                viewport1.WidthRequest = viewport1.Child.Requisition.Width;
                viewport1.HeightRequest = viewport1.Child.Requisition.Height;
            };

            scroll.Add(viewport1);

            _vpaned.Pack2(scroll, true, true);
            _vpaned.Position = _vpaned.MaxPosition;

            _vpaned.Realized += (object sender, EventArgs e) => {
                _vpaned.Position = _vpaned.MaxPosition;
            };

            this.PackStart(_paned, true, true, 0);

            _paned.SizeRequested += (object o, SizeRequestedArgs args) => {
                _document.EditorController.EntityEditor.Resize((o as HPaned).Child2.Allocation.Width);
            };

            _petriView = new EditorView(doc);
            _petriView.CanFocus = true;
            _petriView.CanDefault = true;
            _petriView.AddEvents((int)
				(Gdk.EventMask.ButtonPressMask
            | Gdk.EventMask.ButtonReleaseMask
            | Gdk.EventMask.KeyPressMask
            | Gdk.EventMask.PointerMotionMask));

            _scroll = new ScrolledWindow();
            _scroll.Hadjustment.ValueChanged += (object sender, EventArgs e) => {
                _petriView.Redraw();
            };
            _scroll.Vadjustment.ValueChanged += (object sender, EventArgs e) => {
                _petriView.Redraw();
            };

            _scroll.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);

            Viewport viewport2 = new Viewport();

            viewport2.Add(_petriView);

            _petriView.SizeRequested += (o, args) => {
                viewport2.WidthRequest = viewport2.Child.Requisition.Width;
                viewport2.HeightRequest = viewport2.Child.Requisition.Height;
            };

            _scroll.Add(viewport2);
            _vpaned.Pack1(_scroll, true, true);

            _paned.Pack1(_vpaned, true, true);
            _editor = new Fixed();
            _paned.Pack2(_editor, false, true);
        }

        protected void OnClick(object sender, EventArgs e)
        {
            if(_toggling)
                return;
            _toggling = true;

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
            else if(sender == _arrow) {
                _petriView.CurrentTool = EditorView.EditorTool.Arrow;
                _arrow.Active = true;
                _action.Active = false;
                _transition.Active = false;
                _comment.Active = false;
            }
            else if(sender == _action) {
                _petriView.CurrentTool = EditorView.EditorTool.Action;
                _arrow.Active = false;
                _action.Active = true;
                _transition.Active = false;
                _comment.Active = false;
            }
            else if(sender == _transition) {
                _petriView.CurrentTool = EditorView.EditorTool.Transition;
                _arrow.Active = false;
                _action.Active = false;
                _transition.Active = true;
                _comment.Active = false;
            }
            else if(sender == _comment) {
                _petriView.CurrentTool = EditorView.EditorTool.Comment;
                _arrow.Active = false;
                _action.Active = false;
                _transition.Active = false;
                _comment.Active = true;
            }
            else if(sender == _findTool) {
                Find();
            }

            _toggling = false;
        }

        public void Find()
        {
            _find.Show();
        }

        public ToolButton CodeGen {
            get {
                return _cpp;
            }
        }

        public void PerformFind(string what, Find.FindType type)
        {
            _document.SwitchToEditor();
            _findResults.Clear();
            _findStore.Clear();

            var list = _document.PetriNet.BuildEntitiesList();
            foreach(var ee in list) {
                if(ee is Action && (type == Petri.Editor.Find.FindType.All || type == Petri.Editor.Find.FindType.Action)) {
                    var e = (Action)ee;
                    if(e.Function.MakeUserReadable().Contains(what)) {
                        _findResults.Add(e);
                        _findStore.AppendValues(e.ID.ToString(),
                                                Configuration.GetLocalized("State"),
                                                e.Name,
                                                e.Function.MakeUserReadable());
                    }
                }
                else if(ee is Transition && (type == Petri.Editor.Find.FindType.All || type == Petri.Editor.Find.FindType.Transition)) {
                    var e = (Transition)ee;
                    if(e.Condition.MakeUserReadable().Contains(what)) {
                        _findResults.Add(e);
                        _findStore.AppendValues(e.ID.ToString(),
                                                Configuration.GetLocalized("Transition"),
                                                e.Name,
                                                e.Condition.MakeUserReadable());
                    }
                }
                else if(ee is Comment && (type == Petri.Editor.Find.FindType.All || type == Petri.Editor.Find.FindType.Comment)) {
                    var e = (Comment)ee;
                    if(e.Name.Contains(what)) {
                        _findResults.Add(e);
                        _findStore.AppendValues(e.ID.ToString(),
                                                Configuration.GetLocalized("Comment"),
                                                "-",
                                                e.Name);
                    }
                }
            }

            if(_vpaned.Position == _vpaned.MaxPosition) {
                _vpaned.Position = Math.Max((_vpaned.MaxPosition - _vpaned.MinPosition) / 2,
                                            (_vpaned.HeightRequest - _scroll.HeightRequest));
            }
        }

        public override void UpdateToolbar()
        {
        }

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

        public override void FocusIn()
        {
            _petriView.FocusIn();
        }

        public override void FocusOut()
        {
            _petriView.FocusOut();
        }

        public override void Redraw()
        {
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

        Find _find;
        List<Entity> _findResults = new List<Entity>();

        EditorView _petriView;
        Fixed _editor;
        VPaned _vpaned;
        TreeView _findView;
        ListStore _findStore;

        ScrolledWindow _scroll;

        Toolbar _toolbar;
        ToolButton _save, _cpp, _compile, _switchToDebug, _zoomIn, _zoomOut, _findTool;
        ToggleToolButton _arrow, _action, _transition, _comment;

        bool _toggling = false;

        Document _document;
    }
}

