using System.Collections.Generic;

namespace Offbeat.GitWorkbench.RepositoryManagement {
	public interface ICommitLogEntryViewModel {
		string Message { get; }
		
		IReadOnlyList<FileStatusViewModel> Changes { get; }

		bool IsLoading { get; }
	}
}