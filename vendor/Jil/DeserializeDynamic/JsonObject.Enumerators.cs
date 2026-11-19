using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jil.DeserializeDynamic
{
    sealed partial class JsonObject
    {
        sealed class ArrayEnumeratorWrapper : DynamicObject, IEnumerator, IEnumerator<JsonObject>, IDisposable
        {
            List<JsonObject>.Enumerator Wrapped;

            private ArrayEnumeratorWrapper(List<JsonObject>.Enumerator wrapped)
            {
                Wrapped = wrapped;
            }

            public static IEnumerator MakeAsIEnumerator(List<JsonObject>.Enumerator wrapped)
            {
                return new ArrayEnumeratorWrapper(wrapped);
            }

            public static IEnumerator<JsonObject> MakeAsIEnumeratorOfT(List<JsonObject>.Enumerator wrapped)
            {
                return new ArrayEnumeratorWrapper(wrapped);
            }

            public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
            {
                result = null;
                
                switch(binder.Name)
                {
                    case "Dispose":
                        if (args.Length == 0)
                        {
                            this.Dispose();
                            return true;
                        }
                        return false;
                    case "Equals":
                        if (args.Length == 1)
                        {
                            result = this.Equals(args[0]);
                            return true;
                        }
                        return false;
                    case "GetHashCode":
                        if (args.Length == 0)
                        {
                            result = this.GetHashCode();
                            return true;
                        }
                        return false;
                    case "GetType":
                        if (args.Length == 0)
                        {
                            result = this.GetType();
                            return true;
                        }
                        return false;
                    case "MoveNext":
                        if (args.Length == 0)
                        {
                            result = this.MoveNext();
                            return true;
                        }
                        return false;
                    case "ToString":
                        if (args.Length == 0)
                        {
                            result = this.ToString();
                            return true;
                        }
                        return false;
                    case "Reset":
                        if (args.Length == 0)
                        {
                            this.Reset();
                            return true;
                        }
                        return false;
                    default:
                        return false;
                }
            }

            static readonly string[] DynamicMemberNames = new[] { "Current" };
            public override IEnumerable<string> GetDynamicMemberNames()
            {
                return DynamicMemberNames;
            }

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                result = null;

                if (binder.Name == "Current")
                {
                    result = this.Current;
                    return true;
                }

                return false;
            }

            public object Current
            {
                get { return Wrapped.Current; }
            }

            public bool MoveNext()
            {
                return Wrapped.MoveNext();
            }

            public void Reset()
            {
                ((IEnumerator)Wrapped).Reset();
            }

            JsonObject IEnumerator<JsonObject>.Current
            {
                get { return Wrapped.Current; }
            }

            public void Dispose()
            {
                Wrapped.Dispose();
            }

            public override bool Equals(object obj)
            {
                return Wrapped.Equals(obj);
            }

            public override int GetHashCode()
            {
                return Wrapped.GetHashCode();
            }

            public override string ToString()
            {
                return Wrapped.ToString();
            }
        }

        sealed class ObjectEnumeratorWrapper : DynamicObject, IEnumerator, IEnumerator<object>, IDisposable, IDictionaryEnumerator
        {
            sealed class KeyValuePairWrapper : DynamicObject
            {
                KeyValuePair<string, JsonObject> Wrapped;

                public KeyValuePairWrapper(KeyValuePair<string, JsonObject> wrapped)
                {
                    Wrapped = wrapped;
                }

                static readonly string[] DynamicMemberNames = new[] { "Key", "Value" };
                public override IEnumerable<string> GetDynamicMemberNames()
                {
                    return DynamicMemberNames;
                }

                public override bool TryGetMember(GetMemberBinder binder, out object result)
                {
                    result = null;

                    switch (binder.Name)
                    {
                        case "Key":
                            result = Wrapped.Key;
                            return true;
                        case "Value":
                            result = Wrapped.Value;
                            return true;
                        default:
                            return false;
                    }
                }

                public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
                {
                    result = null;

                    switch (binder.Name)
                    {
                        case "Equals":
                            if (args.Length == 1)
                            {
                                result = Wrapped.Equals(args[0]);
                                return true;
                            }
                            return false;
                        case "GetHashCode":
                            if (args.Length == 0)
                            {
                                result = Wrapped.GetHashCode();
                                return true;
                            }
                            return false;
                        case "GetType":
                            if (args.Length == 0)
                            {
                                // Do *not* proxy this call, but *do* respond to it
                                result = this.GetType();
                                return true;
                            }
                            return false;
                        case "ToString":
                            if (args.Length == 0)
                            {
                                result = Wrapped.ToString();
                                return true;
                            }

                            return false;
                        default:
                            return false;
                    }
                }
            }

            Dictionary<string, JsonObject>.Enumerator Wrapped;

            private ObjectEnumeratorWrapper(Dictionary<string, JsonObject>.Enumerator wrapped)
            {
                Wrapped = wrapped;
            }

            public static IEnumerator MakeAsIEnumerator(Dictionary<string, JsonObject>.Enumerator wrapped)
            {
                return new ObjectEnumeratorWrapper(wrapped);
            }

            public static IEnumerator<object> MakeAsIEnumeratorOfT(Dictionary<string, JsonObject>.Enumerator wrapped)
            {
                return new ObjectEnumeratorWrapper(wrapped);
            }

            public static IDictionaryEnumerator MakeAsIDictionaryEnumerator(Dictionary<string, JsonObject>.Enumerator wrapped)
            {
                return new ObjectEnumeratorWrapper(wrapped);
            }

            public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
            {
                result = null;

                switch (binder.Name)
                {
                    case "Dispose":
                        if (args.Length == 0)
                        {
                            this.Dispose();
                            return true;
                        }
                        result = null;
                        return false;
                    case "Equals":
                        if (args.Length == 1)
                        {
                            result = this.Equals(args[0]);
                            return true;
                        }
                        return false;
                    case "GetHashCode":
                        if (args.Length == 0)
                        {
                            result = this.GetHashCode();
                            return true;
                        }
                        return false;
                    case "GetType":
                        if (args.Length == 0)
                        {
                            result = this.GetType();
                            return true;
                        }

                        return false;
                    case "MoveNext":
                        if (args.Length == 0)
                        {
                            result = this.MoveNext();
                            return true;
                        }
                        return false;
                    case "ToString":
                        if (args.Length == 0)
                        {
                            result = this.ToString();
                            return true;
                        }
                        return false;
                    case "Reset":
                        if (args.Length == 0)
                        {
                            this.Reset();
                            return true;
                        }
                        return false;
                    default:
                        return false;
                }
            }

            static readonly string[] DynamicMemberNames = new[] { "Current", "Entry", "Key", "Value" };
            public override IEnumerable<string> GetDynamicMemberNames()
            {
                return DynamicMemberNames;
            }

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                result = null;
                switch (binder.Name)
                {
                    case "Current":
                        result = this.Current;
                        return true;
                    case "Entry":
                        result = this.Entry;
                        return true;
                    case "Key":
                        result = this.Key;
                        return true;
                    case "Value":
                        result = this.Value;
                        return true;
                    default:
                        return false;
                }
            }

            public object Current
            {
                get { return new KeyValuePairWrapper(Wrapped.Current); }
            }

            public bool MoveNext()
            {
                return Wrapped.MoveNext();
            }

            public void Reset()
            {
                ((IEnumerator)Wrapped).Reset();
            }

            object IEnumerator<object>.Current
            {
                get { return new KeyValuePairWrapper(Wrapped.Current); }
            }

            public void Dispose()
            {
                Wrapped.Dispose();
            }

            public DictionaryEntry Entry
            {
                get { return ((IDictionaryEnumerator)Wrapped).Entry; }
            }

            public object Key
            {
                get { return ((IDictionaryEnumerator)Wrapped).Key; }
            }

            public object Value
            {
                get { return ((IDictionaryEnumerator)Wrapped).Value; }
            }

            public override bool Equals(object obj)
            {
                return Wrapped.Equals(obj);
            }

            public override int GetHashCode()
            {
                return Wrapped.GetHashCode();
            }

            public override string ToString()
            {
                return Wrapped.ToString();
            }
        }

        sealed class EnumerableObjectWrapper : IEnumerable, IEnumerable<object>
        {
            Dictionary<string, JsonObject> Wrapped;

            private EnumerableObjectWrapper(Dictionary<string, JsonObject> wrapped)
            {
                Wrapped = wrapped;
            }

            public static IEnumerable MakeAsIEnumerable(Dictionary<string, JsonObject> wrapped) 
            {
                return new EnumerableObjectWrapper(wrapped);
            }

            public static IEnumerable<object> MakeAsIEnumerableOfT(Dictionary<string, JsonObject> wrapped)
            {
                return new EnumerableObjectWrapper(wrapped);
            }

            public IEnumerator GetEnumerator()
            {
                return ObjectEnumeratorWrapper.MakeAsIEnumerator(Wrapped.GetEnumerator());
            }

            IEnumerator<object> IEnumerable<object>.GetEnumerator()
            {
                return ObjectEnumeratorWrapper.MakeAsIEnumeratorOfT(Wrapped.GetEnumerator());
            }
        }

        sealed class EnumerableArrayWrapper : IEnumerable, IEnumerable<object>
        {
            List<JsonObject> Wrapped;

            private EnumerableArrayWrapper(List<JsonObject> wrapped)
            {
                Wrapped = wrapped;
            }

            public static IEnumerable MakeAsIEnumerable(List<JsonObject> wrapped)
            {
                return new EnumerableArrayWrapper(wrapped);
            }

            public static IEnumerable<object> MakeAsIEnumerableOfT(List<JsonObject> wrapped)
            {
                return new EnumerableArrayWrapper(wrapped);
            }

            public IEnumerator GetEnumerator()
            {
                return ArrayEnumeratorWrapper.MakeAsIEnumerator(Wrapped.GetEnumerator());
            }

            IEnumerator<object> IEnumerable<object>.GetEnumerator()
            {
                return ArrayEnumeratorWrapper.MakeAsIEnumeratorOfT(Wrapped.GetEnumerator());
            }
        }
    }
}
