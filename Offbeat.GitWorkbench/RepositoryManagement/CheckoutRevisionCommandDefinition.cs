using System.ComponentModel.Composition;
using Gemini.Framework.Commands;
using Gemini.Framework.Menus;

namespace Offbeat.GitWorkbench.RepositoryManagement
{
	[CommandDefinition]
	public class CheckoutRevisionCommandDefinition : CommandDefinition
	{
		public const string CommandName = "Repository.CheckoutSelectedRevision";

		public override string Name => CommandName;

		public override string Text => "Check out";

		public override string ToolTip => "Check out selected revision";

	}
}