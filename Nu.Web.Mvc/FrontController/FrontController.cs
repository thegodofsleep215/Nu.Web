using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Nu.Web.ViewModel.Attributes;

namespace Nu.Web.ViewModel.FrontController
{
    public class FrontController
    {
        /// <summary>
        /// All commands.
        /// </summary>
        private readonly ConcurrentDictionary<string, Command> commands = new ConcurrentDictionary<string, Command>();

        private Dictionary<MethodInfo, T[]> GetMethodWithAttrbute<T>(Object reflectedObject) where T : Attribute, new()
        {
            var result = new Dictionary<MethodInfo, T[]>();
            Type t = reflectedObject.GetType();
            Type attType = typeof(T);
            MethodInfo[] methods = t.GetMethods();
            foreach (var method in methods)
            {
                object[] methodAttributres = method.GetCustomAttributes(attType, false);

                if (methodAttributres.Length <= 0)
                {
                    continue;
                }

                result.Add(method, methodAttributres.Cast<T>().ToArray());
            }
            return result;
        }

        /// <summary>
        /// Adds commands functions decorated with Cmd.NuControllerAttribute
        /// </summary>
        public void RegisterObject<T>() where T: new()
        {
            var commandObject = Activator.CreateInstance<T>();
            var typedMethodInfos = GetMethodWithAttrbute<NuControllerAttribute>(commandObject);
            foreach (var method in typedMethodInfos.Keys)
            {
                try
                {
                    AddCommand(method, commandObject);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Unable to register typed method '{0}'", method.Name), ex);
                }
            }
        }

        public void AddCommand(MethodInfo method, Object commandObject)
        {
            if (commands.ContainsKey(method.Name))
            {
                throw new DuplicateNameException("A controller already exists with the name, " + method.Name);
            }
            var mex = GetMethodExectuion(method, commandObject);
            commands[method.Name] = new Command(method.Name, mex);
        }
      
        /// <summary>
        /// Removes a command  to commands.
        /// </summary>
        /// <param name="commandName"></param>
        public void RemoveCommand(string commandName)
        {
            Command garbage;
            commands.TryRemove(commandName, out garbage);
        }

        /// <summary>
        /// Checks the existence of commandName in commands.
        /// </summary>
        /// <param name="commandName"></param>
        /// <returns></returns>
        public bool HasCommand(string commandName)
        {
            return commands.ContainsKey(commandName);
        }

        public MethodInfoExecution GetMethodExectuion(MethodInfo method, Object commandObject)
        {
            return new MethodInfoExecution(method, commandObject);
        }

        /// <summary>
        /// Executes commandName.
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="parameters"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public bool Invoke(string commandName, object[] parameters, out object output)
        {
            Command com;
            output = null;
            if (commands.TryGetValue(commandName, out com))
            {
                object[] args;
                if(com.MethodExecution.CanExecute(parameters, out args))
                {
                    output = com.MethodExecution.Execute(args);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets a list of all commands.
        /// </summary>
        /// <returns></returns>
        public List<string> GetCommands()
        {
            return commands.Keys.ToList();
        }

        /// <summary>
        /// Gets a specific command.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public Command GetCommand(string command)
        {
            Command result;
            commands.TryGetValue(command, out result);
            return result;
        }
    }
}