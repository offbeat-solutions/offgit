using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using Gemini.Framework;
using LibGit2Sharp;

namespace Offbeat.GitWorkbench.RepositoryManagement
{
	public class GitRepositoryViewModel : Document
	{
		public string Path { get; }
		private bool loading;
		private ObservableCollection<ICommitLogEntryViewModel> commits;
		private ICommitLogEntryViewModel selectedRevision;
		private double? detailsViewHeight;

		public GitRepositoryViewModel(string path, string repositoryName)
		{
			this.Path = path;

			DisplayName = repositoryName;

			Loading = true;
		}

		public bool Loading
		{
			get { return loading; }
			set
			{
				if (value == loading)
				{
					return;
				}
				loading = value;
				NotifyOfPropertyChange();
			}
		}

		protected override async void OnActivate()
		{
			base.OnActivate();

			await EnsureInitialized();
		}

		private bool isInitialized;
		private async Task EnsureInitialized() {
			if (isInitialized) {
				return;
			}

			Repository = await OpenRepositoryAsync();
			if (Repository != null) {
				var revisions = await LoadCommitsAsync();

				Commits = new ObservableCollection<ICommitLogEntryViewModel>(revisions);

				Loading = false;
			}

			isInitialized = true;
		}

		private ICommitLogEntryViewModel LoadWorkingDirectoryChanges() {
			var repositoryStatus =
				Repository.RetrieveStatus(new StatusOptions() {
					Show = StatusShowOption.IndexAndWorkDir,
					DetectRenamesInIndex = true,
					DetectRenamesInWorkDir = true
				});
			if (!repositoryStatus.IsDirty) {
				return null;
			}

			return new UncommittedChangesViewModel(Repository) {
				GraphEntry = GraphEntry.FromWorkingDirectory(repositoryStatus, Repository.Head.Tip)
			};
		}

		public ICommitLogEntryViewModel SelectedRevision {
			get { return selectedRevision; }
			set {
				if (Equals(value, selectedRevision)) return;
				selectedRevision = value;
				NotifyOfPropertyChange();
			}
		}

		public double? DetailsViewHeight {
			get { return detailsViewHeight; }
			set {
				if (value.Equals(detailsViewHeight)) return;
				detailsViewHeight = value;
				NotifyOfPropertyChange();
			}
		}

		public ObservableCollection<ICommitLogEntryViewModel> Commits
		{
			get { return commits; }
			set
			{
				if (Equals(value, commits)) return;
				commits = value;
				NotifyOfPropertyChange();
			}
		}

		private Task<List<ICommitLogEntryViewModel>> LoadCommitsAsync()
		{
			return Task.Run(() => GetCommits().ToList());
		}

		private IEnumerable<ICommitLogEntryViewModel> GetCommits() {

			var uncommitted = LoadWorkingDirectoryChanges();
			if (uncommitted != null) {
				yield return uncommitted;
			}

			var branchHeads = Repository.Branches.ToLookup(b => b.Tip.Id, b => b.Name);
			var tags = Repository.Tags.ToLookup(b => b.Target.Id, b => b.Name);

			GraphEntry previous = uncommitted?.GraphEntry;
			var commitLog = Repository.Commits.QueryBy(new CommitFilter() { Since = Repository.Refs.Where(r => !r.CanonicalName.StartsWith("refs/stash")) });

			foreach (var commit in commitLog) {
				var current = new RevisionViewModel(Repository) {
					RevisionId = commit.Id,
					Message = commit.MessageShort,
					Author = $"{commit.Author.Name} <{commit.Author.Email}>",
					Hash = commit.Sha,
					Created = commit.Author.When,
					Labels = branchHeads[commit.Id].Concat(tags[commit.Id]).ToList(),
					GraphEntry = GraphEntry.FromCommit(previous, commit)
				};
				yield return current;

				previous = current.GraphEntry;
			}
		}

		private Task<Repository> OpenRepositoryAsync()
		{
			return Task.Run(() =>
			{
				if (Repository.IsValid(Path))
				{
					return new Repository(Path);
				}
				return null;
			});
		}

		private Repository Repository { get; set; }
		public Guid RepositoryId { get; set; }

	}
}