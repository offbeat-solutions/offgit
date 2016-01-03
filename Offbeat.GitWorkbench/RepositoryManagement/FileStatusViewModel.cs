using System.Collections.Generic;
using LibGit2Sharp;

namespace Offbeat.GitWorkbench.RepositoryManagement {
	public class FileStatusViewModel {
		public string Path { get; set; }
		public RepositoryFileStatus State { get; set; }


		public static FileStatusViewModel FromFileStatus(StatusEntry se) {
			return new FileStatusViewModel() {
				Path = se.FilePath,
				State = GetUnstagedState(se.State)
			};
		}

		public static FileStatusViewModel FromStagedChangeStatus(StatusEntry se) {
			return new FileStatusViewModel() {
				Path = se.FilePath,
				State = GetStagedState(se.State)
			};
		}

		public static FileStatusViewModel FromTreeEntryChange(TreeEntryChanges changes) {
			return new FileStatusViewModel() {
				Path = changes.Path,
				State = GetState(changes.Status)
			};
		}

		private static Dictionary<FileStatus, RepositoryFileStatus> unstagedStatuses = new Dictionary<FileStatus, RepositoryFileStatus>
		{
			[FileStatus.Missing] = RepositoryFileStatus.Missing,
			[FileStatus.Modified] = RepositoryFileStatus.Modified,
			[FileStatus.RenamedInWorkDir] = RepositoryFileStatus.Renamed,
			[FileStatus.Untracked] = RepositoryFileStatus.Untracked,
			[FileStatus.Staged | FileStatus.Modified] = RepositoryFileStatus.Modified,
			[FileStatus.Added | FileStatus.Modified] = RepositoryFileStatus.Modified
		};

		private static Dictionary<FileStatus, RepositoryFileStatus> stagedStatuses = new Dictionary<FileStatus, RepositoryFileStatus>
		{
			[FileStatus.Added] = RepositoryFileStatus.Added,
			[FileStatus.Removed] = RepositoryFileStatus.Removed,
			[FileStatus.Modified] = RepositoryFileStatus.Modified,
			[FileStatus.RenamedInIndex] = RepositoryFileStatus.Renamed,
			[FileStatus.Staged] = RepositoryFileStatus.Modified,
			[FileStatus.Staged | FileStatus.Modified] = RepositoryFileStatus.Modified,
			[FileStatus.Added | FileStatus.Modified] = RepositoryFileStatus.Added,
		};

		private static Dictionary<ChangeKind, RepositoryFileStatus> statusesByChangeKind = new Dictionary<ChangeKind, RepositoryFileStatus>
		{
			[ChangeKind.Added] = RepositoryFileStatus.Added,
			[ChangeKind.Deleted] = RepositoryFileStatus.Removed,
			[ChangeKind.Modified] = RepositoryFileStatus.Modified,
			[ChangeKind.Renamed] = RepositoryFileStatus.Renamed,
		};

		private static RepositoryFileStatus GetUnstagedState(FileStatus state) {
			return unstagedStatuses[state];
		}

		private static RepositoryFileStatus GetStagedState(FileStatus state) {
			return stagedStatuses[state];
		}

		private static RepositoryFileStatus GetState(ChangeKind status) {
			return statusesByChangeKind[status];
		}

		public static FileStatusViewModel FromTreeEntry(TreeEntry entry) {
			return new FileStatusViewModel() {
				Path = entry.Path,
				State = RepositoryFileStatus.Added
			};
		}
	}

	public enum RepositoryFileStatus {
		None = 0,
		Modified = 1,
		Added = 2,
		Removed = 3,
		Missing = 4,
		Renamed = 5,
		Untracked = 6,
	}
}