using System.ComponentModel.Composition;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using Gemini.Framework;
using Gemini.Framework.Services;
using Gemini.Modules.MainWindow.Views;
using Gemini.Modules.Shell.Views;
using Offbeat.GitWorkbench.RepositoryManagement;

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

			this.shell.AttemptingDeactivation += ShellDeactivating;
		}

		private void ShellDeactivating(object sender, DeactivationEventArgs e) {
			stateManager.SaveSettingsAsync();
		}

		public override void Initialize() {
			base.Initialize();

			mainWindow.Title = "OffGit";

			stateManager.LoadSettingsAsync()
				.ContinueWith(t => {
					foreach (var b in t.Result.Bookmarks) {
						shell.OpenDocument(new GitRepositoryViewModel(b.Path, b.Name) {
							DetailsViewHeight = b.DetailsViewHeight,
							RepositoryId = b.Id
						});
					}

					if (t.Result.SelectedRepository.HasValue) {
						var activeRepository = shell.Documents.OfType<GitRepositoryViewModel>()
							.SingleOrDefault(d => d.RepositoryId == t.Result.SelectedRepository.Value);

						shell.OpenDocument(activeRepository);
					}
				});
		}
	}

	public class FileSystemExporter {
		[Export(typeof(IFileSystem))]
		public IFileSystem FileSystem => new FileSystem();
	}
}
