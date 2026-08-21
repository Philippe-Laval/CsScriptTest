using CSScripting;
using CSScriptLib;
using CsScriptTest;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace CsScriptTest;

public class Program
{
    static void Main(string[] args)
    {
        Samples2.Test1();
        Samples2.Test2();
        Samples2.Test3();
        Samples2.Test4();
        Samples2.Test5();
        Samples2.Test6();
        Samples2.Test7();
        Samples2.Test8();
        Samples2.Test9();
        Samples2.Test10();
        Samples2.Test11();

        PrepareCodeDomCompilers();

        Samples.LoadAndUnload();
        Samples.LoadCode();
        Samples.LoadFile();
        Samples.CreateDelegate();
        Samples.LoadCodeWithInterface();
        Samples.CompileCode();
        Samples.ScriptReferencingScript();
        Samples.Referencing();
        Samples.LoadMethod();
        Samples.LoadMethodWithInterface();


        TestRoslynEvaluatorLoadMethod();
        TestEvaluatorLoadMethodWithInterface();
        Test_CodeDom();
        Test_Roslyn();
        Test();
        TestEvaluatorCompileCodeAndLoadMethodWithInterface();


    }

    private static void TestRoslynEvaluatorLoadMethod()
    {
        // In order to be able to debug the code (step inside the code)
        CSScript.EvaluatorConfig.PdbFormat = Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Embedded;
        CSScript.EvaluatorConfig.DebugBuild = true;

        dynamic script = CSScript.RoslynEvaluator
                                 .LoadMethod(@"public (int, int) func()
                                                   {
                                                       return (0,5);
                                                   }");

        /*
         * In debugger F11 (step in) and we will see the code executed :
public class DynamicClass
{
	public (int, int) func()
	{
		return (0, 5);
	}
}
         */

        (int, int) result = script.func();
    }

    private static void TestEvaluatorCompileCodeAndLoadMethodWithInterface()
    {
        // In order to be able to debug the code (step inside the code)
        CSScript.EvaluatorConfig.PdbFormat = Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Embedded;
        CSScript.EvaluatorConfig.DebugBuild = true;

        var info = new CompileInfo { 
            //RootClass = "Printing",
            AssemblyFile = "Printer3.dll", 
            AssemblyName = "Print3Asm" 
        };

        // in debuger, we see that the object printer_asm has one entry in DefinedTypes
        // Name = "Printer", FullName = "MyGreatApp.Printer"

        
        var printer_asm = CSScript.Evaluator
                                  .CompileCode(@"using System;
                                                     namespace MyGreatApp;
                                                     public class Printer
                                                     {
                                                         public static void Print() =>
                                                             Console.WriteLine(""Printing..."");
                                                     }", info);

        var script = CSScript.Evaluator
                             .With(eval => eval.IsAssemblyUnloadingEnabled = true)
                             .ReferenceAssembly(printer_asm)
                             .LoadMethod<ICalc>(@"public int Sum(int a, int b)
                                                     {
                                                         MyGreatApp.Printer.Print();
                                                         return a+b;
                                                     }");

        // Using the debugger, we step in Sum() to see the generated code below.
        // We see that :
        // - using LoadMethod<ICalc>() will generate the "using Client.NET;" line. Its the namespace where the ICalc is defined.
        // - MyGreatApp.Printer.Print(); will generate "using MyGreatApp;" and "Printer.Print();"

        script.Sum(1, 2);
        script.GetType().Assembly.Unload();

        /*
        using Client.NET;
        using MyGreatApp;

        public class DynamicClass : ICalc
        {
            public int Sum(int a, int b)
            {
                Printer.Print();
                return a + b;
            }
        }
        */

        // We can then step in the Printer.Print() to see the generated code below.

        /*
        using System;

        namespace MyGreatApp;

        public class Printer
        {
            public static void Print()
            {
                Console.WriteLine("Printing...");
            }
        }
        */
    }

    static void TestEvaluatorLoadMethodWithInterface()
    {
        // In order to be able to debug the code (step inside the code)
        CSScript.EvaluatorConfig.PdbFormat = Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Embedded;
        CSScript.EvaluatorConfig.DebugBuild = true;

        var script = CSScript.Evaluator
                             .With(eval => eval.IsAssemblyUnloadingEnabled = true)
                             .LoadMethod<ICalc>(@"public int Sum(int a, int b)
                                                     { return a+b; }");


        /*
         * In debugger F11 (step in) and we will see the code executed.
         * We see that .LoadMethod<ICalc> we generate the line "using Client.NET;" because ICalc is defined in this namespace.
         * 
using Client.NET;

public class DynamicClass : ICalc
{
	public int Sum(int a, int b)
	{
		return a + b;
	}
}
         */
        script.Sum(1, 2);

        script.GetType().Assembly.Unload();
    }


    private static void Test()
    { 
        PrepareCodeDomCompilers();

        Console.WriteLine("================\n");
        Console.WriteLine($"Loading and unloading script 20 times");
        Test_Unloading();
        Console.WriteLine("================\n");

        CSScript.StopBuildServer();
        CSScript.EvaluatorConfig.DebugBuild = true;

        var sw = Stopwatch.StartNew();

        Console.WriteLine($"Hosting runtime: .NET {(Runtime.IsCore ? "Core" : "Framework")}");
        Console.WriteLine("================\n");

        Console.WriteLine("CodeDOM");
        Test_CodeDom();
        Console.WriteLine("  first run: " + sw.ElapsedMilliseconds);
        sw.Restart();
        Test_CodeDom();
        Console.WriteLine("  next run: " + sw.ElapsedMilliseconds);

        Console.WriteLine("\nRoslyn");
        sw.Restart();
        Test_Roslyn();
        Console.WriteLine("  first run: " + sw.ElapsedMilliseconds);
        sw.Restart();
        Test_Roslyn();
        Console.WriteLine("  next run: " + sw.ElapsedMilliseconds);
    }
    

    static void Test_CodeDom()
    {
        dynamic script = CSScript.CodeDomEvaluator
                                 .LoadMethod(@"public (int, int) func()
                                                   {
                                                       return (0,5);
                                                   }");
        (int, int) result = script.func();
    }

    static void Test_Roslyn()
    {
        dynamic script = CSScript.RoslynEvaluator
                                 .LoadMethod(@"public (int, int) func()
                                                   {
                                                       return (0,5);
                                                   }");

        (int, int) result = script.func();
    }

    static void call_UnloadAssembly()
    {
        CSScript.EvaluatorConfig.PdbFormat = Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Embedded;
        CSScript.EvaluatorConfig.DebugBuild = true;

        var script = CSScript.Evaluator
                             .With(eval => eval.IsAssemblyUnloadingEnabled = true)
                             .LoadMethod<ICalc>(@"public int Sum(int a, int b)
                                                     { return a+b; }");

        script.Sum(1, 2);

        script.GetType().Assembly.Unload();
    }

    static Assembly? printer_asm = null;

    static void call_UnloadAssemblyWithDependency()
    {
        var info = new CompileInfo { RootClass = "Printing", AssemblyFile = "Printer.dll", AssemblyName = "PrintAsm" };

        if (printer_asm == null)
            printer_asm = CSScript.Evaluator
                                  .CompileCode(@"using System;
                                                     public class Printer
                                                     {
                                                         public static void Print() =>
                                                             Console.WriteLine(""Printing..."");
                                                     }", info);

        var script = CSScript.Evaluator
                             .With(eval => eval.IsAssemblyUnloadingEnabled = true)
                             .ReferenceAssembly(printer_asm)
                             .LoadMethod<ICalc>(@"public int Sum(int a, int b)
                                                     {
                                                         Printing.Printer.Print();
                                                         return a+b;
                                                     }");
        script.Sum(1, 2);
        script.GetType().Assembly.Unload();
    }

    static void call_UnloadAssembly_Failing()
    {
        // using 'dynamic` completely breaks CLR unloading mechanism. Most likely it triggers an
        // accidental referencing of the assembly or System.Runtime.Loader.AssemblyLoadContext.
        dynamic script = CSScript.Evaluator
                                 .With(eval => eval.IsAssemblyUnloadingEnabled = true)
                                 .LoadMethod(@"public int Sum(int a, int b)
                                                { return a+b; }");

        script.Sum(1, 2);

        (script as object).GetType().Assembly.Unload();
    }

    static void call_UnloadAssembly_Crashing_CLR()
    {
        CSScript.EvaluatorConfig.PdbFormat = Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Embedded;
        CSScript.EvaluatorConfig.DebugBuild = true;

        var script = CSScript.Evaluator
                             .With(eval => eval.IsAssemblyUnloadingEnabled = true)
                             .LoadMethod<ICalc>(@"public int Sum(int a, int b)
                                                     { return a+b; }");

        script.Sum(1, 2);

        GC.Collect(); // see https://github.com/oleg-shilo/cs-script/issues/301 for details

        script.GetType().Assembly.Unload();
    }

    static void Test_Unloading()
    {
        for (int i = 0; i < 20; i++)
        {
            Console.WriteLine("Loaded assemblies count: " + AppDomain.CurrentDomain.GetAssemblies().Count());

            call_UnloadAssembly();
            //call_UnloadAssemblyWithDependency(); // also works OK; provided just for demo

            // call_UnloadAssembly_Failing();
            // call_UnloadAssembly_Crashing_CLR();
            GC.Collect();
        }
    }

    static void PrepareCodeDomCompilers()
    {
        // If you are using CodeDom evaluator and your hosting environment does not have .NET SDK installed
        // you will need to install the compiler by downloading SDK tools NuGet package.
        // Either manually from
        // https://api.nuget.org/v3-flatcontainer/microsoft.net.sdk.compilers.toolset/10.0.103/microsoft.net.sdk.compilers.toolset.10.0.103.nupkg
        // Or by executing `css -deploy-csc` command in the terminal, which will download and extract the package to the default location.
        // Or by using CS-Script's own downloader. Uncomment next two lines:
        //
        // NugetPackageDownloader.OnProgressOutput = Console.WriteLine;
        // NugetPackageDownloader.DownloadLatestSdkCompiler(includePrereleases: false);
        // This sample works even if you do not uncomment the code above because the compiling tools package is added to this
        // project.

        // Globals.csc is internally initialized the same way. Providing it here for demo purposes only.
        Globals.csc =
            Globals.FindSdKCompiler() ?? // or from .NET SDK installed on OS
            Globals.FindSdkToolsetPackageCompiler(includePrereleases: false); // from the installed Microsoft.Net.Sdk.Compilers.Toolset package
    }
}