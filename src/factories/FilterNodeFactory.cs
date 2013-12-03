using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VVVV.Hosting.Factories;
using VVVV.Hosting.Interfaces;
using VVVV.Hosting.IO;
using VVVV.CV.Core;
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

        private readonly CompositionContainer FParentContainer;
        private Type FReflectionOnlyIFilterInstanceType;

        [ImportingConstructor]
        public FilterNodeFactory(CompositionContainer parentContainer)
            : base(".dll")
        {
            FParentContainer = parentContainer;
        }

        protected override bool CreateNode(INodeInfo nodeInfo, IInternalPluginHost nodeHost)
        {
            var assemblyLocation = nodeInfo.Filename;
            var assembly = Assembly.LoadFrom(assemblyLocation);
            var filterInstanceType = assembly.GetType(nodeInfo.Arguments);
            var genericFilterNodeType = typeof(FilterNode<>);
            var filterNodeType = genericFilterNodeType.MakeGenericType(filterInstanceType);
            var pluginContainer = new PluginContainer(nodeHost, FIORegistry, FParentContainer, FNodeInfoFactory, FPluginFactory, filterNodeType, nodeInfo);
            nodeHost.Plugin = pluginContainer;
            return true;
        }

        protected override bool DeleteNode(INodeInfo nodeInfo, IInternalPluginHost nodeHost)
        {
            var pluginContainer = nodeHost.Plugin as PluginContainer;
            pluginContainer.Dispose();
            return true;
        }

        public override string JobStdSubPath
        {
            get { return "plugins"; }
        }

        protected override IEnumerable<INodeInfo> LoadNodeInfos(string filename)
        {
            if (!IsDotNetAssembly(filename)) yield break;
            if (FReflectionOnlyIFilterInstanceType == null)
            {
                // Can't get it to load in constructor
                var cvCoreAssemblyName = typeof(IFilterInstance).Assembly.FullName;
                var cvCoreAssembly = Assembly.ReflectionOnlyLoad(cvCoreAssemblyName);
                FReflectionOnlyIFilterInstanceType = cvCoreAssembly.GetExportedTypes()
                    .Where(t => t.Name == typeof(IFilterInstance).Name)
                    .First();
            }

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
            var name = attribute.ConstructorArguments[0].Value as string;
            //var namedArguments = new Dictionary<string, object>();
            //foreach (var namedArgument in attribute.NamedArguments)
            //{
            //    namedArguments[namedArgument.MemberInfo.Name] = namedArgument.TypedValue.Value;
            //}

            var nodeInfo = FNodeInfoFactory.CreateNodeInfo(
                name,
                "CV.Image",
                "",
                filename,
                true);

            //foreach (var entry in namedArguments)
            //{
            //    nodeInfo.GetType().InvokeMember((string)entry.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty, Type.DefaultBinder, nodeInfo, new object[] { entry.Value });
            //}

            return nodeInfo;
        }

        // TODO: Should be a utility function in VVVV.Utils
        // From http://www.anastasiosyal.com/archive/2007/04/17/3.aspx
        private static bool IsDotNetAssembly(string fileName)
        {
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    using (var binReader = new BinaryReader(fs))
                    {
                        try
                        {
                            fs.Position = 0x3C; //PE Header start offset
                            uint headerOffset = binReader.ReadUInt32();

                            fs.Position = headerOffset + 0x18;
                            UInt16 magicNumber = binReader.ReadUInt16();

                            int dictionaryOffset;
                            switch (magicNumber)
                            {
                                case 0x010B: dictionaryOffset = 0x60; break;
                                case 0x020B: dictionaryOffset = 0x70; break;
                                default:
                                    throw new BadImageFormatException("Invalid Image Format");
                            }

                            //position to RVA 15
                            fs.Position = headerOffset + 0x18 + dictionaryOffset + 0x70;


                            //Read the value
                            uint rva15value = binReader.ReadUInt32();
                            return rva15value != 0;
                        }
                        finally
                        {
                            binReader.Close();
                        }
                    }
                }
                finally
                {
                    fs.Close();
                }

            }
        }
    }
}
