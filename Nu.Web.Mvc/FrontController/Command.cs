using System.Collections.Generic;

namespace Nu.Web.ViewModel.FrontController
{
    /// <summary>
    /// Used to store commands for Cmd and their usages.
    /// </summary>
    public class Command
    {
        public Command(string commandName, MethodInfoExecution methodExecution)
        {
            CommandName = commandName;
            MethodExecution = methodExecution;
        }
        
        /// <summary>
        /// The name of the command.
        /// </summary>
        public string CommandName { get; private set; }

        public MethodInfoExecution MethodExecution { get; set; }

    }
}