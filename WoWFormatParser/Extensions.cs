using System;
using WoWFormatParser.Helpers;
using WoWFormatParser.Structures;
using WoWFormatParser.Structures.ADT;
using WoWFormatParser.Structures.Common;
using WoWFormatParser.Structures.MDX;

namespace WoWFormatParser.Extensions
{
    public static class Extensions
    {
        public static T Cast<T>(this IFormat format) where T : Format => format as T;
        public static bool Is<T>(this IFormat format) where T : Format => format is T;

        public static T Cast<T>(this IGEOS format) where T : class => format as T;
        public static bool Is<T>(this IGEOS format) where T : class => format is T;

        /// <summary>
        /// Finds the string based on its starting index (pointer) 
        /// </summary>
        /// <param name="values"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static string FindByPointer(this string[] values, int index)
        {
            int l = 0, i = 0;
            for (; i < values.Length && l < index; i++)
                l += values[i].Length + 1;

            return l == index ? values[i] : "";
        }

        #region HeighMap
        private static int TileSize = 16;
        private static float TileSizeYrds = 533.33333F;
        private static float ChunkSize = TileSizeYrds / TileSize;
        private static float UnitSize = ChunkSize / 8.0f;
        public static HeightmapTranform TransformHeightData(this ADT adt)
        {
            HeightmapTranform transformed = new HeightmapTranform();

            for (int x = 0; x < 128; x++)
            {
                for (int y = 0; y < 128; y++)
                {
                    transformed.V8[x, y] = adt.MapChunks[x / 8, y / 8].V8[x % 8, y % 8];
                    transformed.V9[x, y] = adt.MapChunks[x / 8, y / 8].V9[x % 8, y % 8];
                }

                transformed.V9[x, 128] = adt.MapChunks[x / 8, 15].V9[x % 8, 8];
                transformed.V9[128, x] = adt.MapChunks[15, x / 8].V9[8, x % 8];
            }

            transformed.V9[128, 128] = adt.MapChunks[15, 15].V9[8, 8];

            return transformed;
        }

        public static float CalculateZ(this HeightmapTranform transformed, float cy, float cx)
        {
            var x = (cy * TileSizeYrds) / (256 - 1);
            var y = (cx * TileSizeYrds) / (256 - 1);
            return transformed.GetZ(x, y);
        }

        private static float GetZ(this HeightmapTranform transformed, float x, float z)
        {
            C3Vector[] v = new C3Vector[3] { new C3Vector(), new C3Vector(), new C3Vector() };
            C3Vector p = new C3Vector();

            // Find out quadrant
            int xc = (int)(x / UnitSize);
            int zc = (int)(z / UnitSize);

            if (xc > 127)
                xc = 127;

            if (zc > 127)
                zc = 127;

            float lx = x - xc * UnitSize;
            float lz = z - zc * UnitSize;
            p.X = lx;
            p.Z = lz;

            v[0].X = UnitSize / 2;
            v[0].Y = transformed.V8[xc, zc];
            v[0].Z = UnitSize / 2;

            if (lx > lz)
            {
                v[1].X = UnitSize;
                v[1].Y = transformed.V9[xc + 1, zc];
                v[1].Z = 0;
            }
            else
            {
                v[1].X = 0.0f;
                v[1].Y = transformed.V9[xc, zc + 1];
                v[1].Z = UnitSize;
            }

            if (lz > UnitSize - lx)
            {
                v[2].X = UnitSize;
                v[2].Y = transformed.V9[xc + 1, zc + 1];
                v[2].Z = UnitSize;
            }
            else
            {
                v[2].X = 0;
                v[2].Y = transformed.V9[xc, zc];
                v[2].Z = 0;
            }

            return -Solve(v, p);
        }

        /// <summary>ñ
        /// Plane equation ax+by+cz+d=0
        /// </summary>
        private static float Solve(C3Vector[] v, C3Vector p)
        {
            float a = v[0].Y * (v[1].Z - v[2].Z) + v[1].Y * (v[2].Z - v[0].Z) + v[2].Y * (v[0].Z - v[1].Z);
            float b = v[0].Z * (v[1].X - v[2].X) + v[1].Z * (v[2].X - v[0].X) + v[2].Z * (v[0].X - v[1].X);
            float c = v[0].X * (v[1].Y - v[2].Y) + v[1].X * (v[2].Y - v[0].Y) + v[2].X * (v[0].Y - v[1].Y);
            float d = v[0].X * (v[1].Y * v[2].Z - v[2].Y * v[1].Z) + v[1].X * (v[2].Y * v[0].Z - v[0].Y * v[2].Z) + v[2].X * (v[0].Y * v[1].Z - v[1].Y * v[0].Z);

            return ((a * p.X + c * p.Z - d) / b);
        }
        #endregion
    }
}
