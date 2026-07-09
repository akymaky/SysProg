using Serilog.Sinks.SystemConsole.Themes;

namespace _03_34_SysProg.Logger;

public static class LoggerColorTheme
{
    public static readonly SystemConsoleTheme ColorTheme = new(
        new Dictionary<ConsoleThemeStyle, SystemConsoleThemeStyle>
        {
            [ConsoleThemeStyle.Text] = new(),
            [ConsoleThemeStyle.SecondaryText] = new() { Foreground = ConsoleColor.DarkGray },
            [ConsoleThemeStyle.TertiaryText] = new() { Foreground = ConsoleColor.DarkGray },
            [ConsoleThemeStyle.Invalid] = new() { Foreground = ConsoleColor.White, Background = ConsoleColor.Red },
            [ConsoleThemeStyle.Null] = new() { Foreground = ConsoleColor.DarkGray },
            [ConsoleThemeStyle.Name] = new() { Foreground = ConsoleColor.DarkCyan },
            [ConsoleThemeStyle.String] = new() { Foreground = ConsoleColor.DarkGreen },
            [ConsoleThemeStyle.Number] = new() { Foreground = ConsoleColor.DarkMagenta },
            [ConsoleThemeStyle.Boolean] = new() { Foreground = ConsoleColor.DarkMagenta },
            [ConsoleThemeStyle.Scalar] = new() { Foreground = ConsoleColor.DarkMagenta },

            [ConsoleThemeStyle.LevelVerbose] = new()
            {
                Foreground = ConsoleColor.White,
                Background = ConsoleColor.DarkGray
            },
            [ConsoleThemeStyle.LevelDebug] = new()
            {
                Foreground = ConsoleColor.Black,
                Background = ConsoleColor.Cyan
            },
            [ConsoleThemeStyle.LevelInformation] = new()
            {
                Foreground = ConsoleColor.White,
                Background = ConsoleColor.Blue
            },
            [ConsoleThemeStyle.LevelWarning] = new()
            {
                Foreground = ConsoleColor.Black,
                Background = ConsoleColor.Yellow
            },
            [ConsoleThemeStyle.LevelError] = new()
            {
                Foreground = ConsoleColor.White,
                Background = ConsoleColor.Red
            },
            [ConsoleThemeStyle.LevelFatal] = new()
            {
                Foreground = ConsoleColor.White,
                Background = ConsoleColor.DarkRed
            }
        });
}