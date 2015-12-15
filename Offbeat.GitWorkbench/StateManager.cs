using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Converters;
using Gemini.Framework.Services;
using Newtonsoft.Json;
using Offbeat.GitWorkbench.RepositoryManagement;

namespace Offbeat.GitWorkbench {
	public interface IStateManager {
		Task SaveBookmarksAsync();
		Task<IList<GitRepositoryViewModel>> LoadBookmarksAsync();
	}

	[Export(typeof(IStateManager))]
	public class StateManager : IStateManager {
		private readonly IShell shell;
		private readonly IFileSystem fileSystem;
		private readonly string applicationDirectory;

		[ImportingConstructor]
		public StateManager(IShell shell, IFileSystem fileSystem) {
			this.shell = shell;
			this.fileSystem = fileSystem;

			applicationDirectory = fileSystem.Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				"GitWorkbench");

		}

		public Task<IList<GitRepositoryViewModel>> LoadBookmarksAsync() {
			return Task.Run(() => {
				try {
					using (var input = new StreamReader(fileSystem.File.OpenRead(GetBookmarkFilePath()))) {
						var bookmarks = (IList<RepositoryBookmark>)new JsonSerializer().Deserialize(input, typeof (List<RepositoryBookmark>));

						return (IList<GitRepositoryViewModel>)bookmarks.Select(b => new GitRepositoryViewModel(b.Path)).ToList();
					}
				} catch (FileNotFoundException) {
					return new List<GitRepositoryViewModel>();
				}
			});
		}

		public async Task SaveBookmarksAsync() {
			var bookmarks = shell.Documents.OfType<GitRepositoryViewModel>()
				.Select(d => new RepositoryBookmark() {
					Path = d.Path
				});

			await EnsureApplicationDirectory();

			await Task.Run(() => {
				using (var output = new StreamWriter(fileSystem.File.OpenWrite(GetBookmarkFilePath()))) {
					new JsonSerializer().Serialize(output, bookmarks);
				}
			});
		}

		private async Task EnsureApplicationDirectory() {
			fileSystem.Directory.CreateDirectory(applicationDirectory);
		}

		private string GetBookmarkFilePath() {
			return fileSystem.Path.Combine(applicationDirectory, "bookmarks.json");
		}
	}
}