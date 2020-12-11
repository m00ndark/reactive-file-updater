using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ReactiveFileUpdater.Model
{
	public class TargetFile
	{
		public event EventHandler WatcherEvent;

		public TargetFile(string filePath, List<FileUpdate> fileUpdates)
		{
			FilePath = filePath;
			Path = System.IO.Path.GetDirectoryName(filePath);
			FileName = System.IO.Path.GetFileName(filePath);
			FileUpdates = fileUpdates;
		}

		public string Id { get; } = Guid.NewGuid().ToString();
		public string FilePath { get; }
		public string Path { get; }
		public string FileName { get; }
		public bool IsValid => Path != null && FileName != null;
		public List<FileUpdate> FileUpdates { get; }
		public long FileSize { get; set; } = -1;
		public DateTime ModifiedTime { get; set; } = DateTime.MinValue;
		public FileSystemWatcher Watcher { get; private set; }
		public bool IsWatching => Watcher != null;

		public void EnableWatcher()
		{
			if (IsWatching) return;

			Watcher = new FileSystemWatcher(Path, FileName);
			Watcher.Created += Watcher_Event;
			Watcher.Deleted += Watcher_Event;
			Watcher.Changed += Watcher_Event;
			Watcher.EnableRaisingEvents = true;
		}

		public void DisableWatcher()
		{
			if (!IsWatching) return;

			Watcher.EnableRaisingEvents = false;
			Watcher.Created -= Watcher_Event;
			Watcher.Deleted -= Watcher_Event;
			Watcher.Changed -= Watcher_Event;
			Watcher = null;
		}

		public void EnableWatcherEvents()
		{
			if (IsWatching)
				Watcher.EnableRaisingEvents = true;
		}

		public void DisableWatcherEvents()
		{
			if (IsWatching)
				Watcher.EnableRaisingEvents = false;
		}

		private void Watcher_Event(object sender, FileSystemEventArgs e)
		{
			WatcherEvent?.Invoke(this, EventArgs.Empty);
		}
	}
}
