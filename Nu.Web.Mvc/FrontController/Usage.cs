using System;

namespace Nu.Web.ViewModel.FrontController
{
    /// <summary>
    /// Specifies a usage for a command
    /// </summary>
    public class Usage
    {
        public string Name { get; set; }
        /// <summary>
        /// Method to execute.
        /// </summary>
        public IMethodExecution Method { get; set; }

        public virtual bool MatchesUsage(params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="nop"></param>
        /// <param name="method"></param>
        public Usage(string name, IMethodExecution method)
        {
            Name = name;
            Method = method;
        }
    }
}