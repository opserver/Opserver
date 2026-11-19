using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Jil.Common;
using System.IO;
using Sigil;

namespace Jil.Deserialize
{
    delegate int SetterLookupThunkReaderDelegate(ref ThunkReader reader);

    static class SetterLookup<ForType, SerializationNameFormatType>
    {
        private static readonly IReadOnlyList<Tuple<string, MemberInfo[]>> _nameOrderedSetters;
        private static Func<TextReader, int> _findMember;
        private static SetterLookupThunkReaderDelegate _findMemberThunkReader;

        public static Dictionary<string, int> Lookup;

        static SetterLookup()
        {
            _nameOrderedSetters = GetOrderedSetters();

            Lookup =
                _nameOrderedSetters
                .Select((setter, index) => Tuple.Create(setter.Item1, index))
                .ToDictionary(kv => kv.Item1, kv => kv.Item2);

            _findMember = CreateFindMember(_nameOrderedSetters.Select(setter => setter.Item1));
            _findMemberThunkReader = CreateFindMemberThunkReader(_nameOrderedSetters.Select(setter => setter.Item1));
        }

        private static IReadOnlyList<Tuple<string, MemberInfo[]>> GetOrderedSetters()
        {
            var forType = typeof(ForType);
            var flags = BindingFlags.Instance | BindingFlags.Public;
            var serializationNameFormat = SerializationNameFormat.Verbatim;
            if (typeof(SerializationNameFormatType) == typeof(SerializationNameFormatCamelCase))
            {
                serializationNameFormat = SerializationNameFormat.CamelCase;
            }

            var fields = forType.GetFields(flags).Where(field => field.ShouldUseMember());
            var props = forType.GetProperties(flags).Where(p => p.SetMethod != null && p.ShouldUseMember());

            var allMembers = fields.Cast<MemberInfo>().Concat(props.Cast<MemberInfo>()).ToList();
            var hidden = new List<MemberInfo>();
            foreach(var members in allMembers.GroupBy(g => g.Name))
            {
                if (members.Count() == 1) continue;

                var paths = new Dictionary<MemberInfo, int>();

                foreach(var member in members)
                {
                    var path = new Stack<Type>();
                    var cur = member.DeclaringType;
                    while(cur != null)
                    {
                        path.Push(cur);
                        cur = cur.BaseType();
                    }

                    paths[member] = path.Count;
                }

                var keep = paths.OrderByDescending(kv => kv.Value).First().Key;

                hidden.AddRange(paths.Keys.Except(new[] { keep }));
            }

            var withoutHidden = allMembers.Except(hidden);

            var setters = new Dictionary<string, List<MemberInfo>>();

            foreach (var member in withoutHidden)
            {
                var name = member.GetSerializationName(serializationNameFormat);
                List<MemberInfo> members;
                if (!setters.TryGetValue(name, out members))
                {
                    setters[name] = members = new List<MemberInfo>();
                }

                members.Add(member);
            }

            var ret =
                setters
                    .Select(kv => Tuple.Create(kv.Key, kv.Value.ToArray()))
                    .OrderBy(t => t.Item1)
                    .ToList()
                    .AsReadOnly();

            return ret;
        }

        private static Func<TextReader, int> CreateFindMember(IEnumerable<string> names)
        {
            var nameToResults =
                names
                .Select((name, index) => NameAutomata<int>.CreateName(typeof(TextReader), name, emit => emit.LoadConstant(index)))
                .ToList();

            var ret = NameAutomata<int>.Create<Func<TextReader, int>>(typeof(TextReader), nameToResults, true, defaultValue: -1);

            return (Func<TextReader, int>)ret;
        }

        private static SetterLookupThunkReaderDelegate CreateFindMemberThunkReader(IEnumerable<string> names)
        {
            var nameToResults =
                names
                .Select((name, index) => NameAutomata<int>.CreateName(typeof(ThunkReader).MakeByRefType(), name, emit => emit.LoadConstant(index)))
                .ToList();

            var ret = NameAutomata<int>.Create<SetterLookupThunkReaderDelegate>(typeof(ThunkReader).MakeByRefType(), nameToResults, true, defaultValue: -1);

            return (SetterLookupThunkReaderDelegate)ret;
        }

        // probably not the best place for this; but sufficent I guess...
        public static int FindSetterIndex(TextReader reader)
        {
            return _findMember(reader);
        }

        public static int FindSetterIndexThunkReader(ref ThunkReader reader)
        {
            return _findMemberThunkReader(ref reader);
        }

        public static Dictionary<string, MemberInfo[]> GetSetters()
        {
            return _nameOrderedSetters.ToDictionary(m => m.Item1, m => m.Item2);
        }
    }
}