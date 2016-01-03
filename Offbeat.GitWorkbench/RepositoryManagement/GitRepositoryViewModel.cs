using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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

				var uncommitted = await LoadWorkingDirectoryChangesAsync();
				if (uncommitted != null) {
					revisions.Insert(0, uncommitted);
				}

				Commits = new ObservableCollection<ICommitLogEntryViewModel>(revisions);

				Loading = false;
			}

			isInitialized = true;
		}

		private Task<ICommitLogEntryViewModel> LoadWorkingDirectoryChangesAsync() {
			return Task.Run(() => {
				var repositoryStatus =
					Repository.RetrieveStatus(new StatusOptions() {
						Show = StatusShowOption.IndexAndWorkDir,
						DetectRenamesInIndex = true,
						DetectRenamesInWorkDir = true
					});
				if (!repositoryStatus.IsDirty) {
					return (ICommitLogEntryViewModel)null;
				}

				return new UncommittedChangesViewModel(Repository);
			});
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
			return Task.Run(() => {
				return Repository.Commits.Select(c => new RevisionViewModel(Repository) {
					Message = c.MessageShort,
					Author = $"{c.Author.Name} <{c.Author.Email}>",
					Hash = c.Sha,
					Created = c.Author.When,
				})
				.Cast<ICommitLogEntryViewModel>()
				.ToList();
			});
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