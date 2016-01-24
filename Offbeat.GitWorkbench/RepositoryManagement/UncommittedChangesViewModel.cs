using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gemini.Framework;
using LibGit2Sharp;

namespace Offbeat.GitWorkbench.RepositoryManagement {
	public class UncommittedChangesViewModel : Document, ICommitLogEntryViewModel {
		private readonly Repository repository;
		private bool isLoading;
		private FileStatusViewModel selectedStagedChange;
		private FileStatusViewModel selectedUnstagedChange;
		private WorkingCopyDiffViewModel selectedChange;

		public UncommittedChangesViewModel(Repository repository) {
			this.repository = repository;
		}

		public string Message => "Uncommitted changes";

		public IReadOnlyList<FileStatusViewModel> Changes { get; private set; }

		public IReadOnlyList<FileStatusViewModel> Index { get; private set; }

		public GraphEntry GraphEntry { get; set; }

		public bool IsLoading {
			get { return isLoading; }
			set {
				if (value == isLoading) return;
				isLoading = value;
				NotifyOfPropertyChange();
			}
		}

		protected override async void OnViewLoaded(object view) {
			await RefreshStatusAsync();
		}

		public FileStatusViewModel SelectedStagedChange {
			get { return selectedStagedChange; }
			set {
				if (Equals(value, selectedStagedChange)) {
					return;
				}

				selectedStagedChange = value;
				NotifyOfPropertyChange();

				if (value != null) {
					SelectedUnstagedChange = null;
					SelectedChange = new WorkingCopyDiffViewModel(repository, value.Path);
				}
			}
		}

		public FileStatusViewModel SelectedUnstagedChange {
			get { return selectedUnstagedChange; }
			set {
				if (Equals(value, selectedUnstagedChange)) {
					return;
				}

				selectedUnstagedChange = value;
				NotifyOfPropertyChange();

				if (value != null) {
					SelectedStagedChange = null;
					SelectedChange = new WorkingCopyDiffViewModel(repository, value.Path);
				}
			}
		}

		public WorkingCopyDiffViewModel SelectedChange {
			get { return selectedChange; }
			set {
				if (Equals(value, selectedChange)) return;
				selectedChange = value;
				NotifyOfPropertyChange();
			}
		}

		public async void Stage(FileStatusViewModel change) {
			repository.Stage(change.Path);

			await RefreshStatusAsync();
		}

		public async void StageAll() {
			foreach (var change in Changes) {
				repository.Stage(change.Path);
			}

			await RefreshStatusAsync();
		}

		public async void Unstage(FileStatusViewModel change) {
			repository.Unstage(change.Path);

			await RefreshStatusAsync();
		}

		public async void UnstageAll() {
			foreach (var change in Index) {
				repository.Unstage(change.Path);
			}

			await RefreshStatusAsync();
		}

		private async Task RefreshStatusAsync() {
			IsLoading = true;

			await Task.Run(() => {
				var repositoryStatus = repository.RetrieveStatus();

				Changes = repositoryStatus.Modified
					.Concat(repositoryStatus.Untracked)
					.Concat(repositoryStatus.Missing)
					.Select(FileStatusViewModel.FromFileStatus)
					.OrderBy(ch => (int) ch.State)
					.ThenBy(ch => ch.Path)
					.ToList();

				Index = repositoryStatus.Staged
					.Concat(repositoryStatus.Added)
					.Concat(repositoryStatus.Removed)
					.Select(FileStatusViewModel.FromStagedChangeStatus)
					.OrderBy(ch => (int) ch.State)
					.ThenBy(ch => ch.Path)
					.ToList();

				NotifyOfPropertyChange(() => Changes);
				NotifyOfPropertyChange(() => Index);
			});

			IsLoading = false;
		}
	}
}