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
using System.Collections.Generic;
using System.Xml.Linq;
using System.Reflection;
using System.Linq;

namespace Petri.Editor
{
    public enum Platform
    {
        Windows,
        Linux,
        Mac,
    }

    public class Configuration : ConfigurationSection
    {
        /// <summary>
        /// Gets the running platform.
        /// </summary>
        /// <value>The running platform.</value>
        public static Platform RunningPlatform {
            get {
                Configuration.Get();
                return Configuration._platform;
            }
        }

        /// <summary>
        /// Gets the configuration instance.
        /// </summary>
        public static Configuration Get()
        {
            if(Configuration._instance == null) {
                try {
                    switch(Environment.OSVersion.Platform) {
                    case PlatformID.Unix:
                        if(Directory.Exists("/Applications") && Directory.Exists("/System") && Directory.Exists("/Users") && Directory.Exists("/Volumes")) {
                            Configuration._platform = Platform.Mac;
                        }
                        else {
                            Configuration._platform = Platform.Linux;
                        }
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
                    System.Console.Error.WriteLine("Corrupted settings file, deleting…");
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
                    catch(System.Exception) {
                    }
                    Console.Error.WriteLine("CreateConfigurationFile: {0}", err.Message);
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

        /// <summary>
        /// Save the configuration.
        /// </summary>
        public static void Save()
        {
            if(Configuration._instance != null) {
                Configuration._config.Save(ConfigurationSaveMode.Full, true);
            }
        }

        /// <summary>
        /// Gets the GUI language.
        /// </summary>
        /// <value>The language.</value>
        public static string Language {
            get {
                return System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            }
        }

        /// <summary>
        /// Gets the localized value of the given key, accordind to the current GUI language.
        /// </summary>
        /// <returns>The localized.</returns>
        /// <param name="key">The key.</param>
        public static string GetLocalized(string key)
        {
            if(Configuration._localizedStrings.Count == 0) {
                Configuration.Get().LoadLanguage();
            }
            if(!Configuration._localizedStrings.ContainsKey(key)) {
                Console.Error.WriteLine("No localization found for locale \"" + Language + "\" and key \"" + key + "\"");
                return key;
            }
            else {
                return Configuration._localizedStrings[key];
            }
        }

        /// <summary>
        /// Gets the localized value for the given key, and formats it withe the given arguments.
        /// </summary>
        /// <returns>The localized.</returns>
        /// <param name="value">Value.</param>
        /// <param name="args">Arguments.</param>
        public static string GetLocalized(string key, params object[] args)
        {
            return string.Format(GetLocalized(key), args);
        }
            
        [ConfigurationProperty("savePath",
                               DefaultValue = "",
                               IsRequired = true)]
        /// <summary>
        /// Gets or sets the path used for the last document save operation.
        /// </summary>
        /// <value>The save path.</value>
        public static string SavePath {
            get {
                return (string)Get()["savePath"];
            }
            set {
                Get()["savePath"] = value;
            }
        }

        [ConfigurationProperty("recentDocuments",
                               DefaultValue = "",
                               IsRequired = false)]
        /// <summary>
        /// Gets or sets the recent documents list, as a JSON-serialized array of paths and access time.
        /// </summary>
        /// <value>The recent documents.</value>
        public static string RecentDocuments {
            get {
                return (string)Get()["recentDocuments"];
            }
            set {
                Get()["recentDocuments"] = value;
            }
        }

        [ConfigurationProperty("arch",
                               DefaultValue = 0,
                               IsRequired = true)]
        /// <summary>
        /// Gets or sets the build architecture (32 or 64).
        /// </summary>
        /// <value>The arch.</value>
        public static int Arch {
            get {
                return (int)Get()["arch"];
            }
            set {
                Get()["arch"] = value;
            }
        }

        /// <summary>
        /// Gets the path of <paramref name="file"/> relative to <paramref name="folder"/>.
        /// </summary>
        /// <returns>The relative path.</returns>
        /// <param name="file">File.</param>
        /// <param name="folder">Folder.</param>
        public static string GetRelativePath(string file, string folder)
        {
            Uri pathUri = new Uri(file);
            if(!folder.EndsWith(Path.DirectorySeparatorChar.ToString())) {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/',
                                                                                                Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Loads the current language's resource file.
        /// </summary>
        private void LoadLanguage()
        {
            string resource;
            string noloc;
            if(Language == "fr") {
                resource = "fr.lang";
                noloc = "<Traduction non disponible>";
            }
            else if(Language == "en") {
                resource = "en.lang";
                noloc = "<Localization unavailable>";
            }
            else {
                Console.Error.WriteLine("Language \"" + Language + "\" not supported, defaulting to English locale.");
                resource = "en.lang";
                noloc = "<Localization unavailable>";
            }

            _localizedStrings.Add("__noloc__", noloc);

            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream objStream = assembly.GetManifestResourceStream(resource);
            var document = XDocument.Load(new StreamReader(objStream));
            var root = document.FirstNode as XElement;
            string key = null;
            foreach(var element in root.Descendants()) {
                if(key == null) {
                    key = element.Value;
                }
                else {
                    if(_localizedStrings.ContainsKey(key)) {
                        Console.Error.WriteLine("Warning: the key \"" + key + "\" is already present for the locale \"" + Language + "\".");
                    }
                    else {
                        _localizedStrings.Add(key, element.Value);
                    }
                    key = null;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.Configuration"/> class.
        /// Must be kept public, otherwise a Save operation will result in an exception.
        /// </summary>
        public Configuration()
        {
        }

        private static System.Configuration.Configuration _config = null;
        private static Configuration _instance = null;
        private static Platform _platform;
        private static Dictionary<string, string> _localizedStrings = new Dictionary<string, string>();
    }
}

