using WoWFormatParser.Structures.Interfaces;

namespace WoWFormatParser.Structures.Common
{
    public struct CiRange : IStringDescriptor
    {
        public int min;
        public int max;

        public CiRange(int min, int max)
        {
            this.min = min;             
            this.max = max;
        }

        public override string ToString() => $"Min: {min}, Max: {max}";
    }
}
