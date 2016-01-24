using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using DiffPlex.DiffBuilder.Model;

namespace Offbeat.GitWorkbench.RepositoryManagement
{
	/// <summary>
	/// Interaction logic for DiffView.xaml
	/// </summary>
	public partial class DiffView {
		private ScrollViewer scrollViewer;

		public DiffView() {
			InitializeComponent();


			Loaded += (sender, args) => {
				scrollViewer = (ScrollViewer) viewer.Template.FindName("PART_ContentHost", viewer);
				scrollViewer.ScrollChanged += OnDocumentScrollChanged;
			};
		}


		public static readonly DependencyProperty DiffProperty = DependencyProperty.Register(
			"Diff", typeof (DiffPaneModel), typeof (DiffView), new PropertyMetadata(default(DiffPaneModel), OnDiffChange));

		public DiffPaneModel Diff {
			get { return (DiffPaneModel) GetValue(DiffProperty); }
			set { SetValue(DiffProperty, value); }
		}

		public event EventHandler<ScrollChangedEventArgs> ScrollChanged;

		private static void OnDiffChange(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs) {
			var oldValue = (DiffPaneModel) dependencyPropertyChangedEventArgs.OldValue;
			var newValue = (DiffPaneModel) dependencyPropertyChangedEventArgs.NewValue;

			((DiffView) dependencyObject).OnDiffChange(oldValue, newValue);
		}

		private void OnDiffChange(DiffPaneModel oldValue, DiffPaneModel newValue) {
			var textBlocks = newValue.Lines.Select(Format).ToList();

			var paragraph = new Paragraph {
				KeepTogether = true
			};
			paragraph.Inlines.AddRange(textBlocks.Select(t => new Span() {
				Inlines = { t, new LineBreak() }
			}));

			var document = new FlowDocument {
				FontFamily = new FontFamily("Consolas"),
				FontSize = 11,
				Blocks = {paragraph},
				PageWidth = textBlocks.Max(block => {
					block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
					return block.DesiredSize.Width;
				}) + 50,
			};

			textBlocks.ForEach(tb => tb.Width = document.PageWidth);

			viewer.Document = document;
		}

		private TextBlock Format(DiffPiece diffPiece) {
			return new TextBlock() {
				Text = FormatText(diffPiece),
				TextWrapping = TextWrapping.NoWrap,
				Background = GetLineBackgroundBrush(diffPiece.Type)
			};
		}

		private static string FormatText(DiffPiece diffPiece) {
			return new StringBuilder(diffPiece.Text)
				.Replace(' ', '·')
				.Replace("\t", "→   ")
				.ToString();
		}

		private Brush GetLineBackgroundBrush(ChangeType type) {
			return new SolidColorBrush(GetLineBackgroundColor(type));
		}

		private Color GetLineBackgroundColor(ChangeType type) {
			switch (type) {
				case ChangeType.Imaginary:
					return Colors.LightGray;
				case ChangeType.Deleted:
					return Colors.LightPink;
				case ChangeType.Inserted:
					return Colors.LightGreen;
				case ChangeType.Modified:
					return Colors.LightSteelBlue;
				default:
					return Colors.White;
			}
		}

		private void OnDocumentScrollChanged(object sender, ScrollChangedEventArgs e) {
			ScrollChanged?.Invoke(this, e);
		}

		public void SetScrollPosition(double x, double y) {
			scrollViewer.ScrollToHorizontalOffset(x);
			scrollViewer.ScrollToVerticalOffset(y);
		}
	}
}
