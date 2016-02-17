using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Offbeat.GitWorkbench.RepositoryManagement
{
	public class GraphNode : Control
	{
		public static readonly DependencyProperty GraphEntryProperty = DependencyProperty.Register(
			"GraphEntry", typeof (GraphEntry), typeof (GraphNode), new PropertyMetadata(default(GraphEntry)));

		public GraphEntry GraphEntry {
			get { return (GraphEntry) GetValue(GraphEntryProperty); }
			set { SetValue(GraphEntryProperty, value); }
		}

		public static readonly DependencyProperty MarkerSizeProperty = DependencyProperty.Register(
			"MarkerSize", typeof (double), typeof (GraphNode), new PropertyMetadata(default(double)));

		public double MarkerSize {
			get { return (double) GetValue(MarkerSizeProperty); }
			set { SetValue(MarkerSizeProperty, value); }
		}

		protected override Size MeasureOverride(Size constraint) {
			var markerCount = GraphEntry?.Lines.Max(l => Math.Max(l.StartIndex, l.EndIndex) + 1) ?? 0;

			return new Size(MarkerSize * markerCount, MarkerSize);
		}

		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			return base.ArrangeOverride(arrangeBounds);
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);

			if (GraphEntry == null) {
				return;
			}

			double columnMidpoint = MarkerSize/2;
			double commitMarkerCenter = GraphEntry.RevisionIndex*MarkerSize + columnMidpoint;

			foreach (var line in GraphEntry.Lines) {
				var linePen = GetLinePen(line.Color);

				double startX = line.StartIndex*MarkerSize + columnMidpoint;
				double startY = line.StartsFromThisRevision ? columnMidpoint : LineJoinOffset;

				double endX = line.EndIndex*MarkerSize + columnMidpoint;
				double endY = line.EndsInThisRevision ? columnMidpoint : MarkerSize - LineJoinOffset;

				if (!line.StartsFromThisRevision && !GraphEntry.IsFirst) {
					drawingContext.DrawLine(linePen, new Point(startX, 0), new Point(startX, LineJoinOffset));
				} 

				drawingContext.DrawLine(linePen, new Point(startX, startY), new Point(endX, endY));

				if (!line.EndsInThisRevision) {
					drawingContext.DrawLine(linePen, new Point(endX, endY), new Point(endX, MarkerSize));
				}
			}

			var commitMarkerPen = new Pen(new SolidColorBrush(Colors.Black), 1);

			var revisionBrush = GetBrush(GraphEntry.RevisionColor);

			if (GraphEntry.IsCurrent) {
				drawingContext.DrawEllipse(revisionBrush, commitMarkerPen, new Point(commitMarkerCenter, columnMidpoint), RevisionRadius + 2, RevisionRadius + 2);

				var whitePen = GetLinePen(Colors.White);
				drawingContext.DrawEllipse(whitePen.Brush, whitePen, new Point(commitMarkerCenter, columnMidpoint), RevisionRadius / 2, RevisionRadius / 2);
			} else {
				drawingContext.DrawEllipse(revisionBrush, commitMarkerPen, new Point(commitMarkerCenter, columnMidpoint), RevisionRadius, RevisionRadius);
			}
		}

		private static readonly Dictionary<Color, Pen> PensByColor = new Dictionary<Color, Pen>();
		private static readonly Dictionary<Color, Brush> BrushesByColor = new Dictionary<Color, Brush>();

		private static Pen GetLinePen(Color color) {
			if (!PensByColor.ContainsKey(color)) {
				PensByColor[color] = new Pen(GetBrush(color), 2);
			}

			return PensByColor[color];
		}

		private static Brush GetBrush(Color color) {
			if (!BrushesByColor.ContainsKey(color)) {
				BrushesByColor[color] = new SolidColorBrush(color);
			}

			return BrushesByColor[color];
		}

		private const double LineJoinOffset = 4;
		private const double RevisionRadius = 4;
	}
}
