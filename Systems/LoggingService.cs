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
                sb.AppendLine(line);
            }
            Plugin.Logger.LogInfo(sb.ToString());
        }
    }
}
