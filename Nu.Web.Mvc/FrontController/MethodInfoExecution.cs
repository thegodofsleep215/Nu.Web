using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nu.Web.ViewModel.FrontController
{
    class MethodInfoExecution : IMethodExecution
    {

        public MethodInfo Method { get; set; }
        public Object CommandObject { get; set; }

        #region MethodExecution Members

        public string Execute(object[] args)
        {
            return (string)Method.Invoke(CommandObject, args);
        }

        #endregion

        public MethodInfoExecution(MethodInfo method, Object commandObject)
        {
            Method = method;
            CommandObject = commandObject;
        }

        public bool CanExecute(object[] args, out object[] finalParams, out string error)
        {
            error = "";
            object[] p = args;
            var mParams = Method.GetParameters();
            finalParams = new object[mParams.Count()];
            var optionalMap = new Dictionary<string, string>();
            int i = 0;
            for (; i < p.Count(); i++)
            {
                ParameterInfo pInfo = mParams[i];
                try
                {
                    if (pInfo.ParameterType.BaseType != null && pInfo.ParameterType.BaseType.Name == "Enum")
                    {
                        // Do not parse Enums if they were passed as ints.
                        int garbage;
                        if (int.TryParse((string)p[i], out garbage))
                        {
                            error = "Type Error: Cannot convert an interger type to an Enum, please use the string version of the enum.";
                            return false;
                        }
                        finalParams[i] = Enum.Parse(pInfo.ParameterType, (string)p[i], true);
                    }
                    else if (pInfo.ParameterType.BaseType != null && pInfo.ParameterType.BaseType.IsArray)
                    {
                        throw new NotImplementedException();
                    }
                    else if (pInfo.ParameterType.BaseType != null && pInfo.ParameterType.BaseType.Name == "Dictionary`2")
                    {
                        throw new NotImplementedException();
                    }
                    else if (pInfo.ParameterType.Name == p[i].GetType().Name + "&")
                    {
                        finalParams[i] = p[i];
                    }
                    else
                    {
                        string t = p[i].GetType().Name;
                        if (t == "Dictionary`2")
                        {
                            // addcharm name [type=simple]
                            if (i == p.Count() - 1)
                            {
                                optionalMap = (Dictionary<string, string>)p[i];
                            }
                            else
                            {
                                error = string.Format("Unexpected Dicionary: A dictionary for optional parameters must be the last parameter passed.");
                            }
                        }
                        else
                        {
                            finalParams[i] = Convert.ChangeType(p[i], pInfo.ParameterType);
                        }
                    }
                }
                catch
                {
                    error = String.Format("Type Error: Cannot convert '{0}' to a(n) '{1}'", p[i], pInfo.ParameterType.Name);
                    return false;
                }
            }
            if (optionalMap.Count > 0)
            {
                i--;
            }

            // Fill in the default parameters.
            for (; i < mParams.Count(); i++)
            {
                ParameterInfo pInfo = mParams[i];
                string okey= "";
                foreach (string k in optionalMap.Keys)
                {
                    if (k.ToUpper() == pInfo.Name.ToUpper())
                    {
                        okey = k;
                        break;
                    }
                }

                if (okey.Length > 0 && pInfo.Attributes.HasFlag(ParameterAttributes.HasDefault))
                {
                    if (pInfo.ParameterType.BaseType != null && pInfo.ParameterType.BaseType.Name == "Enum")
                    {
                        // Do not parse Enums if they were passed as ints.
                        int garbage;
                        if (int.TryParse(optionalMap[okey], out garbage))
                        {
                            error = "Type Error: Cannot convert an interger type to an Enum, please use the string version of the enum.";
                            return false;
                        }
                        finalParams[i] = Enum.Parse(pInfo.ParameterType, (string)p[i], true);
                    }
                    else
                    {
                        finalParams[i] = Convert.ChangeType(optionalMap[okey], pInfo.ParameterType);
                    }
                    optionalMap.Remove(okey);
                }
                else if (pInfo.Attributes.HasFlag(ParameterAttributes.HasDefault))
                {
                    finalParams[i] = pInfo.DefaultValue;
                }
                else
                {
                    error = String.Format("Syntax Error: Not enought parameters, '{0}' did not have a default value.", pInfo.Name);
                    return false;
                }
            }
            if (optionalMap.Count > 0)
            {
                error = "Parameter Error: Unknown default parameters were given.";
                return false;
            }
            return true;
        }
    }
}