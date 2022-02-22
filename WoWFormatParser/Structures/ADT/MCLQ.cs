using System.IO;
using System.Runtime.InteropServices;
using WoWFormatParser.Helpers;
using WoWFormatParser.Structures.Common;
using WoWFormatParser.Structures.Interfaces;

namespace WoWFormatParser.Structures.ADT
{
    public class MCLQ
    {
        public CRange Height;
        public object[,] Verts = new object[9, 9];
        public byte[,] Flags = new byte[8, 8];
        public uint NFlowvs;
        public SWFlowv[] Flowvs;
        public MCNK_Flags Flag;

        public MCLQ(BinaryReader br, MCNK_Flags flag)
        {
            Flag = flag;
            Height = br.ReadStruct<CRange>();

            switch (flag)
            {
                case MCNK_Flags.IsOcean:
                    for (int i = 0; i < 9; i++)
                        for (int j = 0; j < 9; j++)
                            Verts[i, j] = br.ReadStruct<SOVert>();
                    break;
                case MCNK_Flags.IsMagma:
                    for (int i = 0; i < 9; i++)
                        for (int j = 0; j < 9; j++)
                            Verts[i, j] = br.ReadStruct<SMVert>();
                    break;
                default:
                    for (int i = 0; i < 9; i++)
                        for (int j = 0; j < 9; j++)
                            Verts[i, j] = br.ReadStruct<SWVert>();
                    break;
            }

            // Read flags.
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                    Flags[i, j] = br.ReadByte();

            NFlowvs = br.ReadUInt32();
            Flowvs = br.ReadStructArray<SWFlowv>(2);
        }

        public float GetHeight(int y, int x)
        {
            if (Verts[y, x] is SMVert magmaVert)
                return magmaVert.Height;
            else if (Verts[y, x] is SWVert waterVert)
                return waterVert.Height;
            else
                return 0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SWVert : IStringDescriptor
    {
        public byte Depth;
        public byte Flow0Pct;
        public byte Flow1Pct;
        public byte Filler;
        public float Height;

        public override string ToString() => $"Depth: {Depth}, Flow0Pct: {Flow0Pct}, Flow1Pct: {Flow1Pct}, Height: {Height}";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SOVert : IStringDescriptor
    {
        public byte Depth;
        public byte Foam;
        public byte Wet;
        public byte Filler;
        public float Height;

        public override string ToString() => $"Depth: {Depth}, Foam: {Foam}, Wet: {Wet}, Height: {Height}";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SMVert : IStringDescriptor
    {
        public ushort S;
        public ushort T;
        public float Height;

        public override string ToString() => $"S: {S}, T: {T}, Height: {Height}";
    }

    public struct SWFlowv
    {
        public CSphere Sphere;
        public C3Vector Dir;
        public float Velocity;
        public float Amplitude;
        public float Frequency;
    }
}
