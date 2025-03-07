﻿#if NETSTANDARD2_0 || NETSTANDARD2_1 || NETCOREAPP
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.CSharp;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using FastReport.Code.CodeDom.Compiler;

namespace FastReport.Code.CSharp
{
    public class CSharpCodeProvider : CodeDomProvider
    {


        public override CompilerResults CompileAssemblyFromSource(CompilerParameters cp, string code)
        {
#if DEBUG
            Console.WriteLine("FR.Compat: " +
#if NETSTANDARD
                "NETSTANDARD"
#elif NETCOREAPP
                "NETCOREAPP"
#endif
                );
#endif

            SyntaxTree codeTree = CSharpSyntaxTree.ParseText(code);
            CSharpCompilationOptions options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                generalDiagnosticOption: ReportDiagnostic.Default,
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default,
                reportSuppressedDiagnostics: true);

            List<MetadataReference> references = new List<MetadataReference>();

            AddReferences(cp, references);


            Compilation compilation = CSharpCompilation.Create(
                "_" + Guid.NewGuid().ToString("D"), new SyntaxTree[] { codeTree },
                references: references, options: options
                );


            OnBeforeEmitCompilation(compilation);

            using (MemoryStream ms = new MemoryStream())
            {
                EmitResult results = compilation.Emit(ms);
                if (results.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    var compiledAssembly = ms.ToArray();                
                    Assembly assembly;
                    using (var asm = new MemoryStream(compiledAssembly))
                    {
                        if (AssemblyLoadContext.CurrentContextualReflectionContext != null)
                        {
                            assembly =
                                AssemblyLoadContext.CurrentContextualReflectionContext.LoadFromStream(asm);
                        }
                        else
                        {
                            assembly = AssemblyLoadContext.Default.LoadFromStream(asm);
                        }
                    }

                    // assembly = Assembly.Load(ms.ToArray());
                    return new CompilerResults()
                    {
                        CompiledAssembly = assembly
                    };
                }
                else
                {
                    CompilerResults result = new CompilerResults();
                    foreach (Diagnostic d in results.Diagnostics)
                    {
                        if (d.Severity == DiagnosticSeverity.Error)
                        {
                            result.Errors.Add(new CompilerError()
                            {
                                ErrorText = d.GetMessage(),
                                ErrorNumber = d.Id,
                                Line = d.Location.GetLineSpan().StartLinePosition.Line,
                                Column = d.Location.GetLineSpan().StartLinePosition.Character,

                            });
                        }
                    }
                    return result;
                }
            }

        }

        public override void Dispose()
        {
            
        }

    }
}
#endif