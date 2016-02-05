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
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using System.Linq;
using Gtk;

namespace Petri.Editor
{
    public class DebugClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.DebugClient"/> class.
        /// </summary>
        /// <param name="doc">Document.</param>
        public DebugClient(Document doc)
        {
            _document = doc;
            _sessionRunning = false;
            _petriRunning = false;
            _pause = false;
        }

        ~DebugClient()
        {
            if(_petriRunning || _sessionRunning) {
                throw new Exception(Configuration.GetLocalized("Debugger still running!"));
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Petri.Editor.DebugClient"/> is attached to a DebugServer instance.
        /// </summary>
        /// <value><c>true</c> if session running; otherwise, <c>false</c>.</value>
        public bool SessionRunning {
            get {
                return _sessionRunning;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the PetriNet enclosed in the DebugServer's dynamic library is running or not.
        /// </summary>
        /// <value><c>true</c> if the petri net running; otherwise, <c>false</c>.</value>
        public bool PetriRunning {
            get {
                return _petriRunning;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Petri.Editor.DebugClient"/> is paused.
        /// The pause will be effective just after the states of the petri net that are active when the message is received have finished their execution.
        /// </summary>
        /// <value><c>true</c> if pause; otherwise, <c>false</c>.</value>
        public bool Pause {
            get {
                return _pause;
            }
            set {
                if(PetriRunning) {
                    try {
                        if(value) {
                            this.SendObject(new JObject(new JProperty("type", "pause")));
                        }
                        else {
                            this.SendObject(new JObject(new JProperty("type", "resume")));
                        }
                    }
                    catch(Exception e) {
                        GLib.Timeout.Add(0, () => {
                            MessageDialog d = new MessageDialog(_document.Window,
                                                                DialogFlags.Modal,
                                                                MessageType.Question,
                                                                ButtonsType.None,
                                                                Application.SafeMarkupFromString(Configuration.GetLocalized("An error occurred in the debugger when pausing the petri net:") + " " + e.Message));
                            d.AddButton(Configuration.GetLocalized("Cancel"), ResponseType.Cancel);
                            d.Run();
                            d.Destroy();

                            return false;
                        });

                        this.Detach();
                    }
                }
                else {
                    _pause = false;
                    _document.Window.DebugGui.UpdateToolbar();
                }
            }
        }

        /// <summary>
        /// Gets the version of the debugger.
        /// </summary>
        /// <value>The version.</value>
        public static string Version {
            get {
                return "1.3.2";
            }
        }

        /// <summary>
        /// Attach this instance to a DebugServer listening on the same port as in the Settings of the document.
        /// </summary>
        public void Attach()
        {
            bool success = true;
            if(_document.Settings.RunInEditor) {
                success = LoadLibAndStartServer();
            }

            if(success) {
                _sessionRunning = true;
                _receiverThread = new Thread(this.Receiver);
                _pause = false;
                _receiverThread.Start();
                DateTime time = DateTime.Now.AddSeconds(1);
                while(_socket == null && DateTime.Now.CompareTo(time) < 0)
                    System.Threading.Thread.Sleep(20);
            }
        }

        /// <summary>
        /// Disconnect this instance from its DebugServer.
        /// </summary>
        public void Detach()
        {
            if(_document.Settings.RunInEditor) {
                // If the petri net is running in the editor, we have to stop it upon detach.
                this.StopOrDetach(true);
                UnloadLibAndStopServer();
            }
            else {
                this.StopOrDetach(false);
            }
        }

        /// <summary>
        /// Loads the library and starts a local DebugServer.
        /// Valid only when the document is set to Run in the editor.
        /// </summary>
        bool LoadLibAndStartServer()
        {
            UnloadLibAndStopServer();

            _libProxy = new GeneratedDynamicLibProxy(_document.Settings.Language, System.IO.Directory.GetParent(_document.Path).FullName,
                                                     _document.Settings.RelativeLibPath,
                                                     _document.Settings.Name);
            try {
                var dylib = _libProxy.Load<Petri.Runtime.GeneratedDynamicLib>();

                if(dylib == null) {
                    GLib.Timeout.Add(0, () => {
                        MessageDialog d = new MessageDialog(_document.Window,
                                                            DialogFlags.Modal,
                                                            MessageType.Question,
                                                            ButtonsType.None,
                                                            Application.SafeMarkupFromString(Configuration.GetLocalized("Unable to load the dynamic library! Try to compile it again.")));
                        d.AddButton(Configuration.GetLocalized("Cancel"), ResponseType.Cancel);
                        d.AddButton(Configuration.GetLocalized("Fix"), ResponseType.Apply);

                        if((ResponseType)d.Run() == ResponseType.Apply) {
                            d.Destroy();
                            _document.Compile(true);
                            Attach();
                        }
                        else {
                            d.Destroy();
                        }

                        return false;
                    });

                    return false;
                }

                _dynamicLib = dylib.Lib;
                _debugServer = new Runtime.DebugServer(_dynamicLib);
                _debugServer.Start();
            }
            catch(Exception e) {
                UnloadLibAndStopServer();
                GLib.Timeout.Add(0, () => {
                    MessageDialog d = new MessageDialog(_document.Window,
                                                        DialogFlags.Modal,
                                                        MessageType.Question,
                                                        ButtonsType.None,
                                                        Application.SafeMarkupFromString(Configuration.GetLocalized("An error occurred in the debugger when loading the lib:") + " " + e.Message));
                    d.AddButton(Configuration.GetLocalized("Cancel"), ResponseType.Cancel);
                    d.Run();
                    d.Destroy();

                    return false;
                });

                return false;
            }

            return true;
        }

        /// <summary>
        /// Stops the local DebugServer and unloads the assembly containing the PetriNet's code.
        /// </summary>
        void UnloadLibAndStopServer()
        {
            if(_debugServer != null) {
                _debugServer.Stop();
                _debugServer = null;
            }
            if(_libProxy != null) {
                _libProxy.Unload();
                _libProxy = null;
            }
        }

        /// <summary>
        /// Stops the petri net, detach the debugger and stops the DebugServer (it will not be listening any more after that).
        /// </summary>
        public void StopSession()
        {
            this.StopOrDetach(true);
        }

        /// <summary>
        /// Stops or detach the instance, depending on the parameter's value.
        /// </summary>
        /// <param name="stop">If set to <c>true</c> then the DebugServer is stopped as well as the DebugClient and petri net, whereas only the DebugClient and petri net are stopped if <c>false</c>.</param>
        void StopOrDetach(bool stop)
        {
            _pause = false;
            if(_sessionRunning) {
                if(PetriRunning) {
                    if(Pause) {
                        this.Pause = false;
                    }
                    StopPetri();
                    _petriRunning = false;
                }

                try {
                    if(stop) {
                        this.SendObject(new JObject(new JProperty("type", "exitSession")));
                    }
                    else {
                        this.SendObject(new JObject(new JProperty("type", "exit")));
                    }
                }
                catch(Exception) {
                }

                if(_receiverThread != null && !_receiverThread.Equals(Thread.CurrentThread)) {
                    _receiverThread.Join();
                }
                _sessionRunning = false;
            }

            lock(_document.DebugController.ActiveStates) {
                _document.DebugController.ActiveStates.Clear();
            }
        }

        /// <summary>
        /// Tells the DebugServer to run the petri net.
        /// </summary>
        public void StartPetri()
        {
            _pause = false;
            try {
                if(!_petriRunning) {
                    this.SendObject(new JObject(new JProperty("type", "start"),
                                                new JProperty("payload",
                                                              new JObject(new JProperty("hash",
                                                                                        _document.GetHash())))));
                }
            }
            catch(Exception e) {
                GLib.Timeout.Add(0, () => {
                    MessageDialog d = new MessageDialog(_document.Window,
                                                        DialogFlags.Modal,
                                                        MessageType.Question,
                                                        ButtonsType.None,
                                                        Application.SafeMarkupFromString(Configuration.GetLocalized("An error occurred in the debugger when starting the petri net:") + " " + e.Message));
                    d.AddButton(Configuration.GetLocalized("Cancel"), ResponseType.Cancel);
                    d.Run();
                    d.Destroy();

                    return false;
                });

                this.Detach();
            }
        }

        /// <summary>
        /// Tells the DebugServer to stop the execution of the petri net.
        /// </summary>
        public void StopPetri()
        {
            _pause = false;
            try {
                if(_petriRunning) {
                    this.SendObject(new JObject(new JProperty("type", "stop")));
                }
            }
            catch(Exception e) {
                GLib.Timeout.Add(0, () => {
                    MessageDialog d = new MessageDialog(_document.Window,
                                                        DialogFlags.Modal,
                                                        MessageType.Question,
                                                        ButtonsType.None,
                                                        Application.SafeMarkupFromString(Configuration.GetLocalized("An error occurred in the debugger when stopping the petri net:") + " " + e.Message));
                    d.AddButton(Configuration.GetLocalized("Cancel"), ResponseType.Cancel);
                    d.Run();
                    d.Destroy();

                    return false;
                });

                this.Detach();
            }
        }

        /// <summary>
        /// Stops the petri net execution, generate and compile the new petri net and load it into the DebugServer.
        /// </summary>
        public void ReloadPetri(bool startAfterReload = false)
        {
            GLib.Timeout.Add(0, () => {
                _document.Window.DebugGui.Status = Configuration.GetLocalized("Reloading the petri net…");
                return false;
            });
            GLib.Timeout.Add(1, () => {
                this.StopPetri();
                if(_document.Compile(true)) {
                    try {
                        _startAfterFix = startAfterReload;
                        this.SendObject(new JObject(new JProperty("type", "reload")));
                    }
                    catch(Exception e) {
                        GLib.Timeout.Add(0, () => {
                            MessageDialog d = new MessageDialog(_document.Window,
                                                                DialogFlags.Modal,
                                                                MessageType.Question,
                                                                ButtonsType.None,
                                                                Application.SafeMarkupFromString(Configuration.GetLocalized("An error occurred in the debugger when reloading the petri net:") + " " + e.Message));
                            d.AddButton(Configuration.GetLocalized("Cancel"), ResponseType.Cancel);
                            d.Run();
                            d.Destroy();

                            return false;
                        });
                        this.Detach();
                    }
                }

                return false;
            });
        }

        /// <summary>
        /// Sends the current breakpoints list to the DebugServer.
        /// </summary>
        public void UpdateBreakpoints()
        {
            if(PetriRunning) {
                var breakpoints = new JArray();
                foreach(var p in _document.DebugController.Breakpoints) {
                    breakpoints.Add(new JValue(p.ID));
                }
                this.SendObject(new JObject(new JProperty("type", "breakpoints"),
                                            new JProperty("payload",
                                                          breakpoints)));
            }
        }

        /// <summary>
        /// Triggers an asynchronous evaluation of a code expression.
        /// </summary>
        /// <param name="expression">The expression to evaluate.</param>
        /// <param name="userData">Additional and optional user data that will be used to generate the code.</param>
        public void Evaluate(Code.Expression expression, params object[] userData)
        {
            if(!PetriRunning) {
                var literals = expression.GetLiterals();
                foreach(var l in literals) {
                    if(l is Code.VariableExpression) {
                        throw new Exception(Configuration.GetLocalized("A variable of the petri net cannot be evaluated when the petri net is not running."));
                    }
                }
            }

            string sourceName = System.IO.Path.GetTempFileName();

            var petriGen = PetriGen.PetriGenFromLanguage(_document.Settings.Language, _document);
            petriGen.WriteExpressionEvaluator(expression, sourceName, userData);

            string libName = System.IO.Path.GetTempFileName();

            var c = new Compiler(_document);
            var o = c.CompileSource(sourceName, libName);
            if(o != "") {
                throw new Exception(Configuration.GetLocalized("Compilation error:") + " " + o);
            }
            else {
                if(_document.Settings.Language == Code.Language.CSharp) {
                    try {
                        if(_debugServer == null && expression.GetVariables().Count > 0) {
                            throw new Exception("Expressions containing variables can only evaluated when the petri net is running in the editor.");
                        }
                        var libProxy = new GeneratedDynamicLibProxy(_document.Settings.Language,
                                                                    System.IO.Directory.GetParent(libName).FullName,
                                                                    System.IO.Path.GetFileName(libName),
                                                                    _document.CodePrefix + "Evaluator");
                        var dylib = libProxy.Load<Runtime.Evaluator>();

                        if(dylib == null) {
                            throw new Exception("Unable to load the evaluator!");
                        }
                        string value = dylib.Evaluate(_debugServer.CurrentPetriNet);
                        libProxy.Unload();
                        GLib.Timeout.Add(0, () => {
                            _document.Window.DebugGui.OnEvaluate(value);
                            return false;
                        });
                    }
                    catch(Exception e) {
                        GLib.Timeout.Add(0, () => {
                            MessageDialog d = new MessageDialog(_document.Window,
                                                                DialogFlags.Modal,
                                                                MessageType.Question,
                                                                ButtonsType.None,
                                                                Application.SafeMarkupFromString(Configuration.GetLocalized("Evaluation error:") + " " + e.Message));
                            d.AddButton(Configuration.GetLocalized("Cancel"), ResponseType.Cancel);
                            d.Run();
                            d.Destroy();

                            return false;
                        });
                    }
                    if(libName != "") {
                        System.IO.File.Delete(libName);
                    }
                }
                else {
                    try {
                        this.SendObject(new JObject(new JProperty("type", "evaluate"),
                                                    new JProperty("payload",
                                                                  new JObject(new JProperty("lib",
                                                                                        libName),
                                                                          new JProperty("language",
                                                                                        _document.Settings.LanguageName())))));
                    }
                    catch(Exception e) {
                        this.Detach();
                        _document.Window.DebugGui.UpdateToolbar();
                        throw e;
                    }
                }
            }
            System.IO.File.Delete(sourceName);
        }

        /// <summary>
        /// Try to connect to a DebugServer.
        /// </summary>
        void Hello()
        {
            try {
                this.SendObject(new JObject(new JProperty("type", "hello"),
                                            new JProperty("payload",
                                                          new JObject(new JProperty("version",
                                                                                    Version)))));
		
                var ehlo = this.ReceiveObject();
                if(ehlo != null && ehlo["type"].ToString() == "ehlo") {
                    GLib.Timeout.Add(0, () => {
                        _document.Window.DebugGui.Status = Configuration.GetLocalized("Sucessfully connected.");
                        return false;
                    });
                    _document.Window.DebugGui.UpdateToolbar();
                    return;
                }
                else if(ehlo != null && ehlo["type"].ToString() == "error") {
                    throw new Exception(Configuration.GetLocalized("An error was returned by the debugger:") + " " + ehlo["payload"]);
                }
                throw new Exception(Configuration.GetLocalized("Invalid message received from debugger (expected ehlo).") + " " + ehlo);
            }
            catch(Exception e) {
                GLib.Timeout.Add(0, () => {
                    MessageDialog d = new MessageDialog(_document.Window,
                                                        DialogFlags.Modal,
                                                        MessageType.Question,
                                                        ButtonsType.None,
                                                        Application.SafeMarkupFromString(Configuration.GetLocalized("An error occurred in the debugger during the handshake:") + " " + e.Message));
                    d.AddButton(Configuration.GetLocalized("Cancel"), ResponseType.Cancel);
                    d.Run();
                    d.Destroy();

                    return false;
                });
                this.Detach();
                _document.Window.DebugGui.UpdateToolbar();
            }
        }

        /// <summary>
        /// The method in charge of receiving and dispatching the messages from the DebugServer.
        /// </summary>
        void Receiver()
        {
            try {
                _socket = new TcpClient();
                var result = _socket.BeginConnect(_document.Settings.Hostname,
                                                  _document.Settings.Port,
                                                  null,
                                                  null);
              
                if(!result.AsyncWaitHandle.WaitOne(1000)) {
                    throw new Exception("Timeout!");
                }

                _socket.EndConnect(result);
            }
            catch(Exception e) {
                this.Detach();

                GLib.Timeout.Add(0, () => {
                    MessageDialog d = new MessageDialog(_document.Window,
                                                        DialogFlags.Modal,
                                                        MessageType.Question,
                                                        ButtonsType.None,
                                                        Application.SafeMarkupFromString(Configuration.GetLocalized("Unable to connect to the server:") + " " + e.Message));
                    d.AddButton(Configuration.GetLocalized("Cancel"), ResponseType.Cancel);
                    d.Run();
                    d.Destroy();

                    return false;
                });
                return;
            }

            try {
                this.Hello();

                while(_sessionRunning && _socket.Connected) {
                    JObject msg = this.ReceiveObject();
                    if(msg == null)
                        break;

                    if(msg["type"].ToString() == "ack") {
                        if(msg["payload"].ToString() == "start") {
                            _petriRunning = true;
                            _document.Window.DebugGui.UpdateToolbar();
                            this.UpdateBreakpoints();
                            GLib.Timeout.Add(0, () => {
                                _document.Window.DebugGui.Status = Configuration.GetLocalized("The petri net is running…");
                                return false;
                            });
                        }
                        else if(msg["payload"].ToString() == "end_exec") {
                            StopPetri();
                        }
                        else if(msg["payload"].ToString() == "stop") {
                            _petriRunning = false;
                            _document.Window.DebugGui.UpdateToolbar();
                            lock(_document.DebugController.ActiveStates) {
                                _document.DebugController.ActiveStates.Clear();
                            }
                            _document.Window.DebugGui.View.Redraw();
                            GLib.Timeout.Add(0, () => {
                                _document.Window.DebugGui.Status = Configuration.GetLocalized("The petri net execution has ended.");
                                return false;
                            });
                        }
                        else if(msg["payload"].ToString() == "reload") {
                            _document.Window.DebugGui.UpdateToolbar();
                            GLib.Timeout.Add(0, () => {
                                _document.Window.DebugGui.Status = Configuration.GetLocalized("The Petri net has been successfully reloaded.");
                                if(_startAfterFix) {
                                    StartPetri();
                                }
                                return false;
                            });
                        }
                        else if(msg["payload"].ToString() == "pause") {
                            _pause = true;
                            _document.Window.DebugGui.UpdateToolbar();
                            GLib.Timeout.Add(0, () => {
                                _document.Window.DebugGui.Status = Configuration.GetLocalized("Paused.");
                                return false;
                            });
                        }
                        else if(msg["payload"].ToString() == "resume") {
                            _pause = false;
                            _document.Window.DebugGui.UpdateToolbar();
                            GLib.Timeout.Add(0, () => {
                                _document.Window.DebugGui.Status = Configuration.GetLocalized("The petri net is running…");
                                return false;
                            });
                        }
                    }
                    else if(msg["type"].ToString() == "error") {
                        _startAfterFix = false;
                        GLib.Timeout.Add(0, () => {
                            MessageDialog d = new MessageDialog(_document.Window,
                                                                DialogFlags.Modal,
                                                                MessageType.Question,
                                                                ButtonsType.None,
                                                                Application.SafeMarkupFromString(Configuration.GetLocalized("An error occurred in the debugger:") + " " + msg["payload"].ToString()));
                            d.AddButton(Configuration.GetLocalized("Cancel"), ResponseType.Cancel);
                            if(msg["payload"].ToString() == "You are trying to run a Petri net that is different from the one which is compiled!") {
                                d.AddButton(Configuration.GetLocalized("Fix and run"),
                                            ResponseType.Apply);
                            }
                            if((ResponseType)d.Run() == ResponseType.Apply) {
                                ReloadPetri(true);
                            }
                            d.Destroy();

                            return false;
                        });

                        if(_petriRunning) {
                            this.StopPetri();
                        }
                    }
                    else if(msg["type"].ToString() == "exit" || msg["type"].ToString() == "exitSession") {
                        if(msg["payload"].ToString() == "kbye") {
                            _sessionRunning = false;
                            _petriRunning = false;
                            _document.Window.DebugGui.UpdateToolbar();
                            GLib.Timeout.Add(0, () => {
                                _document.Window.DebugGui.Status = Configuration.GetLocalized("Disconnected.");
                                return false;
                            });
                        }
                        else {
                            _sessionRunning = false;
                            _petriRunning = false;

                            throw new Exception(Configuration.GetLocalized("Remote debugger requested a session termination for reason:") + " " + msg["payload"].ToString());
                        }
                    }
                    else if(msg["type"].ToString() == "states") {
                        var states = msg["payload"].Select(t => t).ToList();

                        lock(_document.DebugController.ActiveStates) {
                            _document.DebugController.ActiveStates.Clear();
                            foreach(var s in states) {
                                var id = UInt64.Parse(s["id"].ToString());
                                var e = _document.EntityFromID(id);
                                if(e == null || !(e is State)) {
                                    throw new Exception(Configuration.GetLocalized("Entity sent from runtime doesn't exist on our side! (id: {0})",
                                                                                   id));
                                }
                                _document.DebugController.ActiveStates[e as State] = int.Parse(s["count"].ToString());
                            }
                        }

                        _document.Window.DebugGui.View.Redraw();
                    }
                    else if(msg["type"].ToString() == "evaluation") {
                        var lib = msg["payload"]["lib"].ToString();
                        if(lib != "") {
                            System.IO.File.Delete(lib);
                        }
                        GLib.Timeout.Add(0, () => {
                            _document.Window.DebugGui.OnEvaluate(msg["payload"]["eval"].ToString());
                            return false;
                        });
                    }
                }
                if(_sessionRunning) {
                    throw new Exception(Configuration.GetLocalized("Socket unexpectedly disconnected"));
                }
            }
            catch(Exception e) {
                GLib.Timeout.Add(0, () => {
                    MessageDialog d = new MessageDialog(_document.Window,
                                                        DialogFlags.Modal,
                                                        MessageType.Question,
                                                        ButtonsType.None,
                                                        Application.SafeMarkupFromString(Configuration.GetLocalized("An error occurred in the debugger client:") + " " + e.Message));
                    d.AddButton(Configuration.GetLocalized("Cancel"), ResponseType.Cancel);
                    d.Run();
                    d.Destroy();

                    return false;
                });
                this.Detach();
            }

            try {
                _socket.Close();
            }
            catch(Exception) {
            }
            _document.Window.DebugGui.UpdateToolbar();
        }

        /// <summary>
        /// Tries to receive a JSON object from the DebugServer.
        /// </summary>
        /// <returns>The object.</returns>
        JObject ReceiveObject()
        {
            int count = 0;
            while(_sessionRunning) {
                string val = this.ReceiveString();

                if(val.Length > 0)
                    return JObject.Parse(val);

                if(++count > 5) {
                    throw new Exception(Configuration.GetLocalized("Remote debugger isn't available anymore!"));
                }
                Thread.Sleep(1);
            }

            return null;
        }

        /// <summary>
        /// Sends a JSON message to the DebugServer.
        /// </summary>
        /// <param name="o">O.</param>
        void SendObject(JObject o)
        {
            this.SendString(o.ToString());
        }

        /// <summary>
        /// Low level message reception routine.
        /// </summary>
        /// <returns>The string.</returns>
        string ReceiveString()
        {
            byte[] msg;

            lock(_downLock) {
                byte[] countBytes = new byte[4];

                int len = _socket.GetStream().Read(countBytes, 0, 4);
                if(len != 4)
                    return "";

                UInt32 count = (UInt32)countBytes[0] | ((UInt32)countBytes[1] << 8) | ((UInt32)countBytes[2] << 16) | ((UInt32)countBytes[3] << 24);
                UInt32 read = 0;

                msg = new byte[count];
                while(read < count) {
                    read += (UInt32)_socket.GetStream().Read(msg, (int)read, (int)(count - read));
                }
            }

            return System.Text.Encoding.UTF8.GetString(msg);
        }

        /// <summary>
        /// Low level message sending routine.
        /// </summary>
        /// <returns>The string.</returns>
        void SendString(string s)
        {
            var msg = System.Text.Encoding.UTF8.GetBytes(s);

            UInt32 count = (UInt32)msg.Length;

            byte[] bytes = new byte[4 + count];

            bytes[0] = (byte)((count >> 0) & 0xFF);
            bytes[1] = (byte)((count >> 8) & 0xFF);
            bytes[2] = (byte)((count >> 16) & 0xFF);
            bytes[3] = (byte)((count >> 24) & 0xFF);

            msg.CopyTo(bytes, 4);

            lock(_upLock) {
                _socket.GetStream().Write(bytes, 0, bytes.Length);
            }
        }

        GeneratedDynamicLibProxy _libProxy;
        Runtime.DebugServer _debugServer;
        Runtime.DynamicLib _dynamicLib;

        bool _startAfterFix = false;
        bool _petriRunning, _pause;
        volatile bool _sessionRunning;
        Thread _receiverThread;

        volatile TcpClient _socket;
        object _upLock = new object();
        object _downLock = new object();

        Document _document;
    }
}

