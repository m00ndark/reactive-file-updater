using System.ServiceProcess;

namespace ReactiveFileUpdater
{
	public partial class ReactiveFileUpdaterService : ServiceBase
	{
		public ReactiveFileUpdaterService()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			if (!ReactiveFileUpdater.Instance.IsRunning)
				ReactiveFileUpdater.Instance.Start();
		}

		protected override void OnStop()
		{
			if (ReactiveFileUpdater.Instance.IsRunning)
				ReactiveFileUpdater.Instance.ShutDown();
		}
	}
}
