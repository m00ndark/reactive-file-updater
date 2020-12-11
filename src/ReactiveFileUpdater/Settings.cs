using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ReactiveFileUpdater.Model;
using ToolComponents.Core.Logging;

namespace ReactiveFileUpdater
{
	public class Settings
	{
		private const string SETTINGS_FOLDER_NAME = Program.APPLICATION_NAME;
		private const string SETTINGS_FILE_NAME = "settings.json";

		public List<FileUpdate> FileUpdates { get; set; } = new List<FileUpdate>();
		public TimeSpan PollFrequency { get; set; } = TimeSpan.FromSeconds(2);

		// ---------------------------------------------------------------------

		private static FileSystemWatcher _watcher;
		private static DateTime _lastWatcherEventTime;
		private static bool _skipWatcherEvents;
		private static Settings _settings;
		private static readonly object _lock = new object();
		private static readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), SETTINGS_FOLDER_NAME);
		private static readonly string _filePath = Path.Combine(_path, SETTINGS_FILE_NAME);

		private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
			{
				DefaultValueHandling = DefaultValueHandling.Ignore,
				Converters = new JsonConverter[] { new StringEnumConverter() }
			};

		public static event EventHandler Changed;

		public static Settings Default
		{
			get
			{
				lock (_lock)
				{
					return _settings ?? (_settings = Load());
				}
			}
		}

		public static bool Exist => File.Exists(_filePath);

		private static void Reset()
		{
			lock (_lock)
			{
				_settings = null;
			}
		}

		private static Settings Load()
		{
			Logger.Add($"Loading configuration from {_filePath}");
			string json = Exist ? File.ReadAllText(_filePath, Encoding.UTF8) : null;
			return Deserialize(string.IsNullOrWhiteSpace(json) ? "{}" : json).PostProcess();
		}

		private Settings PostProcess()
		{
			if (PollFrequency < TimeSpan.FromSeconds(1))
				PollFrequency = TimeSpan.FromSeconds(1);

			return this;
		}

		public void Save()
		{
			Directory.CreateDirectory(_path);

			if (Exist && !Backup()) return;

			bool watcherIsRaisingEvent = _watcher?.EnableRaisingEvents ?? false;

			try
			{
				if (watcherIsRaisingEvent)
					_watcher.EnableRaisingEvents = false;

				File.WriteAllText(_filePath, Serialize(this), Encoding.UTF8);
			}
			catch
			{
				Restore();
			}
			finally
			{
				if (watcherIsRaisingEvent)
					_watcher.EnableRaisingEvents = true;
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

		public static void EnableChangeNotifications()
		{
			if (_watcher != null) return;

			_lastWatcherEventTime = DateTime.MinValue;
			_watcher = new FileSystemWatcher(_path, SETTINGS_FILE_NAME);
			_watcher.Created += Watcher_Event;
			_watcher.Deleted += Watcher_Event;
			_watcher.Changed += Watcher_Event;
			_watcher.EnableRaisingEvents = true;
		}

		public static void DisableChangeNotifications()
		{
			if (_watcher == null) return;

			_watcher.EnableRaisingEvents = false;
			_watcher.Created -= Watcher_Event;
			_watcher.Deleted -= Watcher_Event;
			_watcher.Changed -= Watcher_Event;
			_watcher = null;
		}

		private static void Watcher_Event(object sender, FileSystemEventArgs e)
		{
			Thread.Sleep(100);

			DateTime now = DateTime.Now;
			if (now - TimeSpan.FromMilliseconds(200) < _lastWatcherEventTime) return;

			_lastWatcherEventTime = now;

			if (_skipWatcherEvents) return;

			Reset();
			Changed?.Invoke(null, EventArgs.Empty);
		}

		private static Settings Deserialize(string json) => JsonConvert.DeserializeObject<Settings>(json, _serializerSettings);

		private static string Serialize(Settings settings) => JsonConvert.SerializeObject(settings, Formatting.Indented, _serializerSettings);
	}
}
