using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jil.Common
{
    class PeekSupportingTextReader : TextReader
    {
        TextReader Inner;

        int? Peeked;

        public PeekSupportingTextReader(TextReader inner)
        {
            Inner = inner;
        }

        public override int Peek()
        {
            if (Peeked != null) return Peeked.Value;

            Peeked = Inner.Read();

            return Peeked.Value;
        }

        public override int Read()
        {
            if (Peeked != null)
            {
                var ret = Peeked.Value;
                Peeked = null;

                return ret;
            }

            return Inner.Read();
        }
    }
}
