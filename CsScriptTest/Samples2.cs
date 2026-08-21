using CSScriptLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CSScripting;

// Code commes from comment in EvaluatorBase.cs

namespace CsScriptTest;

public interface ICalc3
{
    int Sum(int a, int b);
    int Div(int a, int b);
}

public static class Samples2
{
    public static void Test1()
    {
        // In order to be able to debug the code (step inside the code)
        CSScript.EvaluatorConfig.PdbFormat = Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Embedded;
        CSScript.EvaluatorConfig.DebugBuild = true;


        var info = new CompileInfo
        {
            AssemblyFile = @"C:\temp\asm.dll"
        };

        var code = @"using System;
                       public class Script
                       {
                           public int Sum(int a, int b)
                           {
                               return a+b;
                           }
                       }";

        Assembly asm = CSScript.Evaluator.CompileCode(code, info);

        // info is optional, so you can omit it if you don't need to specify the assembly file
        // Assembly asm = CSScript.Evaluator.CompileCode(code);

        dynamic script = asm.CreateObject("*");
        var result = script.Sum(7, 3);
    }

    public static void Test2()
    {
        string content =
                """
                using System;
                public class Script
                {
                    public int Sum(int a, int b)
                    {
                        return a+b;
                    }
                }
                """;

        File.WriteAllText("script.cs", content);

        Assembly asm = CSScript.Evaluator
                            .CompileFile("script.cs");

        dynamic script = asm.CreateObject("*");
        var result = script.Sum(7, 3);
    }

    // Script caching is disabled by default for Roslyn evaluator to avoid the side effects.

    public static void Test3()
    {
        string content =
                """
                using System;
                public class Script
                {
                    public int Sum(int a, int b)
                    {
                        return a+b;
                    }
                }
                """;

        File.WriteAllText("MyScript.cs", content);

        string asmFile = CSScript.Evaluator
                              .CompileAssemblyFromFile(
                                     "MyScript.cs", new CompileInfo { AssemblyFile = "MyScript.dll" });

        var assembly = Assembly.LoadFrom(asmFile);
        dynamic script = assembly.CreateObject("*");
        var result = script.Sum(7, 3);
    }

    public static void Test4()
    {
        string asmFile = CSScript.Evaluator
                                 .CompileAssemblyFromCode(
                                        @"using System;
                                               public class Script
                                               {
                                                   public int Sum(int a, int b)
                                                   {
                                                       return a+b;
                                                   }
                                               }",
        new CompileInfo { AssemblyFile = "MyScript2.dll" });

        var assembly = Assembly.LoadFrom(asmFile);
        dynamic script = assembly.CreateObject("*");
        var result = script.Sum(7, 3);
    }

    public static void Test5()
    {
        // Compiles the specified script text without loading it into the
        // AppDomain or writing to the file system.

        try
        {
            var info = new CompileInfo { CompilerOptions = "/unsafe" };
            CSScript.Evaluator
                        .CheckCode(@"using System;
                                  public class Script
                                  {
                                      public int Sum(int a, int b)
                                      {
                                          // On purpose compilation problem
                                          error
                                          return a+b;
                                      }
                                  }", info);
        }
        catch (Exception e)
        {
            Console.WriteLine("Compile error: " + e.Message);
        }
    }

    public static void Test6()
    {
        try
        {
            string content =
                """
                using System;
                public class Script
                {
                    public int Sum(int a, int b)
                    {
                        return a+b;
                    }
                }
                """;

            File.WriteAllText("script.cs", content);

            var info = new CompileInfo { CompilerOptions = "/unsafe" };
            CSScript.Evaluator
                        .CheckFile("script.cs", info);
        }
        catch (Exception e)
        {
            Console.WriteLine("Compile error: " + e.Message);
        }
    }

    public static void Test7()
    {
        // In order to be able to debug the code (step inside the code)
        CSScript.EvaluatorConfig.PdbFormat = Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Embedded;
        CSScript.EvaluatorConfig.DebugBuild = true;

        //Wraps C# code fragment into auto-generated class (type name
        // <c>DynamicClass</c>) and evaluates it.

        // This method is a logical equivalent of
        // <see cref="CSScriptLib.IEvaluator.CompileCode"/> but is allows you to
        // define your script class by specifying class method instead of whole
        // class declaration.

        var info = new CompileInfo { AssemblyFile = "Printer.dll", AssemblyName = "PrintAsm" };

        // "css_root" is the defined type
        var assembly = CSScript.RoslynEvaluator
                                  .With(eval => eval.IsAssemblyUnloadingEnabled = true)
                                  .CompileMethod(@"int Sum(int a, int b)
                                                    {
                                                        return a+b;
                                                    }"
                                                   , info);
        dynamic script = assembly.CreateObject("*");

        var result = script.Sum(7, 3);

        assembly.Unload();

        /* In debugger with F11, we can see the generated code.
        
        public class css_root
        {
            public int Sum(int a, int b)
            {
                return a + b;
            }
        } 
         */
    }

    public static void Test8()
    {
        var log = CSScript.Evaluator
                       .CreateDelegate(@"void Log(string message)
                                             {
                                                 Console.WriteLine(message);
                                             }");

        log("Test message");

        var product = CSScript.RoslynEvaluator
                           .CreateDelegate<int>(@"int Product(int a, int b)
                               {
                                   return a * b;
                               }");

        int result = product(3, 2);
    }


    public static void Test9()
    {
        dynamic script = CSScript.RoslynEvaluator
                               .LoadCode(@"using System;
                                               public class Script
                                               {
                                                   public int Sum(int a, int b)
                                                   {
                                                       return a+b;
                                                   }
                                               }");
        int result = script.Sum(1, 2);
    }

    public static void Test10()
    {
        // In order to be able to debug the code (step inside the code)
        CSScript.EvaluatorConfig.PdbFormat = Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Embedded;
        CSScript.EvaluatorConfig.DebugBuild = true;

        dynamic script = CSScript.RoslynEvaluator
                              .LoadMethod(@"int Product(int a, int b)
                                  {
                                      return a * b;
                                  }");

        int result = script.Product(3, 2);

        /*
         * Generated code (use F11 to step in the Product() call)
         * 
public class DynamicClass
{
	public int Product(int a, int b)
	{
		return a * b;
	}
}
         */
    }

    public static void Test11()
    {
        // In order to be able to debug the code (step inside the code)
        CSScript.EvaluatorConfig.PdbFormat = Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Embedded;
        CSScript.EvaluatorConfig.DebugBuild = true;

        ICalc3 script = CSScript.RoslynEvaluator
                               .LoadMethod<ICalc3>(@"public int Sum(int a, int b)
                                 {
                                     return a + b;
                                 }
                                 public int Div(int a, int b)
                                 {
                                     return a/b;
                                 }");

        int result1 = script.Sum(15, 3);
        int result2 = script.Div(15, 3);

        /*
         * Generated code
         * 
         * 
    using CsScriptTest;

    public class DynamicClass : ICalc3
    {
        public int Sum(int a, int b)
        {
            return a + b;
        }

        public int Div(int a, int b)
        {
            return a / b;
        }
    }
         */

    }


}
