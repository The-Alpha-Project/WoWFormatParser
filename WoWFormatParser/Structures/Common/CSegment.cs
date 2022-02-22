namespace WoWFormatParser.Structures.Common
{
    public struct CSegment
    {
        public C3Vector Color;
        public float Alpha;
        public float Scaling;

        public CSegment(C3Vector color, float alpha, float scaling)
        {
            Color = color;
            Alpha = alpha;
            Scaling = scaling;
        }
    }
}
