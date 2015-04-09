using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mustache;
using Nu.Web.ViewModel.Attributes;

namespace Nu.Web.ViewModel
{
    public static class ViewModelEngine
    {
        private static readonly Dictionary<Type, Generator> templates = new Dictionary<Type, Generator>();
        private static readonly FormatCompiler compiler = new FormatCompiler();
        private static string folder = ".\\";

        public static string Compile<T>(T model)
        {
            var t = typeof (T);
            if (!templates.ContainsKey(t))
            {
                throw new ArgumentException("Unknown type.");
            }
            return templates[t].Render(model);
        }

        public static void CrawlForModels()
        {
            var ea = Assembly.GetExecutingAssembly();
            var assemblies = new List<Assembly> { ea } ;
            assemblies.AddRange(ea.GetReferencedAssemblies().Select(Assembly.Load));
            assemblies.ForEach(CrawlForModels);
        }

        private static void CrawlForModels(Assembly ass)
        {
            ass.GetTypes().Where(t => t.GetCustomAttribute<NuModelAttribute>() != null).ToList().ForEach(RegisterModel);
        }

        public static void RegisterModel(Type t)
        {
            var template = LocateViewModelTemplate(t);
            templates[t] = template;
        }

        private static Generator LocateViewModelTemplate(Type t)
        {
            return compiler.Compile(ReadTemplateFile(t));
        }

        private static string ReadTemplateFile(Type t)
        {
            var file = Path.Combine(folder, string.Format("{0}.mst", t.Name));
            if (!File.Exists(file))
            {
                return null;
            }

            using (var fs = new StreamReader(File.OpenRead(file)))
            {
                return fs.ReadToEnd();
            }
        }
    }
}
