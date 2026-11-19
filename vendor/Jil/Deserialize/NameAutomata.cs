using Jil.Common;
using Sigil;
using Sigil.NonGeneric;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Jil.Deserialize
{
    internal static class NameAutomataConfig
    {
        public static bool UseBinarySearch = true;
    }

    internal static class NameAutomata<T>
    {
        static class Helper
        {
            static MethodInfo Consume = typeof(Helper).GetMethod("_Consume", BindingFlags.Static | BindingFlags.NonPublic);
            static MethodInfo ConsumeThunkReader = typeof(Helper).GetMethod("_ConsumeThunkReader", BindingFlags.Static | BindingFlags.NonPublic);

            static int NextName = 0;

            public static string GetNextName()
            {
                var r = Interlocked.Increment(ref NextName);
                return "na_n" + r;
            }

            public static MethodInfo GetConsume(Type forType)
            {
                if (forType == typeof(TextReader)) return Consume;
                if (forType == typeof(ThunkReader).MakeByRefType()) return ConsumeThunkReader;

                throw new Exception("Unexpected type " + forType);
            }

            // reflection uses these
#pragma warning disable IDE0051
#pragma warning disable IDE1006
            static void _Consume(TextReader tr, int ch)
            {
                while (ch != '\"' && ch != -1)
                {
                    ch = tr.Read();
                }
            }

            static void _ConsumeThunkReader(ref ThunkReader tr, int ch)
            {
                while (ch != '\"' && ch != -1)
                {
                    ch = tr.Read();
                }
            }
#pragma warning restore IDE0051
#pragma warning restore IDE1006

            static MethodInfo ExpectUnicodeHexQuad = typeof(Helper).GetMethod("_ExpectUnicodeHexQuad", BindingFlags.Static | BindingFlags.NonPublic);
            static MethodInfo ExpectUnicodeHexQuadThunkReader = typeof(Helper).GetMethod("_ExpectUnicodeHexQuadThunkReader", BindingFlags.Static | BindingFlags.NonPublic);

            public static MethodInfo GetExpectUnicodeHexQuad(Type forType)
            {
                if (forType == typeof(TextReader)) return ExpectUnicodeHexQuad;
                if (forType == typeof(ThunkReader).MakeByRefType()) return ExpectUnicodeHexQuadThunkReader;

                throw new Exception("Unexpected type " + forType);
            }

            // reflection uses these
#pragma warning disable IDE0051
#pragma warning disable IDE1006
            static int _ExpectUnicodeHexQuad(TextReader reader)
            {
                var c = reader.Read();
                if (c != 'u')
                {
                    throw new DeserializationException("Escape sequence expected unicode hex quad", reader, c == -1);
                }

                return Methods.ReadHexQuad(reader);
            }

            static int _ExpectUnicodeHexQuadThunkReader(ref ThunkReader reader)
            {
                var c = reader.Read();
                if (c != 'u')
                {
                    throw new DeserializationException("Escape sequence expected unicode hex quad", ref reader, c == -1);
                }

                return Methods.ReadHexQuadThunkReader(ref reader);
            }
#pragma warning restore IDE0051
#pragma warning restore IDE1006

            static MethodInfo TextReader_Read = typeof(TextReader).GetMethod("Read", Type.EmptyTypes);
            static MethodInfo ThunkReader_Read = typeof(ThunkReader).GetMethod("Read", Type.EmptyTypes);

            public static MethodInfo GetRead(Type forType)
            {
                if (forType == typeof(TextReader)) return TextReader_Read;
                if (forType == typeof(ThunkReader).MakeByRefType()) return ThunkReader_Read;

                throw new Exception("Unexpected type " + forType);
            }
        }

        class Data<V>
        {
            public readonly Action<Action> AddAction;
            public readonly Emit Emit;
            public readonly Action<Emit> DoReturn;
            public readonly Label Start;
            public readonly Label Failure;
            public readonly Local Local_ch;
            public readonly bool SkipWhitespace;
            public readonly bool FoldMultipleValues;
            public readonly bool CaseSensitive;

            public Data(
                Action<Action> addAction, 
                Emit emit, 
                Action<Emit> doReturn, 
                Label start, 
                Label failure, 
                Local local_ch, 
                bool skipWhitespace, 
                bool foldMultipleValues, 
                bool caseSensitive)
            {
                AddAction = addAction;
                Emit = emit;
                DoReturn = doReturn;
                Start = start;
                Failure = failure;
                Local_ch = local_ch;
                SkipWhitespace = skipWhitespace;
                FoldMultipleValues = foldMultipleValues;
                CaseSensitive = caseSensitive;
            }
        }

        [DebuggerDisplay("{Name}")]
        public class AutomataName
        {
            public readonly Type ReaderType;
            public readonly string Name;
            public readonly Action<Emit> OnFound;

            public AutomataName(Type readerType, string name, Action<Emit> onFound)
            {
                ReaderType = readerType;
                Name = name;
                OnFound = onFound;
            }
        }

        static void FinishName<V>(Data<V> d, AutomataName nameValue, Label onMatchChar)
        {
            d.AddAction(() =>
            {
                d.Emit.MarkLabel(onMatchChar);
                nameValue.OnFound(d.Emit);
                d.DoReturn(d.Emit);
            });
        }

        private static void FoldName<V>(Data<V> d, AutomataName nameValue, Label foldName)
        {
            d.AddAction(() =>
            {
                d.Emit.MarkLabel(foldName);
                nameValue.OnFound(d.Emit);
                d.Emit.Branch(d.Start);
            });
        }

        static void NextChar<V>(Type readerType, Data<V> d, IList<AutomataName> nameValues, int pos, Label onMatchChar)
        {
            var chars =
                nameValues
                .GroupBy(
                    nv =>
                    {
                        // we're off the end, group by something that can't be a real char
                        if (pos >= nv.Name.Length) return -1;

                        var c = nv.Name[pos];
                        if (d.CaseSensitive) return c;

                        // if we're case sensitive, group by the char with no regard to case
                        return char.ToLowerInvariant(c);
                    }
                )
                .ToList();

            var namesToFinish = new List<Tuple<char, Label>>();
            foreach (var charGroup in chars)
            {
                var ch = charGroup.Key;
                var items = charGroup.ToList();
                if (ch == -1)
                {
                    if (charGroup.Count() != 1)
                    {
                        throw new ConstructionException("Couldn't build parsing automata, found duplicate terminals for " + string.Join(", ", charGroup.Select(c => c.Name)));
                    }

                    var completeName = d.Emit.DefineLabel("complete_" + items[0].Name);
                    FinishName(d, items[0], completeName);
                    namesToFinish.Add(Tuple.Create('"', completeName));

                    if (d.SkipWhitespace)
                        namesToFinish.Add(Tuple.Create(' ', onMatchChar));

                    if (d.FoldMultipleValues)
                    {
                        var foldName = d.Emit.DefineLabel("fold_" + items[0].Name);
                        FoldName(d, items[0], foldName);
                        namesToFinish.Add(Tuple.Create(',', foldName));
                    }
                }
                else
                {
                    if (d.SkipWhitespace && pos == 0)
                        namesToFinish.Add(Tuple.Create(' ', onMatchChar));

                    Label next = null;
                    if (d.CaseSensitive)
                    {
                        next = d.Emit.DefineLabel("_" + items[0].Name.Substring(0, pos + 1));
                        namesToFinish.Add(Tuple.Create((char)ch, next));
                    }
                    else
                    {
                        var lower = char.ToLower((char)ch);
                        var upper = char.ToUpper((char)ch);

                        var alreadyInUse = 
                            namesToFinish.Any(t => t.Item1 == lower) || 
                            namesToFinish.Any(t => t.Item1 == upper);

                        if (!alreadyInUse)
                        {
                            next = d.Emit.DefineLabel("_" + items[0].Name.Substring(0, pos + 1));
                            namesToFinish.Add(Tuple.Create(lower, next));
                            namesToFinish.Add(Tuple.Create(upper, next));
                        }
                    }

                    if (next != null)
                    {
                        NextChar(readerType, d, items, pos + 1, next);
                    }
                }
            }

            d.AddAction(() =>
            {
                d.Emit.MarkLabel(onMatchChar);

                d.Emit.LoadArgument(0);
                d.Emit.CallVirtual(Helper.GetRead(readerType));
                d.Emit.StoreLocal(d.Local_ch);

                DoCharBranches(d, namesToFinish);

                d.Emit.LoadLocal(d.Local_ch);                               // char
                d.Emit.LoadConstant('\\');                                  // char char
                d.Emit.UnsignedBranchIfNotEqual(d.Failure);                 // 
                d.Emit.LoadArgument(0);                                     // TextReader
                d.Emit.Call(Helper.GetExpectUnicodeHexQuad(readerType));    // char
                d.Emit.StoreLocal(d.Local_ch);                              //

                DoCharBranches(d, namesToFinish);

                d.Emit.Branch(d.Failure);
            });
        }

        static void DoCharBranches<V>(Data<V> d, List<Tuple<char, Label>> namesToFinish)
        {
            if (NameAutomataConfig.UseBinarySearch)
            {
                var bsComparisons = (int)Math.Ceiling(Math.Log(namesToFinish.Count, 2)) + 1;
                if (bsComparisons < namesToFinish.Count)
                {
                    DoCharBinarySearch(d, namesToFinish);
                }
                else
                {
                    DoCharLinearScan(d, namesToFinish);
                }
            }
            else
            {
                DoCharLinearScan(d, namesToFinish);
            }
        }

        static void DoCharLinearScan<V>(Data<V> d, List<Tuple<char, Label>> namesToFinish)
        {
            foreach (var item in namesToFinish)
            {
                d.Emit.LoadLocal(d.Local_ch);
                d.Emit.LoadConstant((int)item.Item1);
                d.Emit.BranchIfEqual(item.Item2);
            }
        }

        static void DoCharBinarySearch<V>(Data<V> d, List<Tuple<char, Label>> namesToFinish)
        {
            var noMatch = d.Emit.DefineLabel(Helper.GetNextName());

            var inOrder = namesToFinish.OrderBy(_ => _.Item1).ToList();

            Action<List<Tuple<char, Label>>> match = null;
            match =
                charsLeft =>
                {
                    if (charsLeft.Count == 0)
                    {
                        d.Emit.Branch(noMatch);                 // --empty--
                        return;
                    }

                    if (charsLeft.Count == 1)
                    {
                        var exact = charsLeft[0];

                        d.Emit.LoadLocal(d.Local_ch);           // int
                        d.Emit.LoadConstant((int)exact.Item1);  // int int
                        d.Emit.BranchIfEqual(exact.Item2);      // --empty--
                        d.Emit.Branch(noMatch);                 // --empty--
                        return;
                    }

                    var midPoint = charsLeft.Count / 2;
                    var midVal = charsLeft[midPoint];

                    var left = charsLeft.Take(midPoint).ToList();
                    var right = charsLeft.Skip(midPoint).ToList();

                    var leftLabel = d.Emit.DefineLabel(Helper.GetNextName());

                    d.Emit.LoadLocal(d.Local_ch);           // int
                    d.Emit.LoadConstant((int)midVal.Item1); // int int
                    d.Emit.BranchIfLess(leftLabel);         // --empty--
                    match(right);                           // --empty--

                    d.Emit.MarkLabel(leftLabel);            // --empty--
                    match(left);                            // --empty
                };

            match(inOrder);

            d.Emit.MarkLabel(noMatch);
        }

        static List<List<Tuple<char, Label>>> SplitIntoContiguousGroups(List<Tuple<char, Label>> chars)
        {
            var inOrder = chars.OrderBy(t => t.Item1).ToList();

            var ret = new List<List<Tuple<char, Label>>>();

            var runningGroup = new List<Tuple<char, Label>>();

            foreach (var t in inOrder)
            {
                if (runningGroup.Count == 0 || (runningGroup[runningGroup.Count - 1].Item1 + 1) == t.Item1)
                {
                    runningGroup.Add(t);
                }
                else
                {
                    ret.Add(runningGroup);
                    runningGroup = new List<Tuple<char, Label>>();
                    runningGroup.Add(t);
                }
            }

            if (runningGroup.Count > 0)
            {
                ret.Add(runningGroup);
            }

            return ret;
        }

        public static AutomataName CreateName(Type readerType, string name, Action<Emit> onFound)
        {
            return new AutomataName(readerType, name, onFound);
        }

        public static Delegate CreateFold<DelegateType>(Type readerType, IEnumerable<AutomataName> names, Action<Emit> initialize, Action<Emit> doReturn, bool skipWhitespace, bool foldMultipleValues, bool caseSensitive, object defaultValue)
        {
            var sorted =
                names
                .OrderBy(kv => kv.Name)
                .ToList();

            var stack = new Stack<Action>();
            Action<Action> addAction =
                action => stack.Push(action);

            var emit = Emit.NewDynamicMethod(typeof(T), new[] { readerType }, doVerify: Utils.DoVerify);

            initialize(emit);

            var ch = emit.DeclareLocal(typeof(int), "ch");
            var failure = emit.DefineLabel("failure");

            var cons = typeof(DeserializationException).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(string), readerType, typeof(bool) });

            if (defaultValue == null)
            {
                // no default? throw a deserialization exception
                addAction(
                    () =>
                    {
                        emit.MarkLabel(failure);

                        emit.LoadArgument(0);
                        emit.LoadLocal(ch);
                        emit.Call(Helper.GetConsume(readerType));

                        emit.LoadConstant("Unexpected value for " + typeof(T).Name);
                        emit.LoadArgument(0);
                        emit.LoadConstant(false);

                        emit.NewObject(cons);
                        emit.Throw();
                    }
                );
            }
            else
            {
                addAction(
                    () =>
                    {
                        emit.MarkLabel(failure);

                        emit.LoadArgument(0);
                        emit.LoadLocal(ch);
                        emit.Call(Helper.GetConsume(readerType));

                        // strip of the ? if it exists
                        var type = defaultValue.GetType();
                        type = Nullable.GetUnderlyingType(type) ?? type;

                        if(!Utils.LoadConstantOfType(emit, defaultValue, type))
                        {
                            throw new Exception("Couldn't load constant for " + type.Name);
                        }

                        emit.Return();
                    }
                );

            }

            var start = emit.DefineLabel("start");
            var d = new Data<T>(addAction, emit, doReturn, start, failure, ch, skipWhitespace, foldMultipleValues, caseSensitive);

            NextChar(readerType, d, sorted, 0, start);

            foreach (var action in stack)
            {
                action();
            }

            return emit.CreateDelegate(typeof(DelegateType), Utils.DelegateOptimizationOptions);
        }

        public static Delegate Create<DelegateType>(Type readerType, IEnumerable<AutomataName> names, bool caseSensitive, object defaultValue)
        {
            return CreateFold<DelegateType>(readerType, names, _ => { }, emit => emit.Return(), false, false, caseSensitive, defaultValue);
        }
    }
}