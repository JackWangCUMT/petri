using System;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using System.Linq;
using Gtk;

namespace Petri
{
	public class DebugServer {
		public DebugServer(Document doc) {
			document = doc;
			sessionRunning = false;
			petriRunning = false;
			pause = false;
		}

		~DebugServer() {
			if(petriRunning || sessionRunning) {
				throw new Exception("Debugger still running!");
			}
		}

		public bool SessionRunning {
			get {
				return sessionRunning;
			}
		}

		public bool PetriRunning {
			get {
				return petriRunning;
			}
		}

		public bool Pause {
			get {
				return pause;
			}
			set {
				if(PetriRunning) {
					try {
						if(value) {
							this.sendObject(new JObject(new JProperty("type", "pause")));
						}
						else {
							this.sendObject(new JObject(new JProperty("type", "resume")));
						}
					}
					catch(Exception) {}
				}
				else {
					pause = false;
					document.Window.DebugGui.UpdateToolbar();
				}
			}
		}

		public string Version {
			get {
				return "0.1";
			}
		}

		public void StartSession() {
			sessionRunning = true;
			receiverThread = new Thread(this.receiver);
			pause = false;
			receiverThread.Start();
			while(socket == null);
		}

		public void StopSession() {
			pause = false;
			if(sessionRunning) {
				if(PetriRunning) {
					if(Pause) {
						this.Pause = false;
					}
					StopPetri();
					petriRunning = false;
				}

				try {
					this.sendObject(new JObject(new JProperty("type", "exit")));
				}
				catch(Exception) {}

				if(receiverThread != null)
					receiverThread.Join();
				sessionRunning = false;
			}
		}

		public void StartPetri() {
			pause = false;
			try {
				if(!petriRunning)
					this.sendObject(new JObject(new JProperty("type", "start"), new JProperty("payload", new JObject(new JProperty("hash", document.GetHash())))));
			}
			catch(Exception) {
				// TODO: present error
				this.StopSession();
			}
		}

		public void StopPetri() {
			pause = false;
			try {
				if(petriRunning)
					this.sendObject(new JObject(new JProperty("type", "stop")));
			}
			catch(Exception) {
				// TODO: present error
				this.StopSession();
			}
		}

		public void ReloadPetri() {
			this.StopPetri();
			document.SaveCppDontAsk();
			if(!document.Compile()) {

			}
			else {
				try {
					this.sendObject(new JObject(new JProperty("type", "reload")));
				}
				catch(Exception) {
					// TODO: present error
					this.StopSession();
				}
			}
		}

		public void UpdateBreakpoints() {
			var breakpoints = new JArray();
			foreach(var p in document.DebugController.Breakpoints) {
				breakpoints.Add(new JValue(p.ID));
			}
			this.sendObject(new JObject(new JProperty("type", "breakpoints"), new JProperty("payload", breakpoints)));
		}

		private void Hello() {
			try {
				this.sendObject(new JObject(new JProperty("type", "hello"), new JProperty("payload", new JObject(new JProperty("version", Version)))));
		
				var ehlo = this.receiveObject();
				if(ehlo != null && ehlo["type"].ToString() == "ehlo") {
					document.Window.DebugGui.UpdateToolbar();
					return;
				}
				else if(ehlo != null && ehlo["type"].ToString() == "error") {
					throw new Exception("An error was returned by the debugger: " + ehlo["error"]);
				}
				throw new Exception("Invalid message received from debugger (expected ehlo)");
			}
			catch(Exception e) {
				Console.WriteLine("Couldn't connect to C++ debugger: " + e);
				sessionRunning = false;
				document.Window.DebugGui.UpdateToolbar();
			}
		}

		private void receiver() {
			socket = new TcpClient(document.Settings.Hostname, document.Settings.Port);
			this.Hello();

			try {
				while(sessionRunning && socket.Connected) {
					JObject msg = this.receiveObject();
					if(msg == null)
						break;

					if(msg["type"].ToString() == "ack") {
						if(msg["payload"].ToString() == "start") {
							petriRunning = true;
							document.Window.DebugGui.UpdateToolbar();
						}
						else if(msg["payload"].ToString() == "stop") {
							petriRunning = false;
							document.Window.DebugGui.UpdateToolbar();
							lock(document.DebugController.ActiveStates) {
								document.DebugController.ActiveStates.Clear();
							}
							document.Window.DebugGui.View.Redraw();
						}
						else if(msg["payload"].ToString() == "reload") {
							document.Window.DebugGui.UpdateToolbar();
						}
						else if(msg["payload"].ToString() == "pause") {
							pause = true;
							document.Window.DebugGui.UpdateToolbar();
						}
						else if(msg["payload"].ToString() == "resume") {
							pause = false;
							document.Window.DebugGui.UpdateToolbar();
						}
					}
					else if(msg["type"].ToString() == "error") {
						GLib.Timeout.Add(0, () => {
							MessageDialog d = new MessageDialog(document.Window, DialogFlags.Modal, MessageType.Question, ButtonsType.None, MainClass.SafeMarkupFromString("Une erreur est survenue dans le débuggueur : " + msg["payload"].ToString()));
							d.AddButton("Annuler", ResponseType.Cancel);
							d.Run();
							d.Destroy();

							return false;
						});

						if(petriRunning) {
							this.StopPetri();
						}
					}
					else if(msg["type"].ToString() == "exit") {
						if(msg["payload"].ToString() == "kbye") {
							sessionRunning = false;
							petriRunning = false;
							document.Window.DebugGui.UpdateToolbar();
						}
						else {
							sessionRunning = false;
							petriRunning = false;

							throw new Exception("Remote debugger requested a session termination for reason: " + msg["payload"].ToString());
						}
					}
					else if(msg["type"].ToString() == "states") {
						var states = msg["payload"].Select(t => t).ToList();

						lock(document.DebugController.ActiveStates) {
							document.DebugController.ActiveStates.Clear();
							foreach(var s in states) {
								var id = UInt64.Parse(s["id"].ToString());
								var e = document.EntityFromID(id);
								if(e == null || !(e is State)) {
									throw new Exception("Entity sent from runtime doesn't exist on our side! (id: " + id + ")");
								}
								document.DebugController.ActiveStates[e as State] = int.Parse(s["count"].ToString());
							}
						}

						document.Window.DebugGui.View.Redraw();
					}
				}
				if(sessionRunning) {
					throw new Exception("Socket unexpectedly disconnected");
				}
			}
			catch(Exception e) {
				Console.WriteLine(e);
				Console.WriteLine("Exception in the debugger, exiting session");
				this.StopSession();
			}

			try {
				socket.Close();
			}
			catch(Exception) {}
			document.Window.DebugGui.UpdateToolbar();
		}

		private JObject receiveObject() {
			int count = 0;
			while(sessionRunning) {
				string val = this.receiveString();
				if(val.Length > 0)
					return JObject.Parse(val);

				if(++count > 5) {
					throw new Exception("Remote debugger not available anymore!");
				}
				Thread.Sleep(1);
			}

			return null;
		}

		private void sendObject(JObject o) {
			this.sendString(o.ToString());
		}

		private string receiveString() {
			byte[] msg;

			lock(downLock) {
				byte[] countBytes = new byte[4];

				int len =  socket.GetStream().Read(countBytes, 0, 4);
				if(len != 4)
					return "";

				UInt32 count = (UInt32)countBytes[0] | ((UInt32)countBytes[1] << 8) | ((UInt32)countBytes[2] << 16) | ((UInt32)countBytes[3] << 24);
				UInt32 read = 0;

				msg = new byte[count];
				while(read < count) {
					read += (UInt32)socket.GetStream().Read(msg, (int)read, (int)(count - read));
				}
			}

			return System.Text.Encoding.UTF8.GetString(msg);
		}

		private void sendString(string s) {
			var msg = System.Text.Encoding.UTF8.GetBytes(s);

			UInt32 count = (UInt32)msg.Length;

			byte[] bytes = new byte[4 + count];

			bytes[0] = (byte)((count >> 0) & 0xFF);
			bytes[1] = (byte)((count >> 8) & 0xFF);
			bytes[2] = (byte)((count >> 16) & 0xFF);
			bytes[3] = (byte)((count >> 24) & 0xFF);

			msg.CopyTo(bytes, 4);

			lock(upLock) {
				socket.GetStream().Write(bytes, 0, bytes.Length);
			}
		}

		bool petriRunning, pause;
		volatile bool sessionRunning;
		Thread receiverThread;

		volatile TcpClient socket;
		object upLock = new object();
		object downLock = new object();

		Document document;
	}
}

