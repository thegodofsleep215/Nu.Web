using System;
using System.Reflection;
using System.Text;

namespace Nu.Web.ViewModel.FrontController
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TypedCommandAttribute : Attribute
    {   
        public string Command { get; set; }
        public string HelpText { get; set; }

        public virtual Usage GetUsage(MethodInfo methodInfo, IMethodExecution methodExecution)
        {
            var par = methodInfo.GetParameters();
            var sb = new StringBuilder();
            sb.Append(Command + " ");
            foreach (var p in par)
            {
                var temp = p.ParameterType.ToString().Split('.');
                sb.Append(string.Format("<{0} {1}>, ", temp[temp.Length - 1], p.Name));
            }

            string u = sb.ToString().TrimEnd(',', ' ');

            return new Usage(methodInfo.Name, methodExecution);
        }

        public TypedCommandAttribute(string command, string helpText)
        {
            Command = command;
            HelpText = helpText;
        }

        public TypedCommandAttribute()
        {
        }
    }
}