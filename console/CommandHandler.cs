using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using glowberry.api.server;

namespace glowberry.console
{
    public class CommandHandler : AbstractConsoleCommandExecutor
    {
        
        /// <summary>
        /// Using reflection, accesses all the methods within this class and writes their name, description and usage.
        /// </summary>
        private void Command_Help(ConsoleCommand command)
        {
            MethodInfo[] methods = this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            OutputHandler.Write("Glowberry Commands Help Menu:", Color.Yellow);
            OutputHandler.Write(new string('-', 33 - command.Command.Length), Color.Chocolate);
            
            foreach (MethodInfo method in methods)
            {
                // If the method does not start with "Command_", then it is not a command.
                if (!method.Name.StartsWith("Command_")) continue;
                
                // Gets the command name and removes the "Command_" prefix.
                string commandName = method.Name.Substring(8).Replace("_", " ");
                string description = ConfigurationManager.AppSettings.Get(method.Name + "_Description");
                string usage = ConfigurationManager.AppSettings.Get(method.Name + "_Usage");
                
                // Writes the command name, description and usage.
                string descriptionSpacing = new string(' ', 15 - commandName.Length);
                OutputHandler.Write($"- {commandName}{descriptionSpacing}| {description}");
                OutputHandler.Write($"> Usage: {usage}" + Environment.NewLine);
            }
        }
        
        /// <summary>
        /// Sends an API call to the backend to start a server.
        /// </summary>
        private void Command_Start(ConsoleCommand command)
        {
            string serverName = command.GetValueForField("server");

            // If the server name is null, then the command was used incorrectly.
            if (serverName == null)
            {
                WrongUsage("Start");
                return;
            }
            
            // Prevents another instance of the server from being started if it is already running.
            if (API.Interactions(serverName).IsRunning())
            {
                OutputHandler.Write("This server is already running!", Color.Red);
                return;
            }
            
            
            OutputHandler.Write($"Started server '{serverName}'.", Color.Green);
            // API.Starter(serverName).Run(new MessageProcessingOutputHandler(null));
            Process.Start("cmd.exe");
        }
        
        /// <summary>
        /// Reads the location of the servers from memory and lists them to the console, alongside
        /// their status.
        /// </summary>
        private void Command_Server_List(ConsoleCommand command)
        {
            OutputHandler.Write("Glowberry Servers:", Color.Chocolate);
            
            foreach (var serverName in ServerInteractions.GetServerList()) {
                
                // Gets the status of the server and the color to print it in.
                string status = API.Interactions(serverName).IsRunning() ? "Online" : "Offline";
                Color color = status == "Online" ? Color.Green : Color.DarkGray;

                // Prints the server name and status.
                OutputHandler.Write($"> {serverName} | {status}", color);
            }
        }

        /// <summary>
        /// Asks the API to send a message to the server, by writing into its STDIN.
        /// </summary>
        private void Command_Send_Message(ConsoleCommand command)
        {
            string serverName = command.GetValueForField("server");

            // If the server name is null, then the command was used incorrectly.
            if (serverName == null)
            {
                WrongUsage("Send_Message");
                return;
            }
            
            // Parses out the message to be sent to the server.
            List<string> messageChunks = command.Arguments.ToList().SkipWhile(x => x != "--message").Skip(1).ToList();
            string message = string.Join(" ", messageChunks);
            API.Interactions(serverName).WriteToServerStdin(message);
            
            OutputHandler.Write($"MESSAGE SENT: {message}", Color.Yellow);
            OutputHandler.Write($"TARGET SERVER: {serverName}", Color.Yellow);
        }
        
        /// <summary>
        /// Sends a stop command to the server, by writing into its STDIN.
        /// </summary>
        private void Command_Stop(ConsoleCommand command)
        {
            string serverName = command.GetValueForField("server");

            // If the server name is null, then the command was used incorrectly.
            if (serverName == null)
            {
                WrongUsage("Stop");
                return;
            }
            
            // If the server is not running, then there is no need to stop it.
            if (!API.Interactions(serverName).IsRunning())
            {
                OutputHandler.Write("This server is not running!", Color.Red);
                return;
            }
            
            // Sends the stop command to the server.
            API.Interactions(serverName).WriteToServerStdin("stop");
            OutputHandler.Write($"Stopped server '{serverName}'.", Color.Red);
        }

        /// <summary>
        /// Kills the process associated with the specified server
        /// </summary>
        private void Command_Force_Stop(ConsoleCommand command)
        {
            string serverName = command.GetValueForField("server");

            // If the server name is null, then the command was used incorrectly.
            if (serverName == null)
            {
                WrongUsage("Force_Stop");
                return;
            }
            
            // If the server is not running, then there is no need to stop it.
            if (!API.Interactions(serverName).IsRunning())
            {
                OutputHandler.Write("This server is not running!", Color.Red);
                return;
            }
            
            API.Interactions(serverName).KillServerProcess();
            OutputHandler.Write($"Killed server '{serverName}'.", Color.Red);
        }

        /// <summary>
        /// Sends a stop command to the server, followed by a start command, in order to restart it.
        /// </summary>
        private void Command_Restart(ConsoleCommand command)
        {
            string serverName = command.GetValueForField("server");

            // If the server name is null, then the command was used incorrectly.
            if (serverName == null)
            {
                WrongUsage("Restart");
                return;
            }
            
            // If the server is not running, then there is no need to restart it.
            if (!API.Interactions(serverName).IsRunning())
            {
                OutputHandler.Write("This server is not running!", Color.Red);
                return;
            }
            
            // Restarts the server.
            OutputHandler.Write($"Stopping server '{serverName}'.", Color.Yellow);
            API.Interactions(serverName).WriteToServerStdin("stop");
            bool success = API.Interactions(serverName).GetServerProcess().WaitForExit(10000);
            
            // If the server did not stop, then the restart is aborted.
            if (!success)
            {
                OutputHandler.Write("Failed to stop server; restart aborted. Please try again.", Color.Red);
                return;
            }
            
            OutputHandler.Write($"Starting server '{serverName}'.", Color.Green);
            API.Starter(serverName).Run(OutputHandler);
        }
        
        /// <summary>
        /// Prints the notice of a command being used incorrectly to the console, letting
        /// the user know the correct usage.
        /// </summary>
        private void WrongUsage(string commandName)
        {
            string usage = ConfigurationManager.AppSettings.Get("Command_" + commandName + "_Usage");
            OutputHandler.Write($"Unknown command definition. Usage: {usage}");   
        }
        
    }
}