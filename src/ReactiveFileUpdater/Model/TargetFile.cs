using System;
using System.Collections.Generic;
using System.IO;

namespace ReactiveFileUpdater.Model
{
	public class TargetFile
	{
		public TargetFile(string filePath, List<FileUpdate> fileUpdates)
		{
			FilePath = filePath;
			FileUpdates = fileUpdates;
		}

		public string FilePath { get; }
		public List<FileUpdate> FileUpdates { get; }
		public long FileSize { get; set; }
		public DateTime ModifiedTime { get; set; }
		public FileSystemWatcher Watcher { get; set; }
	}
}
