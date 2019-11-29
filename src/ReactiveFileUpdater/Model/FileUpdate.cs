using System;
using Newtonsoft.Json;

namespace ReactiveFileUpdater.Model
{
	public class FileUpdate
	{
		[JsonIgnore]
		public string Id { get; } = Guid.NewGuid().ToString();

		public string FilePath { get; set; }
		public string SearchPattern { get; set; }
		public string ReplacePattern { get; set; }
	}
}
