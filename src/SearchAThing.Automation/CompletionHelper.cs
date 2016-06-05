#region SearchAThing.Automation, Copyright(C) 2016 Lorenzo Delana, License under MIT
/*
* The MIT License(MIT)
* Copyright(c) 2016 Lorenzo Delana, https://searchathing.com
*
* Permission is hereby granted, free of charge, to any person obtaining a
* copy of this software and associated documentation files (the "Software"),
* to deal in the Software without restriction, including without limitation
* the rights to use, copy, modify, merge, publish, distribute, sublicense,
* and/or sell copies of the Software, and to permit persons to whom the
* Software is furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
* FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
* DEALINGS IN THE SOFTWARE.
*/
#endregion

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using SearchAThing.Automation;
using SearchAThing.Core;
using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SearchAThing.Automation
{

    public class CompletionHelper
    {

        #region mef host svc
        static MefHostServices _mef = null;
        static MefHostServices mef
        {
            get
            {
                if (_mef == null)
                {
                    var assemblies = new[]
                    {
                        Assembly.Load("Microsoft.CodeAnalysis"),
                        Assembly.Load("Microsoft.CodeAnalysis.CSharp"),
                        Assembly.Load("Microsoft.CodeAnalysis.Features"),
                        Assembly.Load("Microsoft.CodeAnalysis.CSharp.Features"),
                        Assembly.Load(typeof(object).Assembly.GetName().Name),
                        Assembly.Load(typeof(Console).Assembly.GetName().Name),
                        Assembly.Load(typeof(TaskScheduler).Assembly.GetName().Name),
                        Assembly.Load(typeof(HostWorkspaceServices).Assembly.GetName().Name)
                    };

                    var cctx = new ContainerConfiguration()
                        .WithParts(MefHostServices.DefaultAssemblies.Concat(assemblies).Distinct()
                        .SelectMany(x => { Type[] types = new Type[] { }; try { types = x.GetTypes(); } catch { } return types; })
                        .ToArray())
                        .CreateContainer();

                    _mef = MefHostServices.Create(cctx);
                }
                return _mef;
            }
        }
        #endregion

        static object typesLck = new object();
        static HashSet<string> assemblyLoaded = new HashSet<string>();
        internal static Dictionary<string, Type> types = new Dictionary<string, Type>();

        static void ResolveAssemblyTypes(Assembly[] assemblies)
        {
            lock (typesLck)
            {
                var q = assemblies.Where(r => !assemblyLoaded.Contains(r.FullName));
                foreach (var x in q)
                {
                    foreach (var t in x.DefinedTypes)
                    {
                        if (!types.ContainsKey(t.FullName)) types.Add(t.FullName, t);
                    }

                    assemblyLoaded.Add(x.FullName);
                }
            }
        }

        public static async Task<IEnumerable<CompletionInfo>> AutoComplete(string src, int off, Assembly[] additionalAssemblies = null)
        {
            return await Task.Run(async () =>
            {
                var wksp = new AdhocWorkspace(mef);
                var sol = wksp.CurrentSolution.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "prj", "prj", LanguageNames.CSharp));
                var prj = sol.Projects.First();

                var assemblies = new[] { typeof(object).Assembly };
                if (additionalAssemblies != null)
                    assemblies = assemblies.Union(additionalAssemblies).Distinct().ToArray();
                ResolveAssemblyTypes(assemblies);
                prj = prj.AddMetadataReferences(assemblies.Select(w => MetadataReference.CreateFromFile(w.Location)));
                var doc = prj.AddDocument("doc", SourceText.From(src));

                var svc = CompletionService.GetService(doc);
                var text = await doc.GetTextAsync();

                var ci = await svc.GetCompletionsAsync(doc, off);

                if (ci == null) return new List<CompletionInfo>();

                var res = ci.Items.Select(g => new CompletionInfo(g));

                return res;
            });
        }

    }

    public class CompletionInfo
    {

        public CompletionItem CompletionItem { get; private set; }

        bool Extract(ref string s, string prefix, out string part)
        {
            part = null;
            if (!s.Contains(prefix)) return false;

            var idx = s.IndexOf(prefix);
            int j = idx + prefix.Length;
            part = "";
            while (j < s.Length && s[j] != '.') part += s[j++];

            if (prefix.Length + part.Length == s.Length)
                s = "";
            else
                s = s.Substring(0, idx) + s.Substring(j + 1, s.Length - j - 1);

            return true;
        }

        public CompletionInfo(CompletionItem completionItem)
        {
            CompletionItem = completionItem;

            var t = CompletionItem.Properties.First(w => w.Key == "Symbols").Value;

            if (t.Contains("|")) t = t.Split('|').First();
            if (t.Contains("(")) t = t.Substring(0, t.IndexOf('('));

            var s = "";
            Namespace = "";
            while (Extract(ref t, "N:", out s))
            {
                if (Namespace.Length > 0) Namespace += ".";
                Namespace += s;
            }

            Extract(ref t, "T:", out s);
            Name = s;

            switch (MemberType)
            {
                case "Property":
                    Extract(ref t, "P:", out s);
                    MemberName = s;
                    break;

                case "Method":
                    Extract(ref t, "M:", out s);
                    MemberName = s;
                    break;

                case "Event":
                    Extract(ref t, "E:", out s);
                    MemberName = s;
                    break;
            }
        }

        public string CompletionText { get { return CompletionItem.DisplayText; } }

        public string Namespace { get; private set; }
        public string Name { get; private set; }
        public string TypeName { get { return (Namespace + "." + Name).StripEnd('.'); } }
        public string MemberName { get; private set; }
        public string MemberType { get { return CompletionItem.Tags.First(); } }
        public string MemberFullname { get { return Namespace + "." + Name + "." + MemberName; } }

        public PropertyInfo PropertyInfo
        {
            get
            {
                if (MemberType != "Property") return null;

                return TypeInfo.GetProperty(MemberName);
            }
        }

        public IEnumerable<MethodInfo> MethodInfo
        {
            get
            {
                if (MemberType != "Method") return null;

                return TypeInfo.GetMethods().Where(r => r.Name == MemberName);
            }
        }

        public EventInfo EventInfo
        {
            get
            {
                if (MemberType != "Event") return null;

                return TypeInfo.GetEvent(MemberName);
            }
        }

        public Type TypeInfo
        {
            get
            {
                return CompletionHelper.types[TypeName];
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            switch (MemberType)
            {
                case "Property":
                    {
                        var pi = PropertyInfo;

                        sb.Append($"[p]\t{pi.PropertyType.Name} {MemberName} {{ ");
                        var hasGet = false;
                        if (pi.GetMethod != null) { sb.Append("get"); hasGet = true; }
                        if (pi.SetMethod != null) { if (hasGet) sb.Append(";"); sb.Append("set"); }
                        sb.Append(" }");
                    }
                    break;

                case "Method":
                    {
                        var mis = MethodInfo.ToArray();

                        for (int j = 0; j < mis.Length; ++j)
                        {
                            var mi = mis[j];
                            sb.Append($"[m]\t{mi.ReturnType.Name} {MemberName}(");

                            var ps = mi.GetParameters();
                            for (int i = 0; i < ps.Length; ++i)
                            {
                                var p = ps[i];

                                sb.Append($"{p.ParameterType.Name} {p.Name}");
                                if (i != ps.Length - 1) sb.Append(", ");
                            }

                            sb.Append($")");
                            if (j < mis.Length - 1) sb.AppendLine();
                        }
                    }
                    break;

                case "Event":
                    {
                        sb.AppendLine($"[e]\t{EventInfo.EventHandlerType.Name} {MemberName}");
                    }
                    break;

                default:
                    sb.AppendLine("[unknown]");
                    break;
            }

            return sb.ToString();
        }

    }

}

namespace SearchAThing
{

    public static partial class Extensions
    {

        public static Task<IEnumerable<CompletionInfo>> AutoComplete(this string src, int off, Assembly[] additionalAssemblies = null)
        {
            return CompletionHelper.AutoComplete(src, off, additionalAssemblies);
        }

    }

}
