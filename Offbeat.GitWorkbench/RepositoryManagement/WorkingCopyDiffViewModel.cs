using System;
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
		private readonly bool staged;
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

		public WorkingCopyDiffViewModel(Repository repository, string path, bool staged) {
			this.staged = staged;
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

		private SideBySideDiffModel GenerateDiff()
		{
			var patch = GetPatch();

			return new SideBySideDiffBuilder(new Differ())
				.BuildDiffModel(patch.OldContent, patch.NewContent);
		}

		private (string OldContent, string NewContent) GetPatch()
		{

			if (staged)
			{
				var headObjectId = Repository.Head.Tip.Tree[Path].Target.Id;
				var indexObjectId = Repository.Index[Path].Id;

				string headContent = GetFileContent(headObjectId);
				string indexContent = GetFileContent(indexObjectId);

				return (headContent ?? "", indexContent ?? "");
			}
			else
			{
				var headObjectId = Repository.Head.Tip.Tree[Path].Target.Id;
				var indexObjectId = Repository.Index[Path]?.Id;

				string originalContent = GetFileContent(indexObjectId ?? headObjectId);

				return (originalContent, GetNewContent());
			}
		}

		private string GetFileContent(ObjectId objectId)
		{
			return Repository.Lookup<Blob>(objectId)?.GetContentText();
		}

		private string GetNewContent() {
			try {
				return File.ReadAllText(System.IO.Path.Combine(Repository.Info.WorkingDirectory, Path));
			} catch (IOException) {
				return "";
			}
		}
	}
}