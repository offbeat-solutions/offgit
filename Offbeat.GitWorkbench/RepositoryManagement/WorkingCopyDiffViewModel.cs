using System.IO;
using System.Threading.Tasks;
using Caliburn.Micro;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using LibGit2Sharp;
using Xceed.Wpf.DataGrid.FilterCriteria;

namespace Offbeat.GitWorkbench.RepositoryManagement {
	public class WorkingCopyDiffViewModel : Screen {
		private bool isLoading;
		private DiffPaneModel newText;
		private DiffPaneModel oldText;
		public Repository Repository { get; }
		public string Path { get; }

		public bool IsLoading {
			get { return isLoading; }
			set {
				if (value == isLoading) return;
				isLoading = value;
				NotifyOfPropertyChange();
			}
		}

		public DiffPaneModel NewText {
			get { return newText; }
			set {
				if (Equals(value, newText)) return;
				newText = value;
				NotifyOfPropertyChange();
			}
		}

		public DiffPaneModel OldText {
			get { return oldText; }
			set {
				if (Equals(value, oldText)) return;
				oldText = value;
				NotifyOfPropertyChange();
			}
		}

		public WorkingCopyDiffViewModel(Repository repository, string path) {
			Repository = repository;
			Path = path;
		}

		protected override async void OnViewLoaded(object view) {
			base.OnViewLoaded(view);

			IsLoading = true;

			var diff = await GenerateDiffAsync();

			NewText = diff.NewText;
			OldText = diff.OldText;

			IsLoading = false;
		}

		private Task<SideBySideDiffModel> GenerateDiffAsync() {
			return Task.Run(() => GenerateDiff());
		}

		private SideBySideDiffModel GenerateDiff() {
			var patch = Repository.Diff.Compare<Patch>(Repository.Head.Tip.Tree, DiffTargets.Index | DiffTargets.WorkingDirectory, new []{Path});
			var fileChanges = patch[Path];

			string oldContent = GetOldContent(fileChanges);
			string newContent = GetNewContent();
				

			return new SideBySideDiffBuilder(new Differ())
				.BuildDiffModel(oldContent, newContent);
		}

		private string GetNewContent() {
			try {
				return File.ReadAllText(System.IO.Path.Combine(Repository.Info.WorkingDirectory, Path));
			} catch (FileNotFoundException) {
				return "";
			}
		}

		private string GetOldContent(PatchEntryChanges fileChanges) {
			return Repository.Lookup<Blob>(fileChanges.OldOid)?.GetContentText() ?? "";
		}
	}
}