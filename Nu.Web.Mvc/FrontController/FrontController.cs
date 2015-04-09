using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

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
        /// <param name="commandObject"></param>
        public void RegisterObject(Object commandObject)
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

        private string GetTypedCommandUsage(MethodInfo method, string command)
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

        private bool CheckReturnType(MethodInfo method)
        {
            if (method.ReturnType.Name != "String")
            {
                throw new Exception(string.Format("Unable to register method '{0}': doesn't match a return type.", method.Name));
            }
            return true;
        }

        public void AddCommand(MethodInfo method, Object commandObject)
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
        public void AddCommand(Usage usage)
        {
            if (!commands.ContainsKey(usage.Name))
            {
                commands.TryAdd(usage.Name, new Command(usage.Name, usage));
            }
            else
            {
                commands[usage.Name].Usages.Add(usage);
            }
        }

        /// <summary>
        /// Adds a command  to commands.
        /// </summary>
        /// <param name="usage"></param>
        public void AddCommand(IEnumerable<Usage> usage)
        {
            usage.ToList().ForEach(AddCommand);
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

        /// <summary>
        /// Checks to see if a usage with numberOfParameters exists in commandName for commands.
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="numberOfParameters"></param>
        /// <returns></returns>
        public bool HasUsage(string commandName, params object[] args)
        {
            Command com;
            if (commands.TryGetValue(commandName, out com))
            {
                return (from u in com.Usages
                    where  u.MatchesUsage(args)
                    select u).Any();

            }
            return false;
        }

        public IMethodExecution GetMethodExectuion(MethodInfo method, Object commandObject)
        {
            Command com;
            if (commands.TryGetValue(method.Name, out com))
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
        public bool Invoke(string commandName, object[] parameters, out string output)
        {
            Command com;
            output = "";
            if (commands.TryGetValue(commandName, out com))
            {
                var args = new object[0];
                List<IMethodExecution> method = (from u in commands[commandName].Usages
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