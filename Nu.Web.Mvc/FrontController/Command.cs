using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Nu.Web.ViewModel.Attributes;

namespace Nu.Web.ViewModel.FrontController
{
    /// <summary>
    /// Used to store commands for Cmd and their usages.
    /// </summary>
    public class Command
    {
        #region Static Fields

        // Matches an add commmand, 'commandName (param1, param2, param2) some help text'
        private static readonly Regex CommandRegex = new Regex(@"^(?<name>\S+)\s*\(((?<params>[^\s,]+)[\s,]*)*\)\s*(?<help>.*)$");

        /// <summary>
        /// All commands.
        /// </summary>
        private static readonly ConcurrentDictionary<string, Command> Commands = new ConcurrentDictionary<string, Command>();

        #endregion

        #region Static Methods
        private static Dictionary<MethodInfo, T[]> GetMethodWithAttrbute<T>(Object reflectedObject) where T : Attribute, new()
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
        /// <param name="commandObject"></param>
        public static void RegisterObject(Object commandObject)
        {
            var typedMethodInfos = GetMethodWithAttrbute<TypedCommandAttribute>(commandObject);
            foreach (var method in typedMethodInfos.Keys)
            {
                try
                {
                    if (!CheckReturnType(method))
                    {
                        continue;
                    }

                    foreach (var methodAttribute in typedMethodInfos[method])
                    {
                        var usage = methodAttribute.GetUsage(method, GetMethodExectuion(method, commandObject));
                        AddCommand(usage);
                     }
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Unable to register typed method '{0}'", method.Name), ex);
                }
            }
        }

        private static string GetTypedCommandUsage(MethodInfo method, string command)
        {
            var par = method.GetParameters();
            var sb = new StringBuilder();
            sb.Append(command + " ");
            foreach (var p in par)
            {
                var temp = p.ParameterType.ToString().Split('.');
                sb.Append(string.Format("<{0} {1}>, ", temp[temp.Length - 1], p.Name));
            }
            return sb.ToString().TrimEnd(',', ' ');
        }

        private static bool CheckReturnType(MethodInfo method)
        {
            if (method.ReturnType.Name != "String")
            {
                throw new Exception(string.Format("Unable to register method '{0}': doesn't match a return type.", method.Name));
            }
            return true;
        }

        public static void AddCommand(MethodInfo method, Object commandObject)
        {
            IMethodExecution mex = GetMethodExectuion(method, commandObject);
            var usage = new Usage(method.Name, mex);
            AddCommand(usage);
        }
      
        /// <summary>
        /// Adds a command  to commands.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="commandName"></param>
        /// <param name="usage"></param>
        public static void AddCommand(Usage usage)
        {
            if (!Commands.ContainsKey(usage.Name))
            {
                Commands.TryAdd(usage.Name, new Command(usage.Name, usage));
            }
            else
            {
                Commands[usage.Name].Usages.Add(usage);
            }
        }

        /// <summary>
        /// Adds a command  to commands.
        /// </summary>
        /// <param name="usage"></param>
        public static void AddCommand(IEnumerable<Usage> usage)
        {
            usage.ToList().ForEach(AddCommand);
        }

        /// <summary>
        /// Removes a command  to commands.
        /// </summary>
        /// <param name="commandName"></param>
        public static void RemoveCommand(string commandName)
        {
            Command garbage;
            Commands.TryRemove(commandName, out garbage);
        }

        /// <summary>
        /// Checks the existence of commandName in commands.
        /// </summary>
        /// <param name="commandName"></param>
        /// <returns></returns>
        public static bool HasCommand(string commandName)
        {
            return Commands.ContainsKey(commandName);
        }

        /// <summary>
        /// Checks to see if a usage with numberOfParameters exists in commandName for commands.
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="numberOfParameters"></param>
        /// <returns></returns>
        public static bool HasUsage(string commandName, params object[] args)
        {
            Command com;
            if (Commands.TryGetValue(commandName, out com))
            {
                return (from u in com.Usages
                        where  u.MatchesUsage(args)
                        select u).Any();

            }
            return false;
        }

        public static IMethodExecution GetMethodExectuion(MethodInfo method, Object commandObject)
        {
            Command com;
            if (Commands.TryGetValue(method.Name, out com))
            {
                foreach (var u in com.Usages)
                {
                    MethodInfoExecution mie;
                    if ((mie = u.Method as MethodInfoExecution) != null)
                    {
                        if (method == mie.Method)
                        {
                            return mie;
                        }
                    }
                }
            }
            return new MethodInfoExecution(method, commandObject);
        }
      
        /// <summary>
        /// Executes commandName.
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="parameters"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public static bool Invoke(string commandName, object[] parameters, out string output)
        {
            Command com;
            output = "";
            if (Commands.TryGetValue(commandName, out com))
            {
                var args = new object[0];
                List<IMethodExecution> method = (from u in Commands[commandName].Usages
                                                 where u.MatchesUsage(parameters)
                                                 select u.Method).Distinct().ToList();
                string error;
                if (method.Count == 1)
                {
                        var m = method[0] as MethodInfoExecution;
                        output = m.CanExecute(parameters, out args, out error) ? m.Execute(args) : error;
                }
                else
                {
                    List<MethodInfoExecution> mexes = (from mex in method
                                                       where mex is MethodInfoExecution
                                                       select mex as MethodInfoExecution).ToList();

                    MethodInfoExecution valid = null;
                    foreach (var mif in mexes)
                    {
                        object[] temp;
                        if (mif.CanExecute(parameters, out temp, out error))
                        {
                            args = temp;
                            valid = mif;
                        }
                    }

                    output = valid != null ? valid.Execute(args) : "Could not find a method to execute command.";
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets a list of all commands.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetCommands()
        {
            return Commands.Keys.ToList();
        }

        /// <summary>
        /// Gets a specific command.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static Command GetCommand(string command)
        {
            Command result;
            Commands.TryGetValue(command, out result);
            return result;
        }
      
        #endregion

        /// <summary>
        /// The name of the command.
        /// </summary>
        public string CommandName { get; private set; }

        /// <summary>
        /// A list of usages for the command.
        /// </summary>
        public List<Usage> Usages{get; set;}

        /// <summary>
        /// Constructor that takes in one usage.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="cName"></param>
        /// <param name="usage"></param>
        public Command(string cName, Usage usage)
        {
            Usages = new List<Usage> {usage};
            CommandName = cName;
        }

        /// <summary>
        /// Constructor that takes in an array of usages.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="cName"></param>
        /// <param name="aUsages"></param>
        public Command(string cName, IEnumerable<Usage> aUsages)
        {
            CommandName = cName;
            Usages = new List<Usage>(aUsages);
        }
    }
}