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
		Task SaveSettingsAsync();
		Task<RepositorySettings> LoadSettingsAsync();
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

		public Task<RepositorySettings> LoadSettingsAsync() {
			return Task.Run(() => {
				try {
					using (var input = new StreamReader(fileSystem.File.OpenRead(GetBookmarkFilePath()))) {
						return (RepositorySettings)new JsonSerializer().Deserialize(input, typeof (RepositorySettings));

					}
				} catch (FileNotFoundException) {
					return null;
				}
			});
		}

		public async Task SaveSettingsAsync() {
			var settings = new RepositorySettings() {
				Bookmarks = shell.Documents.OfType<GitRepositoryViewModel>()
					.Select(d => new RepositoryBookmark() {
						Path = d.Path,
						Name = d.DisplayName,
						DetailsViewHeight = d.DetailsViewHeight,
						Id = d.RepositoryId
					}).ToList(),
				SelectedRepository = shell.Documents
					.OfType<GitRepositoryViewModel>()
					.SingleOrDefault(d => d.IsActive)?.RepositoryId
			};

			await EnsureApplicationDirectory();

			await Task.Run(() => {
				using (var output = new StreamWriter(fileSystem.File.Create(GetBookmarkFilePath()))) {
					new JsonSerializer().Serialize(output, settings);
				}
			});
		}

		private async Task EnsureApplicationDirectory() {
			fileSystem.Directory.CreateDirectory(applicationDirectory);
		}

		private string GetBookmarkFilePath() {
			return fileSystem.Path.Combine(applicationDirectory, "settings.json");
		}
	}

	public class RepositorySettings {
		public Guid? SelectedRepository { get; set; }
		public IList<RepositoryBookmark> Bookmarks { get; set; }
	}
}