using System.Runtime.InteropServices;
using WoWFormatParser.Structures.Interfaces;

namespace WoWFormatParser.Structures.Common
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CImVector : IStringDescriptor
    {
        public byte b;
        public byte g;
        public byte r;
        public byte a;

        public CImVector(byte b, byte g, byte r, byte a)
        {
            this.b = b;
            this.g = g;
            this.r = r;              
            this.a = a;
        }

        public override string ToString() => $"B: {b}, G: {g}, R: {r}, A: {a}";
    }
}
