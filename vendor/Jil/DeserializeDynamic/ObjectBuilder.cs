using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jil.DeserializeDynamic
{
    sealed class ObjectBuilder
    {
        private Options Options;
        public StringBuilder CommonStringBuffer;

        char[] _CommonCharBuffer;
        public char[] CommonCharBuffer
        {
            get
            {
                if (_CommonCharBuffer != null) return _CommonCharBuffer;

                _CommonCharBuffer = new char[Methods.CharBufferSize];
                return _CommonCharBuffer;
            }
        }

        public static FieldInfo _BeingBuilt = typeof(ObjectBuilder).GetField("BeingBuilt");
        public JsonObject BeingBuilt;

        public ObjectBuilder(Options options)
        {
            Options = options;
        }

        public void PutNull()
        {
            if (BeingBuilt != null)
            {
                BeingBuilt.Put(null);
            }
        }

        public void PutTrue()
        {
            if (BeingBuilt == null)
            {
                BeingBuilt = JsonObject.True;
            }
            else
            {
                BeingBuilt.Put(JsonObject.True);
            }
        }

        public void PutFalse()
        {
            if (BeingBuilt == null)
            {
                BeingBuilt = JsonObject.False;
            }
            else
            {
                BeingBuilt.Put(JsonObject.False);
            }
        }

        public void PutString(string str)
        {
            if (BeingBuilt == null)
            {
                BeingBuilt = JsonObject.ForString(str, Options);
            }
            else
            {
                BeingBuilt.Put(JsonObject.ForString(str, Options));
            }
        }

        public void StartArray()
        {
            var arr = JsonObject.NewArray(BeingBuilt);

            if (BeingBuilt == null)
            {
                BeingBuilt = arr;
            }
            else
            {
                BeingBuilt.Put(arr);
                BeingBuilt = arr;
            }
        }

        public void EndArray()
        {
            BeingBuilt = BeingBuilt.Pop();
        }

        public void StartObject()
        {
            var obj = JsonObject.NewObject(BeingBuilt);

            if (BeingBuilt == null)
            {
                BeingBuilt = obj;
            }
            else
            {
                BeingBuilt.Put(obj);
                BeingBuilt = obj;
            }
        }

        public void EndObject()
        {
            BeingBuilt = BeingBuilt.Pop();
        }

        public void StartObjectMember()
        {
            var member = JsonObject.NewObjectMember(BeingBuilt);

            BeingBuilt = member;
        }

        public void EndObjectMember()
        {
            BeingBuilt = BeingBuilt.Pop();
        }

        public void PutNumber(double number)
        {
            var num = JsonObject.ForNumber(number, Options);
            if (BeingBuilt == null)
            {
                BeingBuilt = num;
            }
            else
            {
                BeingBuilt.Put(num);
            }
        }

        public void PutFastNumber(bool negative, ulong beforeDot, ulong afterDot, byte afterDotLength, long afterE)
        {
            var num = JsonObject.ForFastNumber(negative, beforeDot, afterDot, afterDotLength, afterE, Options);
            if (BeingBuilt == null)
            {
                BeingBuilt = num;
            }
            else
            {
                BeingBuilt.Put(num);
            }
        }
    }
}
