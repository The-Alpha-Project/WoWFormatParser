using WoWFormatParser.Structures.Interfaces;

namespace WoWFormatParser.Structures.Common
{
    public struct C3Vector : IStringDescriptor
    {
        public float X;
        public float Y;
        public float Z;

        public C3Vector(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString() => $"X: {X}, Y: {Y}, Z: {Z}";
    }
}
