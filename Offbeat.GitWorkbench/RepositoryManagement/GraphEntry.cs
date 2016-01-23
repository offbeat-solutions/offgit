using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using LibGit2Sharp;

namespace Offbeat.GitWorkbench.RepositoryManagement {
	public class GraphEntry {
		public List<GraphLine> Lines { get; set; } = new List<GraphLine>();
		public Color RevisionColor { get; set; }
		public int RevisionIndex { get; set; }
		public ObjectId RevisionId { get; set; }
		public static GraphEntry FromCommit(GraphEntry previous, Commit commit) {

			if (previous == null) {
				return CreateFirstEntry(commit);
			}

			return CreateEntryFromCommit(previous, commit);
		}

		private static GraphEntry CreateEntryFromCommit(GraphEntry previous, Commit commit) {
			var entry = new GraphEntry() {RevisionId = commit.Id};

			ContinuePreviousLines(previous, commit, entry);
			AddNewBranches(commit, entry);
			return entry;
		}

		private static void ContinuePreviousLines(GraphEntry previous, Commit commit, GraphEntry entry) {
			bool commitIndexSet = false;
			for (int index = 0; index < GetLinesToContinue(previous).Count; index++) {
				var line = GetLinesToContinue(previous)[index];

				if (line.ParentId == entry.RevisionId) {
					if (!commitIndexSet) {
						entry.RevisionIndex = index;
						entry.RevisionColor = line.Color;
						commitIndexSet = true;
					}

					entry.Lines.Add(new GraphLine() {
						BranchIndex = line.BranchIndex,
						ParentId = commit.Parents.FirstOrDefault()?.Id,
						Color = line.Color,
						StartIndex = index,
						EndIndex = entry.RevisionIndex,
						EndsInThisRevision = index != entry.RevisionIndex || !commit.Parents.Any()
					});
				} else {
					entry.Lines.Add(new GraphLine() {
						BranchIndex = line.BranchIndex,
						ParentId = line.ParentId,
						Color = line.Color,
						StartIndex = index,
						EndIndex = entry.Lines.Count(l => !l.EndsInThisRevision)
					});
				}
			}
		}

		private static List<GraphLine> GetLinesToContinue(GraphEntry previous) {
			return previous.Lines
				.Where(l => !l.EndsInThisRevision)
				.GroupBy(l => l.EndIndex)
				.Select(g => g.First())
				.ToList();
		}

		private static GraphEntry CreateFirstEntry(Commit commit) {
			var entry = new GraphEntry {
				RevisionId = commit.Id,
				RevisionIndex = 0,
				RevisionColor = GetBranchColor(0),
				Lines = {
					new GraphLine() {
						ParentId = commit.Parents.FirstOrDefault()?.Id,
						Color = GetBranchColor(0),
						BranchIndex = 0,
						StartsFromThisRevision = true,
						StartIndex = 0,
						EndIndex = 0
					}
				}
			};

			AddNewBranches(commit, entry);
			return entry;
		}

		private static void AddNewBranches(Commit commit, GraphEntry entry) {
			var branchIndex = entry.Lines.Max(l => l.BranchIndex) + 1;

			foreach (var parent in commit.Parents.Skip(1)) {
				var matchingLine = entry.Lines.FirstOrDefault(e => e.ParentId == parent.Id);
				int currentBranchIndex, endIndex;
				if (matchingLine != null) {
					currentBranchIndex = matchingLine.BranchIndex;
					endIndex = matchingLine.EndIndex;
				} else {
					currentBranchIndex = branchIndex;
					endIndex = entry.Lines.Select(l => l.EndIndex).Distinct().Count();
					branchIndex += 1;
				}

				entry.Lines.Add(new GraphLine() {
					ParentId = parent.Id,
					BranchIndex = currentBranchIndex,
					Color = GetBranchColor(currentBranchIndex),
					StartsFromThisRevision = true,
					StartIndex = entry.RevisionIndex,
					EndIndex = endIndex
				});
			}
		}

		private static Color GetBranchColor(int index) {
			return BranchColors[index%BranchColors.Count];
		}

		private static readonly List<Color> BranchColors = new List<Color>() {
			Colors.DodgerBlue,
			Colors.MediumVioletRed,
			Colors.DarkKhaki,
			Colors.LightGreen,
			Colors.DarkViolet,
			Colors.SaddleBrown,
			Colors.ForestGreen,
			Colors.Aquamarine,
			Colors.BlueViolet
		};
	}

	public class GraphLine {
		public int StartIndex { get; set; }
		public int EndIndex { get; set; }
		public bool StartsFromThisRevision { get; set; }
		public bool EndsInThisRevision { get; set; }
		public int BranchIndex { get; set; }

		public Color Color { get; set; }
		public ObjectId ParentId { get; set; }
	}
}