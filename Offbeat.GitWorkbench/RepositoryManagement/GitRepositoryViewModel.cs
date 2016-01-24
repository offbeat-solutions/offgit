using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
		private FileSystemWatcher watcher;
		private ICommitLogEntryViewModel uncommitted;

		private async Task EnsureInitialized() {
			if (isInitialized) {
				return;
			}

			Loading = true;

			Repository = await OpenRepositoryAsync();
			if (Repository != null) {
				StartWatcher();

				var revisions = await LoadCommitsAsync();

				Commits = new ObservableCollection<ICommitLogEntryViewModel>(revisions);

				Loading = false;
			}

			isInitialized = true;
		}

		private void StartWatcher() {
			watcher = new FileSystemWatcher(Path) {
				IncludeSubdirectories = true
			};
			watcher.Changed += RepositoryDirectoryChanged;
			watcher.Created += RepositoryDirectoryChanged;
			watcher.Deleted += RepositoryDirectoryChanged;
			watcher.Renamed += RepositoryDirectoryChanged;
			watcher.EnableRaisingEvents = true;
		}

		private void StopWatcher() {
			if (watcher == null) {
				return;
			}

			watcher.EnableRaisingEvents = false;
			watcher.Changed -= RepositoryDirectoryChanged;
			watcher.Created -= RepositoryDirectoryChanged;
			watcher.Deleted -= RepositoryDirectoryChanged;
			watcher.Renamed -= RepositoryDirectoryChanged;
		}

		private void SuspendWatcher() {
			watcher.EnableRaisingEvents = false;
		}

		private void ResumeWatcher() {
			watcher.EnableRaisingEvents = true;
		}

		public override void TryClose(bool? dialogResult = null) {
			base.TryClose(dialogResult);

			if (dialogResult == true) {
				StopWatcher();
			}
		}

		private DateTime? previousChangeNotification;
		private TimeSpan changeThreshold = TimeSpan.FromMilliseconds(300);
		private async void RepositoryDirectoryChanged(object sender, FileSystemEventArgs fileSystemEventArgs) {
			var time = DateTime.UtcNow;
			var notification = previousChangeNotification;

			previousChangeNotification = time;
			if (time - notification < changeThreshold) {
				return;
			}

			try {
				SuspendWatcher();

				await RefreshRepositoryStatus();
			} finally {
				ResumeWatcher();
			}
		}

		private async Task RefreshRepositoryStatus() {

			var previousTipId = Commits.OfType<RevisionViewModel>().FirstOrDefault()?.RevisionId;
			var newTipId = Repository.Head.Tip.Id;

			if (previousTipId != newTipId) {
				var revisions = await LoadCommitsAsync();
				Commits = new ObservableCollection<ICommitLogEntryViewModel>(revisions);
				return;
			}

			var firstRevision = Commits.FirstOrDefault() as UncommittedChangesViewModel;

			var newWorkingDirectoryState = LoadWorkingDirectoryChanges();
			if (newWorkingDirectoryState == null && firstRevision == null) {
				return;
			}

			var newCommitList = new ObservableCollection<ICommitLogEntryViewModel>();

			if (newWorkingDirectoryState != null) {
				newCommitList.Add(newWorkingDirectoryState);
			}

			var firstCommit = Commits.FirstOrDefault();
			if (firstCommit != null) {
				firstCommit.GraphEntry.IsCurrent = newWorkingDirectoryState == null;
			}

			foreach (var commit in commits.OfType<RevisionViewModel>()) {
				newCommitList.Add(commit);
			}

			if (SelectedRevision is UncommittedChangesViewModel) {
				SelectedRevision = newWorkingDirectoryState;
			}

			Commits = newCommitList;
		}

		private ICommitLogEntryViewModel LoadWorkingDirectoryChanges() {
			var repositoryStatus =
				Repository.RetrieveStatus(new StatusOptions() {
					Show = StatusShowOption.IndexAndWorkDir,
					DetectRenamesInIndex = true,
					DetectRenamesInWorkDir = true
				});
			if (!repositoryStatus.IsDirty || !repositoryStatus.Any()) {
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

			uncommitted = LoadWorkingDirectoryChanges();
			if (uncommitted != null) {
				yield return uncommitted;
			}

			ObjectId currentTip = null;
			if (uncommitted == null) {
				currentTip = Repository.Head.Tip.Id;
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
					GraphEntry = GraphEntry.FromCommit(previous, commit, currentTip)
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