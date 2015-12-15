using System.ComponentModel.Composition;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Gemini.Framework;
using Gemini.Framework.Services;
using Gemini.Modules.MainWindow.Views;
using Gemini.Modules.Shell.Views;

namespace Offbeat.GitWorkbench
{
	[Export(typeof(IModule))]
	public class CoreModule : ModuleBase
	{
		private readonly IShell shell;
		private readonly IStateManager stateManager;
		private readonly IMainWindow mainWindow;

		[ImportingConstructor]
		public CoreModule(IShell shell, IStateManager stateManager, IMainWindow mainWindow) {
			this.shell = shell;
			this.stateManager = stateManager;
			this.mainWindow = mainWindow;
		}

		public override void Initialize() {
			base.Initialize();

			mainWindow.Title = "OffGit";

			stateManager.LoadBookmarksAsync()
				.ContinueWith(t => {
					foreach (var repository in t.Result) {
						shell.OpenDocument(repository);
					}
				});
		}
	}

	public class FileSystemExporter {
		[Export(typeof(IFileSystem))]
		public IFileSystem FileSystem => new FileSystem();
	}
}
