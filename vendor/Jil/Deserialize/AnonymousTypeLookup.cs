using Jil.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jil.Deserialize
{
    delegate int AnonymousTypeLookupThunkReaderDelegate(ref ThunkReader reader);

    class AnonymousTypeLookup<ForType>
    {
        public static Dictionary<string, Tuple<Type, int>> ParametersToTypeAndIndex;
        private static Func<TextReader, int> _findConstructorParameterIndex;
        private static AnonymousTypeLookupThunkReaderDelegate _findConstructorParameterIndexThunkReader;

        // Still used in ReadAnonymousObjectDictionaryLookup (can be removed if NameAutomata method is always used)
        public static Dictionary<string, int> Lookup;
 
        static AnonymousTypeLookup()
        {
            ParametersToTypeAndIndex = Utils.GetAnonymousNameToConstructorMap(typeof(ForType));

            var orderedNames =
                ParametersToTypeAndIndex
                .OrderBy(kv => kv.Value.Item2)
                .Select(kv => kv.Key);

            _findConstructorParameterIndex = CreateFindMember(orderedNames);
            _findConstructorParameterIndexThunkReader = CreateFindMemberThunkReader(orderedNames);

            // Still used in ReadAnonymousObjectDictionaryLookup (can be removed if NameAutomata method is always used)
            Lookup = ParametersToTypeAndIndex.ToDictionary(d => d.Key, d => d.Value.Item2);
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

        private static AnonymousTypeLookupThunkReaderDelegate CreateFindMemberThunkReader(IEnumerable<string> names)
        {
            var nameToResults =
                names
                .Select((name, index) => NameAutomata<int>.CreateName(typeof(ThunkReader).MakeByRefType(), name, emit => emit.LoadConstant(index)))
                .ToList();

            var ret = NameAutomata<int>.Create<AnonymousTypeLookupThunkReaderDelegate>(typeof(ThunkReader).MakeByRefType(), nameToResults, true, defaultValue: -1);
            return (AnonymousTypeLookupThunkReaderDelegate)ret;
        }

        // probably not the best place for this; but sufficent I guess...
        public static int FindConstructorParameterIndex(TextReader reader)
        {
            return _findConstructorParameterIndex(reader);
        }

        public static int FindConstructorParameterIndexThunkReader(ref ThunkReader reader)
        {
            return _findConstructorParameterIndexThunkReader(ref reader);
        }
    }
}
