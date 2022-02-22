﻿namespace WoWFormatParser.Structures.Common
{
    public struct CAaBox
    {
        public C3Vector Min;
        public C3Vector Max;
        public CAaBox(C3Vector min, C3Vector max)
        {
            Min = min;
            Max = max;
        }
    }
}
