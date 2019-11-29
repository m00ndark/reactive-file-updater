using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ReactiveFileUpdater.Model
{
	public class TargetFile
	{
		public TargetFile(string filePath, List<FileUpdate> fileUpdates)
		{
			FilePath = filePath;
			Path = System.IO.Path.GetDirectoryName(filePath);
			FileName = System.IO.Path.GetFileName(filePath);
			FileUpdates = fileUpdates;
		}

		[JsonIgnore]
		public string Id { get; } = Guid.NewGuid().ToString();

		public string FilePath { get; }
		public string Path { get; }
		public string FileName { get; }
		public bool IsValid => Path != null && FileName != null;
		public List<FileUpdate> FileUpdates { get; }
		public long FileSize { get; set; } = -1;
		public DateTime ModifiedTime { get; set; } = DateTime.MinValue;
		public FileSystemWatcher Watcher { get; set; }
	}
}
