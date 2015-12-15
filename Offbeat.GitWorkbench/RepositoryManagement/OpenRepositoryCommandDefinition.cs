using System;
using System.ComponentModel.Composition;
using System.Windows.Input;
using Gemini.Framework.Commands;
using Gemini.Modules.Shell.Commands;

namespace Offbeat.GitWorkbench.RepositoryManagement {
	[CommandDefinition]
	public class OpenRepositoryCommandDefinition : CommandDefinition
	{
		public const string CommandName = "File.OpenRepository";

		public override string Name => CommandName;

		public override string Text => "_Open repository...";

		public override string ToolTip => "Open an existing Git repository";

		public override Uri IconSource
		{
			get { return new Uri("pack://application:,,,/Gemini;component/Resources/Icons/Open.png"); }
		}

		[Export]
		public static CommandKeyboardShortcut KeyGesture = new CommandKeyboardShortcut<OpenFileCommandDefinition>(new KeyGesture(Key.O, ModifierKeys.Control));
	}
}