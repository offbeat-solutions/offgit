using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO.Abstractions;
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
		private ObservableCollection<RevisionViewModel> commits;
		private RevisionViewModel selectedRevision;

		public GitRepositoryViewModel(string path)
		{
			this.Path = path;

			DisplayName = Path;

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

			Repository = await OpenRepositoryAsync();
			if (Repository != null) {
				var revisions = await LoadCommitsAsync();

				var uncommitted = await LoadWorkingDirectoryChangesAsync();
				if (uncommitted != null) {
					revisions.Insert(0, uncommitted);
				}

				Commits = new ObservableCollection<RevisionViewModel>(revisions);

				Loading = false;
			}
		}

		private Task<RevisionViewModel> LoadWorkingDirectoryChangesAsync() {
			return Task.Run(() => {
				var repositoryStatus =
					Repository.RetrieveStatus(new StatusOptions() {
						Show = StatusShowOption.IndexAndWorkDir,
						DetectRenamesInIndex = true,
						DetectRenamesInWorkDir = true
					});
				if (!repositoryStatus.IsDirty) {
					return null;
				}

				return new RevisionViewModel() {
					Message = "Uncommitted changes",

					Changes = repositoryStatus.Modified
						.Concat(repositoryStatus.Added)
						.Concat(repositoryStatus.Removed)
						.Concat(repositoryStatus.Missing)
						.Concat(repositoryStatus.Untracked)
						.Select(FileStatusViewModel.FromFileStatus)
						.OrderBy(ch => (int) ch.State)
						.ThenBy(ch => ch.Path)
						.ToList()
				};
			});
		}

		public RevisionViewModel SelectedRevision {
			get { return selectedRevision; }
			set {
				if (Equals(value, selectedRevision)) return;
				selectedRevision = value;
				NotifyOfPropertyChange();
			}
		}

		public ObservableCollection<RevisionViewModel> Commits
		{
			get { return commits; }
			set
			{
				if (Equals(value, commits)) return;
				commits = value;
				NotifyOfPropertyChange();
			}
		}

		private Task<List<RevisionViewModel>> LoadCommitsAsync()
		{
			return Task.Run(() => {
				return Repository.Commits.Select(c => new RevisionViewModel() {
					Message = c.MessageShort,
					Author = $"{c.Author.Name} <{c.Author.Email}>",
					Hash = c.Sha,
					Created = c.Author.When,
					Changes = c.Parents.SelectMany(p => Repository.Diff.Compare<TreeChanges>(p.Tree, c.Tree))
						.Select(FileStatusViewModel.FromTreeEntryChange)
						.OrderBy(ch => (int) ch.State)
						.ThenBy(ch => ch.Path)
						.ToList()
				}).ToList();
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
	}

	public enum RepositoryFileStatus {
		None = 0,
		Modified = 1,
		Added = 2,
		Removed = 3,
		Missing = 4,
		Renamed = 5,
		Untracked
	}

	public class FileStatusViewModel {
		public string Path { get; set; }
		public RepositoryFileStatus State { get; set; }


		public static FileStatusViewModel FromFileStatus(StatusEntry se) {
			return new FileStatusViewModel() {
				Path = se.FilePath,
				State = GetState(se.State)
			};
		}

		public static FileStatusViewModel FromTreeEntryChange(TreeEntryChanges changes) {
			return new FileStatusViewModel() {
				Path = changes.Path,
				State = GetState(changes.Status)
			};
		}

		private static Dictionary<FileStatus, RepositoryFileStatus> statusesByChangeType = new Dictionary<FileStatus, RepositoryFileStatus>
		{
			[FileStatus.Added] = RepositoryFileStatus.Added,
			[FileStatus.Missing] = RepositoryFileStatus.Missing,
			[FileStatus.Removed] = RepositoryFileStatus.Removed,
			[FileStatus.Modified] = RepositoryFileStatus.Modified,
			[FileStatus.RenamedInIndex] = RepositoryFileStatus.Renamed,
			[FileStatus.RenamedInWorkDir] = RepositoryFileStatus.Renamed,
			[FileStatus.Untracked] = RepositoryFileStatus.Untracked,
		};

		private static Dictionary<ChangeKind, RepositoryFileStatus> statusesByChangeKind = new Dictionary<ChangeKind, RepositoryFileStatus>
		{
			[ChangeKind.Added] = RepositoryFileStatus.Added,
			[ChangeKind.Deleted] = RepositoryFileStatus.Removed,
			[ChangeKind.Modified] = RepositoryFileStatus.Modified,
			[ChangeKind.Renamed] = RepositoryFileStatus.Renamed,
		};

		private static RepositoryFileStatus GetState(FileStatus state) {
			return statusesByChangeType[state];
		}

		private static RepositoryFileStatus GetState(ChangeKind status) {
			return statusesByChangeKind[status];
		}
	}

	public class RevisionViewModel
	{
		public string Message { get; set; }
		public string Author { get; set; }
		public string Hash { get; set; }
		public DateTimeOffset Created { get; set; }
		public IList<FileStatusViewModel> Changes { get; set; }
	}
}