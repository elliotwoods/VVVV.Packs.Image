using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;

namespace VVVV.CV.Core
{
    [Export(typeof(IPluginBase))]
    public class FilterNode<TFilterInstance> : IFilterNode<TFilterInstance> 
        where TFilterInstance : IFilterInstance, new()
    {
        private readonly List<MemberInfo> FInputMembers = new List<MemberInfo>();
        private readonly List<MemberInfo> FOutputMembers = new List<MemberInfo>();
        private readonly List<IDiffSpread> FInputs = new List<IDiffSpread>();
        private readonly List<ISpread> FOutputs = new List<ISpread>();

        // Called by our node factory
        [ImportingConstructor]
        public FilterNode(IIOFactory factory)
        {
            // Create in and outputs based on properties of the filter instance
            var dummyInstance = new TFilterInstance();
            var filterInstanceType = typeof(TFilterInstance);
            var properties = filterInstanceType.GetProperties();
            var fields = filterInstanceType.GetFields();
            var fieldsAndProperties = fields.Concat<MemberInfo>(properties);
            FInputMembers = fieldsAndProperties.Where(p => p.GetCustomAttributes(typeof(InputAttribute), true).Any()).ToList();
            FOutputMembers = fieldsAndProperties.Where(p => p.GetCustomAttributes(typeof(OutputAttribute), true).Any()).ToList();
            foreach (var member in FInputMembers)
            {
                var attribute = GetIOAttribute<InputAttribute>(dummyInstance, member);
                var memberType = GetMemberType(member);
                var spreadType = typeof(IDiffSpread<>).MakeGenericType(memberType);
                var spread = factory.CreateIO(spreadType, attribute) as IDiffSpread;
                FInputs.Add(spread);
            }
            foreach (var member in FOutputMembers)
            {
                var attribute = GetIOAttribute<OutputAttribute>(dummyInstance, member);
                var memberType = GetMemberType(member);
                var spreadType = typeof(ISpread<>).MakeGenericType(memberType);
                var spread = factory.CreateIO(spreadType, attribute) as ISpread;
                FOutputs.Add(spread);
            }
        }

        private static TIOAttribute GetIOAttribute<TIOAttribute>(object instance, MemberInfo member) where TIOAttribute : IOAttribute
        {
            var attribute = member.GetCustomAttributes(typeof(TIOAttribute), true).First() as TIOAttribute;
            var type = GetMemberType(member);
            if (type.IsPrimitive)
            {
                var memberValue = GetMemberValue(instance, member);
                var typeCode = Type.GetTypeCode(type);
                switch (typeCode)
                {
                    case TypeCode.Boolean:
                        if (memberValue != null)
                            attribute.DefaultBoolean = (bool)memberValue;
                        break;
                    case TypeCode.Byte:
                    case TypeCode.Decimal:
                    case TypeCode.Double:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.SByte:
                    case TypeCode.Single:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        if (memberValue != null)
                            attribute.DefaultValue = (double)Convert.ChangeType(memberValue, typeof(double));
                        break;
                    case TypeCode.String:
                        if (memberValue != null)
                            attribute.DefaultString = memberValue as string;;
                        break;
                    default:
                        break;
                }
            }
            return attribute;
        }

        private static Type GetMemberType(MemberInfo member)
        {
            var fieldInfo = member as FieldInfo;
            if (fieldInfo != null)
                return fieldInfo.FieldType;
            var propertyInfo = member as PropertyInfo;
            return propertyInfo.PropertyType;
        }

        private static object GetMemberValue(object instance, MemberInfo member)
        {
            var fieldInfo = member as FieldInfo;
            if (fieldInfo != null)
                return fieldInfo.GetValue(instance);
            var propertyInfo = member as PropertyInfo;
            if (propertyInfo.CanRead)
            {
                var indexParameters = propertyInfo.GetIndexParameters();
                if (indexParameters.Length == 0)
                    return propertyInfo.GetValue(instance, null);
            }
            return null;
        }

        protected override void Update(int InstanceCount, bool SpreadChanged)
        {
            for (int inputIndex = 0; inputIndex < FInputMembers.Count; inputIndex++)
            {
                var input = FInputs[inputIndex];
                if (input.IsChanged || SpreadChanged)
                {
                    var inputMember = FInputMembers[inputIndex];
                    for (int i = 0; i < InstanceCount; i++)
                    {
                        var filterInstance = FProcessor[i];
                        var inputField = inputMember as FieldInfo;
                        if (inputField != null)
                            inputField.SetValue(filterInstance, input[i]);
                        else
                        {
                            var propertyInfo = inputMember as PropertyInfo;
                            propertyInfo.SetValue(filterInstance, input[i], null);
                        }
						FProcessor[i].FlagForProcess();
                    }
                }
            }
        }
    }
}
