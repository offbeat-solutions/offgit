using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using Gemini.Framework;
using LibGit2Sharp;
using NLog;
using LogManager = NLog.LogManager;
using System.Windows;

namespace Offbeat.GitWorkbench.RepositoryManagement
{
	public class GitRepositoryViewModel : Document
	{
		private static ILogger logger = LogManager.GetCurrentClassLogger();
		public string Path { get; }
		private bool loading;
		private ICommitLogEntryViewModel selectedRevision;
		private double? detailsViewHeight;
		private IRepositoryView view;

		public GitRepositoryViewModel(string path, string repositoryName)
		{
			this.Path = path;

			DisplayName = repositoryName;

			CopyHash = new RelayCommand((item) => {
				var rev = item as RevisionViewModel;
				if (rev != null)
				{
					Clipboard.SetText(rev.Hash);
				}
			},
			(item) =>
			{
				return item is RevisionViewModel;
			});
		}

		public RelayCommand CopyHash { get; }

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

		protected override void OnViewAttached(object view, object context) {
			base.OnViewAttached(view, context);

			this.view = view as IRepositoryView;
		}

		protected override async void OnActivate()
		{
			base.OnActivate();

			await EnsureInitialized();

			if (Repository != null) {
				StartWatcher();
			}
		}

		private bool isInitialized;
		private FileSystemWatcher watcher;
		private UncommittedChangesViewModel uncommitted;
		private RevisionViewModel baseRevision;

		private async Task EnsureInitialized() {
			if (isInitialized) {
				return;
			}

			Loading = true;

			Repository = await OpenRepositoryAsync();
			if (Repository != null) {
				await LoadCommitsAsync();

				Loading = false;
			}

			isInitialized = true;
		}

		private TimeSpan changeThreshold = TimeSpan.FromMilliseconds(300);
		private IDisposable changeSubscription;
		private void StartWatcher() {
			watcher = new FileSystemWatcher(Path) {
				IncludeSubdirectories = true
			};

			var changedObservable = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(h => watcher.Changed += h, h => watcher.Changed -= h);
			var createdObservable = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(h => watcher.Created += h, h => watcher.Created -= h);
			var deletedObservable = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(h => watcher.Deleted += h, h => watcher.Deleted -= h);
			var renamedObservable = Observable.FromEventPattern<RenamedEventHandler, FileSystemEventArgs>(h => watcher.Renamed += h, h => watcher.Renamed -= h);

			changeSubscription = changedObservable
				.Concat(createdObservable)
				.Concat(deletedObservable)
				.Concat(renamedObservable)
				.Throttle(changeThreshold)
				.Subscribe(_ => RefreshRepositoryStatus());

			watcher.EnableRaisingEvents = true;
		}

		private void StopWatcher() {
			if (watcher == null) {
				return;
			}

			changeSubscription?.Dispose();

			watcher.EnableRaisingEvents = false;
			watcher.Dispose();
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

		private async void RefreshRepositoryStatus() {
			logger.Trace($"Working directory was based on {uncommitted.ParentCommitId}. Current head is {Repository.Head.Tip.Id} ({Repository.Head.FriendlyName}).");
			if (uncommitted.ParentCommitId != Repository.Head.Tip.Id) {
				logger.Debug($"Refreshing entire commit tree");
				await LoadCommitsAsync();
				return;
			}

			logger.Debug($"Refreshing working directory status");
			await uncommitted.LoadWorkingDirectoryStatusAsync();
			if (baseRevision != null) {
				baseRevision.GraphEntry.IsCurrent = !uncommitted.HasContent;
				baseRevision.GraphEntry.IsFirst = !uncommitted.HasContent;
			}

			view?.Refresh();
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

		public BindableCollection<ICommitLogEntryViewModel> Commits { get; set; } = new BindableCollection<ICommitLogEntryViewModel>();

		private async Task LoadCommitsAsync()
		{
			uncommitted = new UncommittedChangesViewModel(Repository) {
				GraphEntry = GraphEntry.FromWorkingDirectory(Repository.Head.Tip)
			};
			await uncommitted.LoadWorkingDirectoryStatusAsync();

			var allRevisions = new ObservableCollection<ICommitLogEntryViewModel>(await Task.Run(() => GetCommits(uncommitted)));
			allRevisions.Insert(0, uncommitted);

			Commits.Clear();
			Commits.AddRange(allRevisions);
		}

		private IEnumerable<ICommitLogEntryViewModel> GetCommits(UncommittedChangesViewModel workingDirectory) {
			var branchHeads = Repository.Branches.ToLookup(b => b.Tip.Id, b => b.FriendlyName);
			var tags = Repository.Tags.ToLookup(b => b.Target.Id, b => b.FriendlyName);

			GraphEntry previous = workingDirectory.GraphEntry;
			var commitLog = Repository.Commits.QueryBy(new CommitFilter()
			{
				IncludeReachableFrom = Repository.Refs.Where(r => !r.CanonicalName.StartsWith("refs/stash")).ToList()
			});

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

				if (current.RevisionId == workingDirectory.ParentCommitId) {
					baseRevision = current;
					baseRevision.GraphEntry.IsCurrent = !workingDirectory.HasContent;
					baseRevision.GraphEntry.IsFirst = !workingDirectory.HasContent;
				}

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