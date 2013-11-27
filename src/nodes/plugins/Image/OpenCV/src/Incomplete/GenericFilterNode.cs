using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;

namespace VVVV.Nodes.OpenCV
{
    public class FilterNode<TFilterInstance> : IFilterNode<TFilterInstance> 
        where TFilterInstance : IFilterInstance, new()
    {
        private readonly List<PropertyInfo> FInputProperties = new List<PropertyInfo>();
        private readonly List<PropertyInfo> FOutputProperties = new List<PropertyInfo>();
        private readonly List<IDiffSpread> FInputs = new List<IDiffSpread>();
        private readonly List<object> FOutputs = new List<object>();

        // Called by our node factory
        public FilterNode(IIOFactory factory)
        {
            // Create in and outputs based on properties of the filter instance
            var filterInstanceType = typeof(TFilterInstance);
            FInputProperties = filterInstanceType.GetProperties().Where(p => p.GetCustomAttributes(typeof(InputAttribute), true).Any()).ToList();
            FOutputProperties = filterInstanceType.GetProperties().Where(p => p.GetCustomAttributes(typeof(OutputAttribute), true).Any()).ToList();
            foreach (var property in FInputProperties.Concat(FOutputProperties))
            {
                var attribute = CreateInputOrOutputAttribute(property.GetCustomAttributes(typeof(InputAttribute), true).First());
                var spreadType = typeof(IDiffSpread<>).MakeGenericType(property.PropertyType);
                var spread = factory.CreateIO(spreadType, attribute) as IDiffSpread;
                FInputs.Add(spread);
            }
        }

        static IOAttribute CreateInputOrOutputAttribute(object customAttribute)
        {
            if (customAttribute.GetType().FullName.Contains("Input"))
                return new InputAttribute("Foo");
            return null;
        }

        protected override void Update(int InstanceCount, bool SpreadChanged)
        {
            for (int inputIndex = 0; inputIndex < FInputProperties.Count; inputIndex++)
            {
                var input = FInputs[inputIndex];
                if (input.IsChanged || SpreadChanged)
                {
                    var inputProperty = FInputProperties[inputIndex];
                    for (int i = 0; i < InstanceCount; i++)
                    {
                        var filterInstance = FProcessor[i];
                        inputProperty.SetValue(filterInstance, input[i], null);
                    }
                }
            }
        }
    }
}
