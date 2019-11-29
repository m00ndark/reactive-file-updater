using System;
using ToolComponents.Core;
using ToolComponents.Core.Logging;

namespace ReactiveFileUpdater
{
	internal static class Program
	{
		internal const string SERVICE_NAME = "ReactiveFileUpdaterService";
		internal const string APPLICATION_NAME = "ReactiveFileUpdater";

		private static void Main()
		{
			Logger.Initialize(APPLICATION_NAME, DefaultLogEntry.Headers);

			try
			{
				ServiceStartupSequence.Run<ReactiveFileUpdaterService>(SERVICE_NAME, ReactiveFileUpdater.Instance.Start);
			}
			catch (Exception ex)
			{
				Logger.Add("Fatal error", ex);
			}

			Logger.Flush();
		}
	}
}
