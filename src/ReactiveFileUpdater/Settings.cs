using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ReactiveFileUpdater.Model;

namespace ReactiveFileUpdater
{
	public class Settings
	{
		private const string SETTINGS_FOLDER_NAME = Program.APPLICATION_NAME;
		private const string SETTINGS_FILE_NAME = "settings.json";

		public List<FileUpdate> FileUpdates { get; set; } = new List<FileUpdate>();

		// ---------------------------------------------------------------------

		private static Settings _settings;
		private static readonly object _lock = new object();
		private static readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), SETTINGS_FOLDER_NAME);
		private static readonly string _filePath = Path.Combine(_path, SETTINGS_FILE_NAME);

		private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
			{
				DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
				Converters = new JsonConverter[] { new StringEnumConverter() }
			};

		public static Settings Default
		{
			get
			{
				if (_settings != null)
					return _settings;

				lock (_lock)
				{
					return _settings ?? (_settings = Load());
				}
			}
		}

		public static bool Exist => File.Exists(_filePath);

		private static Settings Load()
		{
			string json = Exist ? File.ReadAllText(_filePath, Encoding.UTF8) : null;
			return Deserialize(string.IsNullOrWhiteSpace(json) ? "{}" : json).PostProcess();
		}

		private Settings PostProcess()
		{
			// add post processing here

			return this;
		}

		public void Save()
		{
			Directory.CreateDirectory(_path);

			if (Exist && !Backup()) return;

			try
			{
				File.WriteAllText(_filePath, Serialize(this), Encoding.UTF8);
			}
			catch
			{
				Restore();
			}
		}

		private static bool Backup()
		{
			try
			{
				File.Copy(_filePath, _filePath + ".bak", true);
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static bool Restore()
		{
			try
			{
				File.Copy(_filePath + ".bak", _filePath, true);
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static Settings Deserialize(string json) => JsonConvert.DeserializeObject<Settings>(json, _serializerSettings);

		private static string Serialize(Settings settings) => JsonConvert.SerializeObject(settings, Formatting.Indented, _serializerSettings);
	}
}
