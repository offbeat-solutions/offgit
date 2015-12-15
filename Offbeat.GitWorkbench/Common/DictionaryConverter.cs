using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Offbeat.GitWorkbench.Common
{
	public class VisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if (Equals(value, VisibleValue)) {
				return Visibility.Visible;
			}

			if (Equals(value, HiddenValue)) {
				return Visibility.Hidden;
			}

			if (Equals(value, CollapsedValue)) {
				return Visibility.Collapsed;
			}

			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}

		public object VisibleValue { get; set; }
		public object HiddenValue { get; set; }
		public object CollapsedValue { get; set; }
	}
}
