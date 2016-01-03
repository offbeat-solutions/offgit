using System;

namespace Offbeat.GitWorkbench {
	public class RepositoryBookmark {
		public Guid Id { get; set; }
		public string Path { get; set; }
		public string Name { get; set; }
		public double? DetailsViewHeight { get; set; }
	}
}