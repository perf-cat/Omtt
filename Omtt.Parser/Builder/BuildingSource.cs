using System;
using System.Collections.Generic;
using Omtt.Parser.Lexical;

namespace Omtt.Parser.Builder
{
    internal sealed class BuildingSource
    {
        private readonly List<Lexem> _lexems;
        private Int32 _index;

        internal BuildingSource(List<Lexem> lexems)
        {
            _lexems = lexems;
            _index = 0;
        }

        internal Lexem GetNext()
        {
            return _lexems[_index++];
        }

        internal Boolean Any()
        {
            return _index < _lexems.Count;
        }
    }
}