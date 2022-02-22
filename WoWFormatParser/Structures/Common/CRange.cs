using System.Runtime.InteropServices;
using WoWFormatParser.Structures.Interfaces;

namespace WoWFormatParser.Structures.Common
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CRange : IStringDescriptor
    {
        public float low;
        public float high;

        public CRange(float low, float high)
        {
            this.low = low;
            this.high = high;
        }

        public override string ToString() => $"Low: {low}, High: {high}";
    }
}
