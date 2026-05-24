using BepInEx.Logging;
using System.Text;

namespace Dread.Systems
{
    public enum LogLevel
    {
        None = 0,
        Error = 1,
        Debug = 2,
        Verbose = 3
    }

    public static class LoggingService
    {
        private static LogLevel _current = LogLevel.Debug;

        private const string AnsiRed = "\u001b[31m";
        private const string AnsiBlue = "\u001b[34m";
        private const string AnsiReset = "\u001b[0m";

        private static readonly string[] AsciiArtLines =
        [
            @"         ███             ███             ███             ███  ",
            @"       ███░            ███░            ███░            ███░   ",
            @"     ███░            ███░            ███░            ███░     ",
            @"   ███░            ███░            ███░            ███░       ",
            @" ███░            ███░            ███░            ███░         ",
            @"██░            ███░            ███░            ███░           ",
            @"░            ███░            ███░            ███░            █",
            @"            ░░░             ░░░             ░░░             ░░",
            @"     ███             ███             ███             ███      ",
            @"   ███@@@@@@@   @@@@@@@   @@@@@@@@ ██@@@@@@   @@@@@@@█░       ",
            @" ███░ @@@@@@@@  @@@@@@@@  @@@@@@@@██@@@@@@@@  @@@@@@@@        ",
            @"██░   @@!  @@@ █@@!  @@@  @@!  ███░ @@!  @@@  @@!  @@@        ",
            @"░     !@!  @!@██!@!  @!@  !@!███░   !@!  @!@ █!@!  @!@       █",
            @"      @!@  !@!░ @!@!!@!   @!!!:!    @!@!@!@!██@!@  !@!     ███",
            @"      !@!  !!!  !!@!@!   █!!!!!:    !!!@!!!!░ !@!  !!!   ███░ ",
            @"      !!:  !!!  !!: :!! ░░!!:       !!: ░!!!  !!:  !!!  ░░░   ",
            @" ███  :!:  !:!  :!:█ !:!  :!:    ███:!:  !:!  :!:  !:!        ",
            @"██░    :::: :: █::░  :::   :: ::::░ ::   :::   :::: ::        ",
            @"░     :: :  :███░:   : :  : ::█::    :   : : █::░:  :        █",
            @"           ███░            ███░            ███░            ███",
            @"         ███░            ███░            ███░            ███░ ",
            @"       ███░            ███░            ███░            ███░   ",
            @"     ███░            ███░            ███░            ███░     ",
            @"    ░░░             ░░░             ░░░             ░░░       ",
            @"             ███             ███             ███             █",
            @"           ███░            ███░            ███░            ███",
            @"         ███░            ███░            ███░            ███░ ",
            @"       ███░            ███░            ███░            ███░   ",
        ];

        public static void Initialize(LogLevel level)
        {
            _current = level;
        }

        public static void SetLevel(LogLevel level)
        {
            _current = level;
        }

        public static LogLevel CurrentLevel => _current;

        public static void LogError(string message)
        {
            if (_current >= LogLevel.Error)
                Plugin.Logger.LogError(message);
        }

        public static void LogWarning(string message)
        {
            if (_current >= LogLevel.Debug)
                Plugin.Logger.LogWarning(message);
        }

        public static void LogInfo(string message)
        {
            if (_current >= LogLevel.Debug)
                Plugin.Logger.LogInfo(message);
        }

        public static void LogDebug(string message)
        {
            if (_current >= LogLevel.Debug)
                Plugin.Logger.LogDebug(message);
        }

        public static void LogVerbose(string message)
        {
            if (_current >= LogLevel.Verbose)
                Plugin.Logger.LogInfo($"[V] {message}");
        }

        public static void PrintAsciiArt()
        {
            var sb = new StringBuilder();
            foreach (string line in AsciiArtLines)
            {
                string currentColor = "";
                foreach (char c in line)
                {
                    string newColor;
                    if (c == '\u2588' || c == '\u2591')
                        newColor = AnsiRed;
                    else if (c == '@')
                        newColor = AnsiBlue;
                    else
                        newColor = AnsiReset;

                    if (newColor != currentColor)
                    {
                        sb.Append(newColor);
                        currentColor = newColor;
                    }
                    sb.Append(c);
                }
                sb.AppendLine(AnsiReset);
            }
            Plugin.Logger.LogInfo(sb.ToString());
        }
    }
}
