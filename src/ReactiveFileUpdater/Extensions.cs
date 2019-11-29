using System;
using System.IO;
using System.Text;
using System.Threading;
using ReactiveFileUpdater.Model;
using ToolComponents.Core.Logging;

namespace ReactiveFileUpdater
{
	public static class Extensions
	{
		public static bool IsNot<T>(this Exception exception)
		{
			return !(exception is T);
		}

		public static (bool Exists, long FileSize, DateTime ModifiedTime) GetLiveProperties(this TargetFile targetFile)
		{
			FileInfo fileInfo = new FileInfo(targetFile.FilePath);
			bool fileExists = fileInfo.Exists;
			return (fileExists, fileExists ? fileInfo.Length : -1, fileExists ? fileInfo.LastAccessTime : DateTime.MinValue);
		}

		public static void UpdateProperties(this TargetFile targetFile)
		{
			FileInfo fileInfo = new FileInfo(targetFile.FilePath);
			targetFile.FileSize = fileInfo.Exists ? fileInfo.Length : -1;
			targetFile.ModifiedTime = fileInfo.Exists ? fileInfo.LastWriteTime : DateTime.MinValue;
		}

		public static bool HasChanged(this TargetFile targetFile)
		{
			FileInfo fileInfo = new FileInfo(targetFile.FilePath);
			return fileInfo.Exists
				&& (fileInfo.Length != targetFile.FileSize || fileInfo.LastWriteTime != targetFile.ModifiedTime);
		}

		public static string ReadContent(this TargetFile targetFile, int retries = 3)
		{
			while (true)
			{
				try
				{
					return File.ReadAllText(targetFile.FilePath, Encoding.UTF8);
				}
				catch (IOException) when (retries-- > 0)
				{
					Thread.Sleep(TimeSpan.FromMilliseconds(200));
				}
			}
		}

		public static void WriteContent(this TargetFile targetFile, string content, int retries = 3)
		{
			while (true)
			{
				try
				{
					File.WriteAllText(targetFile.FilePath, content, Encoding.UTF8);
					return;
				}
				catch (IOException) when (retries-- > 0)
				{
					Thread.Sleep(TimeSpan.FromMilliseconds(200));
				}
			}
		}

		public static string Pluralize(this string word, int count = int.MinValue)
		{
			if (count == 1)
				return word;

			return word.EndsWith("s")
				? word + "es"
				: word.EndsWith("y")
					? word.Substring(0, word.Length - 1) + "ies"
					: word + "s";
		}
	}
}
