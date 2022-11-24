using System;

namespace AgdaLibraryLookup.Lexer
{
    //todo: why not readonly? why not record?
    public struct TextPosition
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public int Index { get; set; }

        public TextPosition(int line, int column, int index) 
            => (Line, Column, Index) = (line, column, index);
    }

    public struct TextRegion
    {
        public TextPosition Start { get; set; }
        public TextPosition End { get; set; }

        public TextRegion(TextPosition start, TextPosition end) 
            => (Start, End) = (start, end);

        public static implicit operator Range(TextRegion r) 
            => new(r.Start.Index, r.End.Index);
    }
}
