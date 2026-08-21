using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Xml.Linq;
using CSScriptLib;

// Important : its indicate where the ICalc and ICalc2 interface are located
namespace CsScriptTest;

public interface ICalc
{
    int Sum(int a, int b);
}

public interface ICalc2
{
    int Sum(int a, int b);

    int Div(int a, int b);
}

public class Samples
{
    public static void LoadCode()
    {
        // In order to be able to debug the code (step inside the code)
        CSScript.EvaluatorConfig.PdbFormat = Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Embedded;
        CSScript.EvaluatorConfig.DebugBuild = true;

        // If we are using a namespace for the interface ICalc (like namespace CsScriptTest in this file)
        // We MUST indicate "using CsScriptTest;" or the code won't load.
        // If the interface were not in the namespace like the original file, we do'nt have to use "using CsScriptTest;"
        dynamic calc = CSScript.Evaluator
                               .LoadCode(
                                   @"using System;
                                     using CsScriptTest;
                                     public class Script : ICalc
                                     {
                                         public int Sum(int a, int b)
                                         {
                                             return a+b;
                                         }
                                     }");
        int result = calc.Sum(1, 2);
    }

    public static void LoadFile()
    {
        // In order to be able to debug the code (step inside the code)
        CSScript.EvaluatorConfig.PdbFormat = Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Embedded;
        CSScript.EvaluatorConfig.DebugBuild = true;


        var script = Path.GetTempFileName();
        try
        {
            string content =
                    """
                    using System; 
                    using CsScriptTest;

                    public class Script : ICalc
                    {
                        public int Sum(int a, int b)
                        {
                            return a + b;
                        }
                    }
                    """;

            File.WriteAllText(script, content);

            dynamic calc = CSScript.Evaluator.LoadFile(script);

            int result = calc.Sum(1, 2);
        }
        finally
        {
            File.Delete(script);
        }
    }

    /*
    public static void LoadAndUnload()
    {
        // Based on https://github.com/dotnet/samples/blob/master/core/tutorials/Unloading/Host/Program.cs
        //
        // Limitations: the "accuracy" of the unloading is determined by the quality of the
        // .NET Core's `AssemblyLoadContext` implementation. For example using variable of `dynamic` type to keep the
        // reference to the script object is problematic. Thus the loaded assembly file stays locked
        // even though the runtime successfully reports unloading the assembly and collecting the weak reference
        // of `AssemblyLoadContext` object.
        //
        // However using interfaces (e.g. `ICalc`) or raw Reflection does not exhibit this problem.
        //
        // Having a debugger attached to the process can also affect the outcome of the unloading.

        WeakReference assemblyHost;

        var asmFile = Path.GetFullPath("Script.dll");

        Samples.LoadAndUnloadImpl(asmFile, out assemblyHost);

        // Poll and run GC until the AssemblyLoadContext is unloaded.
        // You don't need to do that unless you want to know when the context
        // got unloaded. You can just leave it to the regular GC.
        for (int i = 0; assemblyHost.IsAlive && (i < 10); i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Console.WriteLine($"Unload success: {!assemblyHost.IsAlive}");

        File.Delete(asmFile); // prove that the assembly is unloaded
    }

    public static void LoadAndUnloadImpl(string asmFile, out WeakReference alcWeakRef)
    {
        CSScript.Evaluator
                .ReferenceAssemblyOf<ICalc>()
                .CompileAssemblyFromCode(
                    @"using System;
                      public class Script : ICalc
                      {
                          public int Sum(int a, int b)
                          {
                              return a+b;
                          }
                      }", asmFile);

        var asm = new UnloadableAssembly();

        alcWeakRef = new WeakReference(asm);

        ICalc script = (ICalc)asm.LoadFromAssemblyPath(asmFile)
                                    .CreateObject("*"); // or `CreateInstance("css_root+Script")`

        int result = script.Sum(1, 3);

        asm.Unload();
    }

    class UnloadableAssembly : AssemblyLoadContext
    {
        public UnloadableAssembly(string name = null) : base(name ?? Guid.NewGuid().ToString(), isCollectible: true)
            => this.Unloading += x => Console.WriteLine("Unloading " + this.Name);
    }

    public static void LoadMethod()
    {
        dynamic script = CSScript.RoslynEvaluator
                                 .LoadMethod(
                                     @"int Product(int a, int b)
                                       {
                                           return a * b;
                                       }");

        int result = script.Product(3, 2);
    }

    public static void LoadMethodWithInterface()
    {
        ICalc2 script = CSScript.RoslynEvaluator
                                .LoadMethod<ICalc2>(
                                    @"public int Sum(int a, int b)
                                      {
                                          return a + b;
                                      }
                                      public int Div(int a, int b)
                                      {
                                          return a/b;
                                      }");
        int result = script.Sum(15, 3);
    }
    */

    public static void CreateDelegate()
    {
        // In order to be able to debug the code (step inside the code)
        CSScript.EvaluatorConfig.PdbFormat = Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Embedded;
        CSScript.EvaluatorConfig.DebugBuild = true;


        var log = CSScript.RoslynEvaluator
                          .CreateDelegate(@"void Log(string message)
                                            {
                                                Console.WriteLine(message);
                                            }");

        /*
         * With debugger (F11), we can see the generated code :
         * 
        using System;

        public class DynamicClass
        {
            public static void Log(string message)
            {
                Console.WriteLine(message);
            }
        }
        */

        log("Test message");
    }

    public static void LoadCodeWithInterface()
    {
        // In order to be able to debug the code (step inside the code)
        CSScript.EvaluatorConfig.PdbFormat = Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Embedded;
        CSScript.EvaluatorConfig.DebugBuild = true;

        string code = @"
                using System;
                public class Script : ICalc
                {
                    public int Sum(int a, int b)
                    {
                        return a + b;
                    }
                }";

        var script = CSScript.Evaluator.LoadCode<ICalc>(code);

        int result = script.Sum(13, 2);
    }

    public static void CompileCode()
    {
        // In order to be able to debug the code (step inside the code)
        CSScript.EvaluatorConfig.PdbFormat = Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Embedded;
        CSScript.EvaluatorConfig.DebugBuild = true;

        var info = new CompileInfo { 
            //RootClass = "printer_script",
            AssemblyFile = "script.dll" };

        var printer_asm = CSScript.Evaluator
                                  .CompileCode(
                                      @"using System;
                                        public class Printer
                                        {
                                            public static void Print() =>
                                                Console.WriteLine(""Printing..."");
                                        }", info);

        //var type = printer_asm?.GetType("printer_script+Printer");
        var type = printer_asm?.GetType("Printer");
        var method = type?.GetMethod("Print");

        /*
         * In debugger, with F11, we can step in the generated code :
        using System;

        public class Printer
        {
            public static void Print()
            {
                Console.WriteLine("Printing...");
            }
        } 
         */

        method?.Invoke(null, null);
    }

    public static void ScriptReferencingScript()
    {
        // In order to be able to debug the code (step inside the code)
        CSScript.EvaluatorConfig.PdbFormat = Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Embedded;
        CSScript.EvaluatorConfig.DebugBuild = true;

        // Seems not be used with .net core
        var info = new CompileInfo { RootClass = "printer_script" };

        // In debugger, the object printer_asm has a property DefinedTypes with 1 entry: 
        // Name = "Printer", FullName = "MyGreatApp.Printer"
        // If we don't indicate "namespace MyGreatApp;", the DefinedTypes entry will be :
        // Name = "Printer", FullName = "Printer"

        var printer_asm = CSScript.Evaluator
                                  .CompileCode(
                                      @"using System;
                                        namespace MyGreatApp;
                                        public class Printer
                                        {
                                            public void Print() =>
                                                Console.WriteLine(""Printing..."");
                                        }", info);

        dynamic script = CSScript.Evaluator
                                 .ReferenceAssembly(printer_asm)
                                 .LoadMethod(@"void Test()
                                               {
                                                   new MyGreatApp.Printer().Print();
                                               }");
        
        script.Test();

        /*
        using MyGreatApp;

        public class DynamicClass
        {
            public void Test()
            {
                new Printer().Print();
            }
        }
        */

        /*
        using System;

        namespace MyGreatApp;

        public class Printer
        {
            public void Print()
            {
                Console.WriteLine("Printing...");
            }
        }
        */
    }

    public static void Referencing()
    {
        // In order to be able to debug the code (step inside the code)
        CSScript.EvaluatorConfig.PdbFormat = Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Embedded;
        CSScript.EvaluatorConfig.DebugBuild = true;

        string code = @"
                using System;
                using System.Xml;
                // Needed if ICalc is defined in the namespace
                using CsScriptTest;

                public class Script : ICalc
                {
                    public int Sum(int a, int b)
                    {
                        return a + b;
                    }
                }";

        var script = CSScript.Evaluator
                             .ReferenceAssembliesFromCode(code)
                             .ReferenceAssembly(Assembly.GetExecutingAssembly())
                             .ReferenceAssembly(Assembly.GetExecutingAssembly().Location)
                             .ReferenceAssemblyByName("System")
                             .ReferenceAssemblyByNamespace("System.Xml")
                             .TryReferenceAssemblyByNamespace("Fake.Namespace", out var resolved)
                             .ReferenceAssemblyOf(new Samples())
                             .ReferenceAssemblyOf<XDocument>()
                             .ReferenceDomainAssemblies()
                             .LoadCode<ICalc>(code);

        int result = script.Sum(13, 2);

        /*
         * Generated code :
         
using CsScriptTest;

public class Script : ICalc
{
	public int Sum(int a, int b)
	{
		return a + b;
	}
}
*/
    }
}