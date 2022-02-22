using WoWFormatParser.Structures.Interfaces;

namespace WoWFormatParser.Structures.Common
{
    public struct CInterval : IStringDescriptor
    {
        public int Start;
        public int End;
        public int Repeat;

        public CInterval(int start, int end, int repeat)
        {
            Start = start;
            End = end;
            Repeat = repeat;
        }

        public override string ToString() => $"Start: {Start}, End: {End}, Repeat: {Repeat}";
    }
}
