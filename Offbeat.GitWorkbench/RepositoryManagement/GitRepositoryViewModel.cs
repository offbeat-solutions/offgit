using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using Gemini.Framework;
using Gemini.Framework.Commands;
using Gemini.Framework.Threading;
using LibGit2Sharp;
using NLog;
using Offbeat.GitWorkbench.Common;
using Action = System.Action;
using LogManager = NLog.LogManager;
using System.Windows;

namespace Offbeat.GitWorkbench.RepositoryManagement
{
	public class GitRepositoryViewModel : Document, ICommandHandler<CheckoutRevisionCommandDefinition>
	{
		private static ILogger logger = LogManager.GetCurrentClassLogger();
		public string Path { get; }
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

		protected override void OnViewAttached(object view, object context) {
			base.OnViewAttached(view, context);

			this.view = view as IRepositoryView;
		}

		protected override async void OnActivate()
		{
			base.OnActivate();

			await EnsureInitialized();

			if (Repository.Info.CurrentOperation != CurrentOperation.None) {
				BusyIndicatorText = $"Waiting for external Git operation to complete ({Repository.Info.CurrentOperation}).";
			}

			if (Repository != null) {
				StartWatcher();
			}
		}

		async Task ICommandHandler<CheckoutRevisionCommandDefinition>.Run(Command command)
		{
			var rev = (RevisionViewModel) SelectedRevision;

			await CheckoutRevisionAsync(rev);
		}

		void ICommandHandler<CheckoutRevisionCommandDefinition>.Update(Command command)
		{
			command.Enabled = SelectedRevision is RevisionViewModel;
		}

		private Task RunBlockingAsync(string busyIndicatorText, Action action)
		{
			return RunBlockingAsync(busyIndicatorText, () => Task.Run(action));
		}

		private async Task RunBlockingAsync(string busyIndicatorText, Func<Task> action)
		{
			BusyIndicatorText = busyIndicatorText;
			try
			{
				await action();
			}
			finally
			{
				BusyIndicatorText = null;
			}
		}

		public string BusyIndicatorText
		{
			get => _busyIndicatorText;
			set
			{
				if (value == _busyIndicatorText)
				{
					return;
				}
				_busyIndicatorText = value;
				NotifyOfPropertyChange();
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


			await RunBlockingAsync("Opening repository", async () => {
				Repository = await OpenRepositoryAsync();

				if (Repository != null && Repository.Info.CurrentOperation == CurrentOperation.None)
				{
					await LoadCommitsAsync();
				}
			});

			isInitialized = true;
		}

		private TimeSpan changeThreshold = TimeSpan.FromMilliseconds(300);
		private IDisposable changeSubscription;
		private string _busyIndicatorText;

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
			if (Repository.Info.CurrentOperation != CurrentOperation.None)
			{
				BusyIndicatorText = $"Waiting for external Git operation to complete ({Repository.Info.CurrentOperation}).";
				return;
			}

			BusyIndicatorText = null;

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

			var commitLogEntryViewModels = (await Task.Run(() => GetCommits(uncommitted))).ToList();
			commitLogEntryViewModels.Insert(0, uncommitted);

			var allRevisions = new ObservableCollection<ICommitLogEntryViewModel>(commitLogEntryViewModels);

			baseRevision = commitLogEntryViewModels.OfType<RevisionViewModel>()
				.First(c => c.RevisionId == uncommitted.ParentCommitId);

			baseRevision.GraphEntry.IsCurrent = !uncommitted.HasContent;
			baseRevision.GraphEntry.IsFirst = !uncommitted.HasContent;

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
				})
				.OrderByDescending(c => c.Committer.When)
				.ThenByDescending(c => c.Author.When);

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

		private async Task CheckoutRevisionAsync(RevisionViewModel rev)
		{
			
			await RunBlockingAsync($"Checking out {rev.FriendlyName}", async () =>
			{
				await Task.Run(() => {
					var commit = Repository.Lookup<Commit>(rev.FriendlyName);

					Commands.Checkout(Repository, commit, new CheckoutOptions() {});
				});

				await LoadCommitsAsync();
			});
		}

		public async void CheckoutRevision(object rev)
		{
			var revision = SelectedRevision as RevisionViewModel;
			if (revision != null)
			{
				await CheckoutRevisionAsync(revision);
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