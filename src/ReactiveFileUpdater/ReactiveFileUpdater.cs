using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using ReactiveFileUpdater.Model;
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
		private BlockingCollection<UpdateQueueItem> _updateQueue;
		private CancellationTokenSource _cancellationTokenSource;

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

			Logger.Add("Starting up..");

			_shutDownRequested = false;
			_mainThread = new Thread(MainThread);
			_mainThread.Start();
		}

		public void ShutDown()
		{
			if (!IsRunning)
				return;

			Logger.Add("Shutting down..");
			_shutDownRequested = true;
		}

		private void MainThread()
		{
			try
			{
				Logger.Add("Initializing..");

				if (!(Settings.Default.FileUpdates?.Any() ?? false))
				{
					Logger.Add("No or empty configuration, shutting down..");
					return;
				}

				_updateQueue = new BlockingCollection<UpdateQueueItem>();
				_cancellationTokenSource = new CancellationTokenSource();

				_targetFiles = Settings.Default.FileUpdates
					.GroupBy(fileUpdate => fileUpdate.FilePath, StringComparer.InvariantCultureIgnoreCase)
					.Select(fileUpdates => new TargetFile(fileUpdates.Key, fileUpdates.ToList()))
					.Where(targetFile => targetFile.IsValid)
					.ToList();

				Logger.Add($"Loaded {_targetFiles.Count} valid file updates");
				Logger.Add($"Poll frequency is {Settings.Default.PollFrequency}");

				foreach (TargetFile targetFile in _targetFiles)
				{
					Logger.Add($"[{targetFile.Id}] Watching \"{targetFile.FilePath}\"");

					foreach (FileUpdate fileUpdate in targetFile.FileUpdates)
					{
						Logger.Add($"[{targetFile.Id}] Registered update definition {fileUpdate.Id}: {fileUpdate.SearchPattern} >> {fileUpdate.ReplacePattern}");
					}

					//targetFile.UpdateProperties();
					targetFile.Watcher = new FileSystemWatcher(targetFile.Path, targetFile.FileName);
					targetFile.Watcher.Created += (_, e) => CheckFile(targetFile, CheckType.Watching);
					targetFile.Watcher.Deleted += (_, e) => CheckFile(targetFile, CheckType.Watching);
					targetFile.Watcher.Changed += (_, e) => CheckFile(targetFile, CheckType.Watching);
					targetFile.Watcher.EnableRaisingEvents = true;
				}

				Logger.Add("Starting update thread..");

				Thread updateThread = new Thread(UpdateThread) { IsBackground = true };
				updateThread.Start();

				Logger.Add("Entering main loop..");

				while (!_shutDownRequested)
				{
					try
					{
						foreach (TargetFile targetFile in _targetFiles)
						{
							CheckFile(targetFile, CheckType.Polling);
						}
					}
					catch (Exception ex)
					{
						Logger.Add("Error in main loop", ex);
					}

					Thread.Sleep(Settings.Default.PollFrequency);
				}

				Logger.Add("Terminating update thread..");

				_cancellationTokenSource.Cancel();

				Logger.Add("Shut down completed, signing off");
			}
			catch (Exception ex)
			{
				Logger.Add("Fatal exception in main thread, dying..", ex);
			}
			finally
			{
				_mainThread = null; // indicate that we are no longer running
				Logger.Flush();
			}
		}

		private void UpdateThread()
		{
			try
			{
				Logger.Add("Entering update loop..");

				while (true)
				{
					try
					{
						UpdateQueueItem item = _updateQueue.Take(_cancellationTokenSource.Token);

						if (item.TargetFile.HasChanged())
							UpdateFile(item);
					}
					catch (Exception ex) when (ex.IsNot<OperationCanceledException>())
					{
						Logger.Add("Error in update loop", ex);
					}
				}
			}
			catch (OperationCanceledException)
			{
				Logger.Add("Update thread terminated");
			}
		}

		private void CheckFile(TargetFile targetFile, CheckType checkType)
		{
			if (targetFile.HasChanged())
				_updateQueue.Add(new UpdateQueueItem(targetFile, checkType));
		}

		private static void UpdateFile(UpdateQueueItem item)
		{
			try
			{
				item.TargetFile.Watcher.EnableRaisingEvents = false;

				(bool exists, long fileSize, DateTime modifiedTime) = item.TargetFile.GetLiveProperties();

				if (!exists)
					return;

				string fileContent = item.TargetFile.ReadContent();
				IDictionary<FileUpdate, int> changes = item.TargetFile.FileUpdates.ToDictionary(x => x, _ => 0);

				string updatedFileContent = item.TargetFile.FileUpdates.Aggregate(fileContent,
					(content, fileUpdate) => Regex.Replace(content, fileUpdate.SearchPattern, match =>
						{
							changes[fileUpdate]++;
							return match.Result(fileUpdate.ReplacePattern);
						}, RegexOptions.Multiline));

				int totalChanges = changes.Values.Sum();

				if (totalChanges > 0)
				{
					item.TargetFile.WriteContent(updatedFileContent);
				}

				string action = item.TargetFile.FileSize < 0 ? "Inspected" : "Identified modification to";
				item.TargetFile.UpdateProperties();

				if (totalChanges > 0)
				{
					Logger.Add($"{action} \"{item.TargetFile.FilePath}\"" +
						$" by {item.CheckType.ToString().ToLower()} and made {totalChanges} {"change".Pluralize(totalChanges)}" +
						$" ({fileSize} bytes modified {modifiedTime} > {item.TargetFile.FileSize} bytes modified {item.TargetFile.ModifiedTime})");

					foreach (FileUpdate fileUpdate in changes.Keys)
					{
						Logger.Add($"Update definition {fileUpdate.Id} resulted in {changes[fileUpdate]} {"change".Pluralize(changes[fileUpdate])}");
					}
				}
				else
				{
					Logger.Add($"{action} \"{item.TargetFile.FilePath}\"" +
						$" by {item.CheckType.ToString().ToLower()} but made no {"change".Pluralize(totalChanges)}");
				}
			}
			catch (Exception ex)
			{
				Logger.Add($"Failed to update \"{item.TargetFile.FilePath}\"", ex);
			}
			finally
			{
				item.TargetFile.Watcher.EnableRaisingEvents = true;
			}
		}
	}
}
