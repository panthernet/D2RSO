using System.Windows.Input;

namespace D2RSO.Classes
{
    internal static class StaticCommands
    {
        public static ICommand CloseOptionsCommand { get; } =
            new RoutedUICommand(nameof(CloseOptionsCommand), nameof(CloseOptionsCommand), typeof(StaticCommands));
    }
}
