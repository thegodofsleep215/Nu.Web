using System;
using System.Reflection;
using System.Text;
using Nu.Web.ViewModel.FrontController;

namespace Nu.Web.ViewModel.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class NuControllerAttribute : Attribute
    {
        public string Command { get; set; }
        public NuControllerAttribute(string command)
        {
            Command = command;
        }

        public NuControllerAttribute() { }

    }
}