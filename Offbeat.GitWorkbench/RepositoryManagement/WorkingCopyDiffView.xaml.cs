using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Offbeat.GitWorkbench.RepositoryManagement
{
	/// <summary>
	/// Interaction logic for WorkingCopyDiffView.xaml
	/// </summary>
	public partial class WorkingCopyDiffView : UserControl
	{
		public WorkingCopyDiffView()
		{
			InitializeComponent();
		}

		private bool isUpdatingScrollPosition;
		private void NewTextScrollChanged(object sender, ScrollChangedEventArgs e) {
			UpdateScrollPosition(e, oldText);
		}

		private void OldTextScrollChanged(object sender, ScrollChangedEventArgs e) {
			UpdateScrollPosition(e, newText);
		}

		private void UpdateScrollPosition(ScrollChangedEventArgs e, DiffView diffView) {
			if (isUpdatingScrollPosition) {
				return;
			}

			isUpdatingScrollPosition = true;

			diffView.SetScrollPosition(e.HorizontalOffset, e.VerticalOffset);

			isUpdatingScrollPosition = false;
		}
	}
}
