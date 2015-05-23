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
using System.Configuration;
using System.Text;
using System.IO;

namespace Petri
{
	public enum Platform
	{
		Windows,
		Linux,
		Mac,
	}

	public class Configuration : ConfigurationSection {
		public static Platform RunningPlatform {
			get {
				Configuration.Get();
				return Configuration._platform;
			}
		}	

		public static Configuration Get() {
			if(Configuration._instance == null) {
				try {
					switch(Environment.OSVersion.Platform) {
						case PlatformID.Unix:
							if (Directory.Exists("/Applications")
								& Directory.Exists("/System")
								& Directory.Exists("/Users")
								& Directory.Exists("/Volumes"))
								Configuration._platform = Platform.Mac;
							else
								Configuration._platform = Platform.Linux;
							break;
						case PlatformID.MacOSX:
							Configuration._platform = Platform.Mac;
							break;
						default:
							Configuration._platform = Platform.Windows;
							break;
					}

					// Get the current configuration file.
					Configuration._config = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
				}
				catch(System.Xml.XmlException) {
					string s = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
					System.IO.File.Delete(s);
					System.Console.WriteLine("Corrupted settings file, deleting…");
					return Configuration.Get();
				}

				try {
					Configuration._instance = Configuration._config.Sections["CustomSection"] as Configuration;
				}
				catch(System.Configuration.ConfigurationErrorsException err) {
					try {
						string s = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
						System.IO.File.Delete(s);
					}
					catch(System.Exception) {}
					Console.WriteLine("CreateConfigurationFile: {0}", err.Message);
					return Configuration.Get();
				}
				if(Configuration._instance == null) {
					Configuration._instance = new Configuration();
					Configuration._config.Sections.Remove("CustomSection");
					Configuration._config.Sections.Add("CustomSection", Configuration._instance);
				}
			}

			return Configuration._instance;
		}

		public static void Save() {
			if(Configuration._instance != null) {
				Configuration._config.Save(ConfigurationSaveMode.Full, true);
			}
		}

		[ConfigurationProperty("savePath",
			DefaultValue = "",
			IsRequired = true)]
		public static string SavePath
		{
			get
			{
				return (string)Get()["savePath"];
			}
			set
			{
				Get()["savePath"] = value;
			}
		}

		[ConfigurationProperty("windowWidth",
			DefaultValue = 800,
			IsRequired = true)]
		public static int WindowWidth {
			get {
				return (int)Get()["windowWidth"];
			}
			set {
				Get()["windowWidth"] = value;
			}
		}

		[ConfigurationProperty("windowHeight",
			DefaultValue = 600,
			IsRequired = true)]
		public static int WindowHeight {
			get {
				return (int)Get()["windowHeight"];
			}
			set {
				Get()["windowHeight"] = value;
			}
		}

		[ConfigurationProperty("windowX",
			DefaultValue = 80,
			IsRequired = true)]
		public static int WindowX {
			get {
				return (int)Get()["windowX"];
			}
			set {
				Get()["windowX"] = value;
			}
		}

		[ConfigurationProperty("windowY",
			DefaultValue = 80,
			IsRequired = true)]
		public static int WindowY {
			get {
				return (int)Get()["windowY"];
			}
			set {
				Get()["windowY"] = value;
			}
		}

		[ConfigurationProperty("graphWidth",
			DefaultValue = 640,
			IsRequired = true)]
		public static int GraphWidth {
			get {
				return (int)Get()["graphWidth"];
			}
			set {
				Get()["graphWidth"] = value;
			}
		}

		[ConfigurationProperty("arch",
			DefaultValue = 0,
			IsRequired = true)]
		public static int Arch {
			get {
				return (int)Get()["arch"];
			}
			set {
				Get()["arch"] = value;
			}
		}

		public static string GetRelativePath(string file, string folder) {
			Uri pathUri = new Uri(file);
			if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString())) {
				folder += Path.DirectorySeparatorChar;
			}
			Uri folderUri = new Uri(folder);
			return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
		}

					
		public Configuration() {}
		private static System.Configuration.Configuration _config = null;
		private static Configuration _instance = null;
		private static Platform _platform;
	}
}

