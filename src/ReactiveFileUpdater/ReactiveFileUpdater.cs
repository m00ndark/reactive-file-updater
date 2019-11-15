using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ReactiveFileUpdater.Model;
using ToolComponents.Core.Comparers;
using ToolComponents.Core.Logging;

namespace ReactiveFileUpdater
{
	public class ReactiveFileUpdater
	{
		private static readonly object _lock = new object();
		private static volatile ReactiveFileUpdater _instance;

		private Thread _mainThread;
		private volatile bool _shutDownRequested;
		private List<TargetFile> _targetFiles;

		public static ReactiveFileUpdater Instance
		{
			get
			{
				if (_instance != null)
					return _instance;

				lock (_lock)
				{
					return _instance ?? (_instance = new ReactiveFileUpdater());
				}
			}
		}

		public bool IsRunning => _mainThread != null;

		public void Start()
		{
			if (IsRunning)
				return;

			_shutDownRequested = false;
			_mainThread = new Thread(MainThread);
			_mainThread.Start();
		}

		public void ShutDown()
		{
			if (IsRunning)
				_shutDownRequested = true;
		}

		private void MainThread()
		{
			try
			{
				Logger.Add("Initializing...");

				_targetFiles = Settings.Default.FileUpdates
					.GroupBy(fileUpdate => fileUpdate.FilePath, StringComparer.InvariantCultureIgnoreCase)
					.Select(fileUpdates => new TargetFile(fileUpdates.Key, fileUpdates.ToList()))
					.ToList();

				foreach (TargetFile targetFile in _targetFiles)
				{
					FileInfo fileInfo = new FileInfo(targetFile.FilePath);
					SetTargetFileProperties(targetFile, fileInfo);

					string path = Path.GetDirectoryName(targetFile.FilePath);

					if (path == null) continue;

					targetFile.Watcher = new FileSystemWatcher(path, Path.GetFileName(targetFile.FilePath));
					targetFile.Watcher.Created += (_, e) => UpdateFile(targetFile);
					targetFile.Watcher.Deleted += (_, e) => UpdateFile(targetFile);
					targetFile.Watcher.Changed += (_, e) => UpdateFile(targetFile);
					targetFile.Watcher.EnableRaisingEvents = true;
				}

				try
				{
					Logger.Add("Entering main loop...");

					while (!_shutDownRequested)
					{
						foreach (TargetFile targetFile in _targetFiles)
						{
							FileInfo fileInfo = new FileInfo(targetFile.FilePath);

							if (!fileInfo.Exists)
								continue;

							if (fileInfo.Length != targetFile.FileSize || fileInfo.LastWriteTime != targetFile.ModifiedTime)
							{
								//BlockingCollection<TargetFile>

								lock (targetFile)
								{
									SetTargetFileProperties(targetFile, fileInfo);
									Task.Run(() => UpdateFile(targetFile));
								}
							}
						}

						Thread.Sleep(TimeSpan.FromSeconds(1));
					}

					Logger.Add("Exiting main loop...");
				}
				catch (Exception ex)
				{
					Logger.Add("Fatal exception in main loop, dying....", ex);
				}

				Logger.Add("Shut down completed, signing off");
			}
			catch (Exception ex)
			{
				Logger.Add("Fatal exception in main thread, dying...", ex);
			}
			finally
			{
				// indicate that we are no longer running
				_mainThread = null;
			}
		}

		private void SetTargetFileProperties(TargetFile targetFile, FileInfo fileInfo)
		{
			targetFile.FileSize = fileInfo.Exists ? fileInfo.Length : -1;
			targetFile.ModifiedTime = fileInfo.Exists ? fileInfo.LastWriteTime : DateTime.MinValue;
		}

		private void UpdateFile(TargetFile targetFile)
		{
		}
	}
}
