using CSScripting;
using CSScriptLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace CsScriptTest
{
    public class HostRunner
    {
        public List<IPlugin> Plugins { get; private set; }

        public HostRunner(List<IPlugin>? plugins = null)
        {
            Plugins = plugins ?? new List<IPlugin>();
        }

        public void AddPlugin(IPlugin plugin)
        {
            Plugins.Add(plugin);
        }

        public void Run()
        {
            foreach (var plugin in Plugins)
            {
                plugin.Run();
            }
        }

        public void Load()
        {
            // In order to be able to debug the code (step inside the code)
            CSScript.EvaluatorConfig.PdbFormat = Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Embedded;
            CSScript.EvaluatorConfig.DebugBuild = true;

            string code = """
                using System;
                using CsScriptTest;

                public class MyPlugin: Plugin, IPlugin
                {
                        public MyPlugin() : base()
                        {
                        }

                        public MyPlugin(HostRunner hostRunner) : base(hostRunner)
                        {
                        }

                        public override void Run()
                        {
                            Console.Write("MyPlugin");
                        }
                }
                """;

            var script = CSScript.Evaluator
                                 .With(eval => eval.IsAssemblyUnloadingEnabled = true)
                                 .ReferenceAssembliesFromCode(code)
                                 .ReferenceAssembly(Assembly.GetExecutingAssembly())
                                 .ReferenceAssembly(Assembly.GetExecutingAssembly().Location)
                                 .ReferenceAssemblyByName("System")
                                 .ReferenceDomainAssemblies()
                                 .LoadCode(code, this);
        } 
    }


    public interface IPlugin
    {
        public HostRunner? HostRunner { get; set; }

        public void Run();
    }

    public abstract class Plugin : IPlugin
    {
        public HostRunner? HostRunner { get; set; } = null;

        public Plugin()
        {
        }

        public Plugin(HostRunner hostRunner)
        {
            HostRunner = hostRunner;
        }

        public abstract void Run();
    }

    public class MyPlugin2 : Plugin, IPlugin
    {
        public MyPlugin2(HostRunner hostRunner) : base(hostRunner)
        {
        }

        public override void Run()
        {
            Console.Write("MyPlugin");
        }
    }
}
