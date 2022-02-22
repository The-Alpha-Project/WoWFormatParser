using WoWFormatParser.Structures.Interfaces;

namespace WoWFormatParser.Structures.Common
{
    public struct C2Vector : IStringDescriptor
    {
        public float X;
        public float Y;

        public C2Vector(float x, float y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"X: {X}, Y: {Y}";
    }
}
