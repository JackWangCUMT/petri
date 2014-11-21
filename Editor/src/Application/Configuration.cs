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
				return Configuration.platform;
			}
		}	

		public static Configuration Get() {
			if(Configuration.instance == null) {
				try {
					switch(Environment.OSVersion.Platform) {
						case PlatformID.Unix:
							if (Directory.Exists("/Applications")
								& Directory.Exists("/System")
								& Directory.Exists("/Users")
								& Directory.Exists("/Volumes"))
								Configuration.platform = Platform.Mac;
							else
								Configuration.platform = Platform.Linux;
							break;
						case PlatformID.MacOSX:
							Configuration.platform = Platform.Mac;
							break;
						default:
							Configuration.platform = Platform.Windows;
							break;
					}

					// Get the current configuration file.
					Configuration.config = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
				}
				catch(System.Xml.XmlException) {
					string s = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
					System.IO.File.Delete(s);
					System.Console.WriteLine("Corrupted settings file, deleting…");
					return Configuration.Get();
				}

				try {
					Configuration.instance = Configuration.config.Sections["CustomSection"] as Configuration;
				}
				catch(System.Configuration.ConfigurationErrorsException err) {
					Console.WriteLine("CreateConfigurationFile: {0}", err.ToString());
				}
				if(Configuration.instance == null) {
					Configuration.instance = new Configuration();
					Configuration.config.Sections.Add("CustomSection", Configuration.instance);
				}
			}

			return Configuration.instance;
		}

		public static void Save() {
			if(Configuration.instance != null) {
				Configuration.config.Save(ConfigurationSaveMode.Full, true);
			}
		}

		[ConfigurationProperty("exportPath",
			DefaultValue = "",
			IsRequired = true)]
		public static string ExportPath
		{
			get
			{
				return (string)Get()["exportPath"];
			}
			set
			{
				Get()["exportPath"] = value;
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

		public Configuration() {}

		public static string GetRelativePath(string filespec, string folder)
		{
			Uri pathUri = new Uri(filespec);
			// Folders must end in a slash
			if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				folder += Path.DirectorySeparatorChar;
			}
			Uri folderUri = new Uri(folder);
			return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
		}

					
		private static System.Configuration.Configuration config = null;
		private static Configuration instance = null;
		private static Platform platform;
	}
}

