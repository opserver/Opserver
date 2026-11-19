using System;
using System.ComponentModel;

namespace Jil.DeserializeDynamic
{
    sealed partial class JsonObject : ICustomTypeDescriptor
    {
        class DynamicTypeConverter : TypeConverter
        {
            JsonObject Context;

            public DynamicTypeConverter(JsonObject context) : base() 
            {
                Context = context;
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                // we can *never* convert to JsonObject
                return false;
            }

            public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
            {
                // we can *never* convert to JsonObject
                return false;
            }

            void ThrowIfNotValid(object value)
            {
                if (!IsValid(null, value))
                {
                    throw new InvalidOperationException("This TypeConverter is not bound to the passed in value");
                }
            }

            public override bool IsValid(ITypeDescriptorContext context, object value)
            {
                return object.ReferenceEquals(Context, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                object ignored;
                return Context.InnerTryConvert(destinationType, out ignored);
            }

            public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
            {
                ThrowIfNotValid(value);

                object ret;
                if (!Context.InnerTryConvert(destinationType, out ret))
                {
                    throw GetConvertToException(value, destinationType);
                }

                return ret;
            }
        }

        public AttributeCollection GetAttributes()
        {
            return AttributeCollection.Empty;
        }

        public string GetClassName()
        {
            return null;
        }

        public string GetComponentName()
        {
            return null;
        }

        public TypeConverter GetConverter()
        {
            return new DynamicTypeConverter(this);
        }

        public EventDescriptor GetDefaultEvent()
        {
            return null;
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return null;
        }

        public object GetEditor(Type editorBaseType)
        {
            return null;
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return EventDescriptorCollection.Empty;
        }

        public EventDescriptorCollection GetEvents()
        {
            return EventDescriptorCollection.Empty;
        }

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return PropertyDescriptorCollection.Empty;
        }

        public PropertyDescriptorCollection GetProperties()
        {
            return PropertyDescriptorCollection.Empty;
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return null;
        }
    }
}
