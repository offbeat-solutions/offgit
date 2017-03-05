using System.ComponentModel.Composition;
using Gemini.Framework.Menus;
using Gemini.Modules.MainMenu;
using Offbeat.GitWorkbench.RepositoryManagement;

namespace Offbeat.GitWorkbench {
	public static class MenuItems {
		[Export]
		public static ExcludeMenuItemDefinition ExcludeOpenMenuItem = new ExcludeMenuItemDefinition(Gemini.Modules.Shell.MenuDefinitions.FileOpenMenuItem);

		[Export]
		public static ExcludeMenuItemDefinition ExcludeSaveMenuItem = new ExcludeMenuItemDefinition(Gemini.Modules.Shell.MenuDefinitions.FileSaveMenuItem);

		[Export]
		public static ExcludeMenuItemDefinition ExcludeSaveAsMenuItem = new ExcludeMenuItemDefinition(Gemini.Modules.Shell.MenuDefinitions.FileSaveAsMenuItem);

		[Export]
		public static MenuItemDefinition OpenRepositoryMenuItem = new CommandMenuItemDefinition<OpenRepositoryCommandDefinition>(MenuDefinitions.FileNewOpenMenuGroup, 0);
	}

	public static class RepositoryMenuDefinition
	{

		[Export]
		public static MenuDefinition RepositoryMenu = new MenuDefinition(MenuDefinitions.MainMenuBar, 11, "Repository");

		[Export]
		public static MenuItemGroupDefinition RevisionActions = new MenuItemGroupDefinition(RepositoryMenu, 0);

		[Export]
		public static MenuItemDefinition MenuItem = new CommandMenuItemDefinition<CheckoutRevisionCommandDefinition>(RevisionActions, 0);
	}
}