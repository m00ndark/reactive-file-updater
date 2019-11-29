namespace ReactiveFileUpdater.Model
{
	public enum CheckType
	{
		Polling,
		Watching
	}

	public class UpdateQueueItem
	{
		public UpdateQueueItem(TargetFile targetFile, CheckType checkType)
		{
			TargetFile = targetFile;
			CheckType = checkType;
		}

		public TargetFile TargetFile { get; }
		public CheckType CheckType { get; }
	}
}