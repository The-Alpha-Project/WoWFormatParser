namespace WoWFormatParser.Structures.Common
{
    public struct CExtent
    {
        public float Radius;
        public CAaBox Extent;
        public CExtent(float radius, CAaBox extent)
        {
            Radius = radius;
            Extent = extent;
        }
    }
}
