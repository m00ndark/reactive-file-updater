using System.ComponentModel;
using System.Configuration.Install;

namespace ReactiveFileUpdater
{
	[RunInstaller(true)]
	public partial class ReactiveFileUpdaterServiceInstaller : Installer
	{
		public ReactiveFileUpdaterServiceInstaller()
		{
			InitializeComponent();
		}
	}
}
