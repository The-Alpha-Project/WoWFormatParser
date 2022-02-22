namespace WoWFormatParser.Structures.Common
{
    public struct CSphere
    {
        public C3Vector Center;
        public float Radius;

        public CSphere(C3Vector center, float radius)
        {
            Center = center;
            Radius = radius;
        }
    }
}
