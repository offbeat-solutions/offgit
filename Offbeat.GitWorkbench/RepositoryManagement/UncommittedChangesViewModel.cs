using System.Collections.Generic;
using System.IO;
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
		private IReadOnlyList<FileStatusViewModel> changes = new FileStatusViewModel[0];
		private IReadOnlyList<FileStatusViewModel> index = new FileStatusViewModel[0];

		public UncommittedChangesViewModel(Repository repository) {
			this.repository = repository;
		}

		public string Message => "Uncommitted changes";

		public IReadOnlyList<FileStatusViewModel> Changes {
			get { return changes; }
			private set {
				if (Equals(value, changes)) return;
				changes = value;
				NotifyOfPropertyChange();
				NotifyOfPropertyChange(nameof(HasContent));
			}
		}

		public IReadOnlyList<FileStatusViewModel> Index {
			get { return index; }
			private set {
				if (Equals(value, index)) return;
				index = value;
				NotifyOfPropertyChange();
				NotifyOfPropertyChange(nameof(HasContent));
			}
		}

		public GraphEntry GraphEntry { get; set; }

		public ObjectId ParentCommitId { get; set; }

		public bool IsLoading {
			get { return isLoading; }
			set {
				if (value == isLoading) return;
				isLoading = value;
				NotifyOfPropertyChange();
			}
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
					SelectedChange = new WorkingCopyDiffViewModel(repository, value.Path, staged: true);
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
					SelectedChange = new WorkingCopyDiffViewModel(repository, value.Path, staged: false);
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

		public bool HasContent => Changes.Any() || Index.Any();

		public async void Stage(FileStatusViewModel change) {
			repository.Stage(change.Path);

			await LoadWorkingDirectoryStatusAsync();
		}

		public async void StageAll() {
			foreach (var change in Changes) {
				repository.Stage(change.Path);
			}

			await LoadWorkingDirectoryStatusAsync();
		}

		public async void Unstage(FileStatusViewModel change) {
			repository.Unstage(change.Path);

			await LoadWorkingDirectoryStatusAsync();
		}

		public async void UnstageAll() {
			foreach (var change in Index) {
				repository.Unstage(change.Path);
			}

			await LoadWorkingDirectoryStatusAsync();
		}

		public async void Discard() {
			ObjectId id = repository.Index[SelectedUnstagedChange.Path]?.Id;

			if (id == null) {
				repository.CheckoutPaths(ParentCommitId.Sha, new[] {SelectedUnstagedChange.Path}, new CheckoutOptions() {CheckoutModifiers = CheckoutModifiers.Force});
			} else {
				await RevertToVersion(id);
			}

			await LoadWorkingDirectoryStatusAsync();
		}

		private async Task RevertToVersion(ObjectId id)
		{
			using (var contentStream = repository.Lookup<Blob>(id).GetContentStream(new FilteringOptions(SelectedUnstagedChange.Path)))
			using (var fileStream = File.Open(
				Path.Combine(repository.Info.WorkingDirectory, SelectedUnstagedChange.Path),
				FileMode.Create,
				FileAccess.Write))
			{
				await contentStream.CopyToAsync(fileStream);
			}
		}

		public async Task LoadWorkingDirectoryStatusAsync() {
			IsLoading = true;

			Changes = new FileStatusViewModel[0];
			Index = new FileStatusViewModel[0];

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

				ParentCommitId = repository.Head.Tip.Id;

				NotifyOfPropertyChange(() => Changes);
				NotifyOfPropertyChange(() => Index);
			});

			IsLoading = false;
		}
	}
}