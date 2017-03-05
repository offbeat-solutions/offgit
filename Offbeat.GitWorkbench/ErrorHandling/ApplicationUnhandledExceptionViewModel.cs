using System;
using Caliburn.Micro;

namespace Offbeat.GitWorkbench.ErrorHandling
{
	public class ApplicationUnhandledExceptionViewModel : Screen
	{
		private Exception exception;
		private string errorText;

		public ApplicationUnhandledExceptionViewModel(Exception exception)
		{
			this.exception = exception;

			ErrorText = 
$@"{exception.Message}
{exception.StackTrace}";
		}

		public string ErrorText
		{
			get { return errorText; }
			set
			{
				if (value == errorText) return;
				errorText = value;
				NotifyOfPropertyChange();
			}
		}

		public void Close()
		{
			TryClose(true);
		}

		public void Continue()
		{
			TryClose(false);
		}
	}
}
