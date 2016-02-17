using System.Windows.Controls;
using System.Windows.Data;

namespace Offbeat.GitWorkbench.RepositoryManagement
{
	/// <summary>
	/// Interaction logic for GitRepositoryView.xaml
	/// </summary>
	public partial class GitRepositoryView : IRepositoryView
	{
		public GitRepositoryView()
		{
			InitializeComponent();

			var view = (CollectionViewSource) Resources["VisibleRevisions"];
			view.Filter += (sender, args) => {
				args.Accepted = ((ICommitLogEntryViewModel) args.Item).HasContent;
			};
		}

		public void Refresh() {
			var collectionView = RevisionList.Items.SourceCollection as CollectionView;

			collectionView?.Dispatcher.Invoke(() => collectionView.Refresh());
		}
	}

	public interface IRepositoryView {
		void Refresh();
	}
}
