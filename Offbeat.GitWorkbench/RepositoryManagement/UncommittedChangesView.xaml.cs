using System.Windows;
using System.Windows.Controls;

namespace Offbeat.GitWorkbench.RepositoryManagement
{
	/// <summary>
	/// Interaction logic for UncommittedChangesView.xaml
	/// </summary>
	public partial class UncommittedChangesView {
		public UncommittedChangesView()
		{
			InitializeComponent();
		}

		private void StageAllClicked(object sender, RoutedEventArgs e) {
			((CheckBox) sender).IsChecked = false;
		}

		private void UnstageAllClicked(object sender, RoutedEventArgs e) {
			((CheckBox) sender).IsChecked = true;
		}
	}
}
