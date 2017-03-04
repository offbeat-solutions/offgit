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

		public static readonly DependencyProperty MarkerHeightProperty = DependencyProperty.Register(
			"MarkerHeight", typeof (double), typeof (GraphNode), new PropertyMetadata(default(double)));

		public double MarkerHeight {
			get { return (double) GetValue(MarkerHeightProperty); }
			set { SetValue(MarkerHeightProperty, value); }
		}

		public static readonly DependencyProperty MarkerWidthProperty = DependencyProperty.Register(
			"MarkerWidth", typeof (double), typeof (GraphNode), new PropertyMetadata(default(double)));

		public double MarkerWidth {
			get { return (double) GetValue(MarkerWidthProperty); }
			set { SetValue(MarkerWidthProperty, value); }
		}

		protected override Size MeasureOverride(Size constraint) {
			var markerCount = GraphEntry?.Lines.Max(l => Math.Max(l.StartIndex, l.EndIndex) + 1) ?? 0;

			return new Size(MarkerWidth * markerCount, MarkerHeight - LineStartOffset*2);
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

			double columnMidpointX = MarkerWidth/2;
			double columnMidpointY = MarkerHeight/2 - LineStartOffset;

			double commitMarkerCenterX = GraphEntry.RevisionIndex*MarkerWidth + columnMidpointX;

			foreach (var line in GraphEntry.Lines) {
				var linePen = GetLinePen(line.Color);

				double startX = line.StartIndex*MarkerWidth + columnMidpointX;
				double startY = line.StartsFromThisRevision ? columnMidpointY : LineJoinOffset;

				double endX = line.EndIndex*MarkerWidth + columnMidpointX;
				double endY = line.EndsInThisRevision ? columnMidpointY : MarkerHeight - LineJoinOffset;

				if (!line.StartsFromThisRevision && !GraphEntry.IsFirst) {
					drawingContext.DrawLine(linePen, new Point(startX, -LineStartOffset), new Point(startX, LineJoinOffset));
				} 

				drawingContext.DrawLine(linePen, new Point(startX, startY), new Point(endX, endY));

				if (!line.EndsInThisRevision) {
					drawingContext.DrawLine(linePen, new Point(endX, endY), new Point(endX, MarkerHeight));
				}
			}

			var commitMarkerPen = new Pen(new SolidColorBrush(Colors.Black), 1);

			var revisionBrush = GetBrush(GraphEntry.RevisionColor);

			if (GraphEntry.IsCurrent) {
				drawingContext.DrawEllipse(revisionBrush, commitMarkerPen, new Point(commitMarkerCenterX, columnMidpointY), RevisionRadius + 2, RevisionRadius + 2);

				var whitePen = GetLinePen(Colors.White);
				drawingContext.DrawEllipse(whitePen.Brush, whitePen, new Point(commitMarkerCenterX, columnMidpointY), RevisionRadius / 2, RevisionRadius / 2);
			} else {
				drawingContext.DrawEllipse(revisionBrush, commitMarkerPen, new Point(commitMarkerCenterX, columnMidpointY), RevisionRadius, RevisionRadius);
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

		private const double LineStartOffset = 2;
		private const double LineJoinOffset = 4;
		private const double RevisionRadius = 4;
	}
}
