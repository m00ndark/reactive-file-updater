namespace ReactiveFileUpdater.Model
{
	public class FileUpdate
	{
		public string FilePath { get; set; }
		public string SearchPattern { get; set; }
		public string ReplacePattern { get; set; }
	}
}
