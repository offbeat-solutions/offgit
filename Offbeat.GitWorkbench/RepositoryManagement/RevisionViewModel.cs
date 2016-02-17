using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gemini.Framework;
using LibGit2Sharp;

namespace Offbeat.GitWorkbench.RepositoryManagement {
	public class RevisionViewModel : Document, ICommitLogEntryViewModel
	{
		private readonly Repository repository;

		public RevisionViewModel(Repository repository) {
			this.repository = repository;
		}

		public string Message { get; set; }
		public string Author { get; set; }
		public string Hash { get; set; }
		public DateTimeOffset Created { get; set; }

		public bool IsLoading {
			get { return isLoading; }
			set {
				if (value == isLoading) return;
				isLoading = value;
				NotifyOfPropertyChange();
			}
		}

		private bool isLoading;
		private IReadOnlyList<FileStatusViewModel> changes;

		public IReadOnlyList<FileStatusViewModel> Changes {
			get {
				if (changes == null) {
					PopulateChangesAsync();
				}

				return changes;
			}
		}

		public IList<string> Labels { get; set; }
		public ObjectId RevisionId { get; set; }
		public GraphEntry GraphEntry { get; set; }

		public bool HasContent => true;

		private Task PopulateChangesAsync() {
			IsLoading = true;

			return Task.Run(() => {
				var commit = repository.Lookup<Commit>(Hash);

				IEnumerable<FileStatusViewModel> unsorted;
				if (commit.Parents.Any()) {
					unsorted = commit.Parents
						.SelectMany(p => repository.Diff.Compare<TreeChanges>(p.Tree, commit.Tree))
						.Select(FileStatusViewModel.FromTreeEntryChange);
				} else {
					unsorted = commit.Tree.Select(FileStatusViewModel.FromTreeEntry);
				}

				changes = unsorted
					.OrderBy(ch => (int) ch.State)
					.ThenBy(ch => ch.Path)
					.ToList();

				NotifyOfPropertyChange(() => Changes);

				IsLoading = false;
			});
		}
	}
}