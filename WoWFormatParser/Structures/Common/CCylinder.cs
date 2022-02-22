namespace WoWFormatParser.Structures.Common
{
    public struct CCylinder
    {
        public C3Vector Base;
        public float Height;
        public float Radius;

        public CCylinder(C3Vector @base, float height, float radius)
        {
            Base = @base;
            Height = height;
            Radius = radius;
        }
    }
}
