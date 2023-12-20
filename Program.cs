using glowberry.common;
using glowberry.console;

namespace glowberry
{
    /// <summary>
    /// The main entry point for the console 
    /// </summary>
    internal static class Program
    {
        public static void Main(string[] args)
        {
            // Hides the console logging output for any level below Error.
            Logging.MinimumConsoleLoggingLevel = LoggingLevel.Error;
            
            // If no arguments are provided, default to the help command.
            args = args.Length != 0 ? args : new [] {"help"};
            
            // Parses the command line arguments into a ConsoleCommand object and executes it.
            ConsoleCommand command = ConsoleCommandParser.Parse(args);
            new CommandHandler().ExecuteCommand(command);
        }
    }
}