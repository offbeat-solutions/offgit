using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Gemini.Framework.Commands;
using Gemini.Framework.Services;
using Ookii.Dialogs.Wpf;

namespace Offbeat.GitWorkbench.RepositoryManagement {
	[CommandHandler]
	public class OpenRepositoryCommandHandler : CommandHandlerBase<OpenRepositoryCommandDefinition> {
		[Import] private IShell shell;

		[Import] private IStateManager stateManager;

		public override async Task Run(Command command) {
			var folderBrowserDialog = new VistaFolderBrowserDialog();
			if (folderBrowserDialog.ShowDialog() != true) {
				return;
			}
			var selectedPath = folderBrowserDialog.SelectedPath;

			var alreadyOpen = shell.Documents
				.OfType<GitRepositoryViewModel>()
				.SingleOrDefault(repo => string.Equals(repo.Path, selectedPath, StringComparison.InvariantCultureIgnoreCase));

			shell.OpenDocument(alreadyOpen ?? new GitRepositoryViewModel(selectedPath));

			await stateManager.SaveBookmarksAsync();
		}
	}
}