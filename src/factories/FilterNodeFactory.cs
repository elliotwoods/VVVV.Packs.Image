using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VVVV.Hosting.Factories;
using VVVV.Hosting.Interfaces;
using VVVV.Hosting.IO;
using VVVV.Nodes.OpenCV;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.Collections;

namespace VVVV.CV.Factories
{
    [Export(typeof(IAddonFactory))]
    public class FilterNodeFactory : AbstractFileFactory<IInternalPluginHost>
    {
#pragma warning disable 0649
        [Import]
        private IORegistry FIORegistry;

        [Import]
        private DotNetPluginFactory FPluginFactory;
#pragma warning restore

        private readonly Dictionary<IPluginBase, PluginContainer> FFilterNodes;
        private readonly CompositionContainer FParentContainer;
        private readonly Type FReflectionOnlyIFilterInstanceType;

        [ImportingConstructor]
        public FilterNodeFactory(CompositionContainer parentContainer)
            : base(".dll")
        {
            FParentContainer = parentContainer;
            FFilterNodes = new Dictionary<IPluginBase, PluginContainer>();

            var cvCoreAssemblyName = typeof(IFilterInstance).Assembly.FullName;
            var cvCoreAssembly = Assembly.ReflectionOnlyLoad(cvCoreAssemblyName);
            FReflectionOnlyIFilterInstanceType = cvCoreAssembly.GetExportedTypes()
                .Where(t => t.Name == typeof(IFilterInstance).Name)
                .First();
        }

        protected override bool CreateNode(INodeInfo nodeInfo, IInternalPluginHost nodeHost)
        {
            var ioFactory = new IOFactory(nodeHost, FIORegistry, FParentContainer, FNodeInfoFactory, FPluginFactory);
            
            var filterInstanceType = Type.GetType(nodeInfo.Arguments);
            var genericFilterNodeType = typeof(FilterNode<>);
            var filterNodeType = genericFilterNodeType.MakeGenericType(filterInstanceType);
            var filterNodeTypeCtor = filterNodeType.GetConstructor(new [] { typeof(IIOFactory) });
            var filterNode = filterNodeTypeCtor.Invoke(new object [] { ioFactory }) as IPluginBase;

            nodeHost.Plugin = filterNode;

            return true;
        }

        protected override bool DeleteNode(INodeInfo nodeInfo, IInternalPluginHost nodeHost)
        {
            throw new NotImplementedException();
        }

        public override string JobStdSubPath
        {
            get { return "filters"; }
        }

        protected override IEnumerable<INodeInfo> LoadNodeInfos(string filename)
        {
            var assembly = Assembly.ReflectionOnlyLoadFrom(filename);
            foreach (var type in assembly.GetExportedTypes())
            {
                if (!type.IsAbstract && !type.IsGenericTypeDefinition && FReflectionOnlyIFilterInstanceType.IsAssignableFrom(type))
                {
                    var attribute = GetFilterInstanceAttributeData(type);

                    if (attribute != null)
                    {
                        var nodeInfo = ExtractNodeInfoFromAttributeData(attribute, filename);
                        nodeInfo.Arguments = type.FullName;
                        nodeInfo.Type = NodeType.Plugin;
                        nodeInfo.Factory = this;
                        nodeInfo.CommitUpdate();
                        yield return nodeInfo;
                    }
                }
            }
        }

        private static CustomAttributeData GetFilterInstanceAttributeData(Type type)
        {
            return CustomAttributeData.GetCustomAttributes(type)
                .Where(ca => ca.Constructor.DeclaringType.FullName == typeof(FilterInstanceAttribute).FullName)
                .FirstOrDefault();
        }

        private INodeInfo ExtractNodeInfoFromAttributeData(CustomAttributeData attribute, string filename)
        {
            throw new NotImplementedException();
            //var namedArguments = new Dictionary<string, object>();
            //foreach (var namedArgument in attribute.NamedArguments)
            //{
            //    namedArguments[namedArgument.MemberInfo.Name] = namedArgument.TypedValue.Value;
            //}

            //var nodeInfo = FNodeInfoFactory.CreateNodeInfo(
            //    (string)namedArguments.ValueOrDefault("Name"),
            //    (string)namedArguments.ValueOrDefault("Category"),
            //    (string)namedArguments.ValueOrDefault("Version"),
            //    filename,
            //    true);

            //namedArguments.Remove("Name");
            //namedArguments.Remove("Category");
            //namedArguments.Remove("Version");

            //if (namedArguments.ContainsKey("InitialWindowWidth") && namedArguments.ContainsKey("InitialWindowHeight"))
            //{
            //    nodeInfo.InitialWindowSize = new Size((int)namedArguments["InitialWindowWidth"], (int)namedArguments["InitialWindowHeight"]);
            //    namedArguments.Remove("InitialWindowWidth");
            //    namedArguments.Remove("InitialWindowHeight");
            //}

            //if (namedArguments.ContainsKey("InitialBoxWidth") && namedArguments.ContainsKey("InitialBoxHeight"))
            //{
            //    nodeInfo.InitialBoxSize = new Size((int)namedArguments["InitialBoxWidth"], (int)namedArguments["InitialBoxHeight"]);
            //    namedArguments.Remove("InitialBoxWidth");
            //    namedArguments.Remove("InitialBoxHeight");
            //}

            //if (namedArguments.ContainsKey("InitialComponentMode"))
            //{
            //    nodeInfo.InitialComponentMode = (TComponentMode)namedArguments["InitialComponentMode"];
            //    namedArguments.Remove("InitialComponentMode");
            //}

            //foreach (var entry in namedArguments)
            //{
            //    nodeInfo.GetType().InvokeMember((string)entry.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty, Type.DefaultBinder, nodeInfo, new object[] { entry.Value });
            //}

            //return nodeInfo;
        }
    }
}
