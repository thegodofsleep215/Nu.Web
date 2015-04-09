using System;

namespace Nu.Web.ViewModel.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class NuControllerAttribute : Attribute
    {
    }
}