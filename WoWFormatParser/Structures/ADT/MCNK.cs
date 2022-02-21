using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WoWFormatParser.Helpers;
using WoWFormatParser.Structures.Common;
using WoWFormatParser.Structures.Interfaces;

namespace WoWFormatParser.Structures.ADT
{
    public class MCNK : IVersioned
    {
        public MCNK_Flags Flags;               // See SMChunkFlags
        public uint IndexX;
        public uint IndexY;
        public float? Radius;
        public int NLayers;
        public int NDoodadRefs;
        public uint OffsHeight;                // MCVT
        public uint OffsNormal;                // MCNR
        public uint OffsLayer;                 // MCLY
        public uint OffsRefs;                  // MCRF
        public uint OffsAlpha;                 // MCAL
        public int SizeAlpha;
        public uint OffsShadow;                // MCSH
        public int SizeShadow;
        public uint Areaid;
        public int NMapObjRefs;
        public ushort Holes;
        public ushort Unk_0x12;
        public ushort[] PredTex; //[8];
        public byte[] NoEffectDoodad; //[8];
        public uint OffsSndEmitters;           // MCSE
        public int NSndEmitters;
        public uint OffsLiquid;                // MLIQ
        public uint? SizeLiquid;

        public NonUniformArray<float> HeightMap;
        public NonUniformArray<C3bVector> Normals;
        public float[,] V9 = new float[9, 9];
        public float[,] V8 = new float[8, 8];
        public float[] AlphaHeights = new float[8 * 8 + 9 * 9];
        public C3Vector[] AlphaNormals = new C3Vector[9 * 9 + 8 * 8];

        public uint[] AlphaMaps;
        public MCLY[] TextureLayers;
        public uint[] MapObjReferences;
        public uint[] DoodadReferences;
        public int[] Shadows;
        public MCLQ[] Liquids;
        public MCSE[] SoundEmitters;


        public MCNK(BinaryReader br, uint build, int size)
        {
            long endPos = br.BaseStream.Position + size;
            long relativeStart = br.BaseStream.Position;
            long relativeEnd = endPos - relativeStart;
            bool isAlpha = build < 3592;

            Flags = br.ReadEnum<MCNK_Flags>();
            IndexX = br.ReadUInt32();
            IndexY = br.ReadUInt32();
            if (isAlpha)
                Radius = br.ReadSingle();
            NLayers = br.ReadInt32();
            NDoodadRefs = br.ReadInt32();
            OffsHeight = br.ReadUInt32();
            OffsNormal = br.ReadUInt32();
            OffsLayer = br.ReadUInt32();
            OffsRefs = br.ReadUInt32();
            OffsAlpha = br.ReadUInt32();
            SizeAlpha = br.ReadInt32();
            OffsShadow = br.ReadUInt32();
            SizeShadow = br.ReadInt32();
            Areaid = br.ReadUInt32();
            NMapObjRefs = br.ReadInt32();
            Holes = br.ReadUInt16();
            Unk_0x12 = br.ReadUInt16();
            PredTex = br.ReadStructArray<ushort>(8);
            NoEffectDoodad = br.ReadBytes(8);
            OffsSndEmitters = br.ReadUInt32();
            NSndEmitters = br.ReadInt32();
            OffsLiquid = br.ReadUInt32();
            SizeLiquid = br.ReadUInt32();
            br.BaseStream.Position += 20; // padding

            if (build <= 3368)
                SizeLiquid = null;

            // alpha build's offsets are exclusive of header data
            if (isAlpha)
            {
                relativeStart = br.BaseStream.Position;
                relativeEnd = endPos - relativeStart;
            }

            Read(br, build, relativeStart, relativeEnd);
        }

        public void Read(BinaryReader br, uint build, long relativeStart, long relativeEnd)
        {
            bool hasLiquids = (Flags & MCNK_Flags.HasLiquid) != 0;
            bool isAlpha = build < 3592;

            foreach (var (Offset, Token) in GetOffsets(relativeEnd, isAlpha))
            {
                string token = Token;
                br.BaseStream.Position = relativeStart + Offset;

                // use chunk headers when possible
                if (!isAlpha || Token == "MCRF" || Token == "MCLY")
                {
                    var chunk = br.ReadIffChunk(true);
                    token = chunk.Token;

                    // welcome to the world of blizzard
                    bool isliquid = Token == "MCLQ" && hasLiquids;
                    if (chunk.Size <= 0 && !isliquid)
                        continue;
                }

                switch (token)
                {
                    case "MCVT":
                        if (isAlpha)
                            FillAlphaHeightMap(br);
                        else
                            HeightMap = ReadVTNR<float>(br, isAlpha);
                        break;
                    case "MCNR":
                        if (isAlpha)
                            FillAlphaNormals(br);
                        else
                            Normals = ReadVTNR<C3bVector>(br, isAlpha);
                        break;
                    case "MCAL":
                        AlphaMaps = br.ReadStructArray<uint>(SizeAlpha / 4);
                        break;
                    case "MCLY":
                        TextureLayers = br.ReadArray(NLayers, () => new MCLY(br));
                        break;
                    case "MCRF":
                        DoodadReferences = NDoodadRefs > 0 ? br.ReadStructArray<uint>(NDoodadRefs) : null;
                        MapObjReferences = NMapObjRefs > 0 ? br.ReadStructArray<uint>(NMapObjRefs) : null;
                        break;
                    case "MCSH":
                        Shadows = br.ReadStructArray<int>(SizeShadow / 4);
                        break;
                    case "MCLQ":
                        Liquids = GetLiquidFlags().Select(flag => new MCLQ(br, flag))?.ToArray();
                        break;
                    case "MCSE":
                        SoundEmitters = br.ReadStructArray<MCSE>(NSndEmitters);
                        break;
                    default:
                        throw new Exception();
                }
            }
        }

        public void FillAlphaNormals(BinaryReader reader)
        {
            // Interleave vertices (9-8-9-8)
            using (BinaryReader outerVerticesReader = new BinaryReader(new MemoryStream(reader.ReadBytes(243)))) // 81 * 3bytes
            using (BinaryReader innerVerticesReader = new BinaryReader(new MemoryStream(reader.ReadBytes(192)))) // 64 * 3bytes
            {
                int hIndex = 0;
                while (outerVerticesReader.BaseStream.Position != outerVerticesReader.BaseStream.Length)
                {
                    for (int i = 0; i < 9; i++, hIndex++)
                    {
                        var normalZ = outerVerticesReader.ReadSByte();
                        var normalX = outerVerticesReader.ReadSByte();
                        var normalY = outerVerticesReader.ReadSByte();
                        AlphaNormals[hIndex] = new C3Vector() { X = -(float)normalX / 127.0f, Y = normalY / 127.0f, Z = -(float)normalZ / 127.0f };
                    }

                    // If we reached the end, skip inner vertices.
                    if (innerVerticesReader.BaseStream.Position != innerVerticesReader.BaseStream.Length)
                    {
                        for (int j = 0; j < 8; j++, hIndex++)
                        {
                            var normalZ = innerVerticesReader.ReadSByte();
                            var normalX = innerVerticesReader.ReadSByte();
                            var normalY = innerVerticesReader.ReadSByte();
                            AlphaNormals[hIndex] = new C3Vector() { X = -(float)normalX / 127.0f, Y = normalY / 127.0f, Z = -(float)normalZ / 127.0f };
                        }
                    }
                }
            }

            /*
            // TODO: PAD bytes do not match with the comment below.
             * About pad: 0.5.3.3368 lists this as padding always 0 112 245 18 0 8 0 0 0 84 245 18 0.
            */
            var pad = reader.ReadBytes(13);
        }

        public void FillAlphaHeightMap(BinaryReader reader)
        {
            // Interleaved 9-8-9-8
            var bytes_v9 = reader.ReadBytes(324);
            var bytes_v8 = reader.ReadBytes(256);
            using (MemoryStream msV9 = new MemoryStream(bytes_v9))
            using (MemoryStream msV8 = new MemoryStream(bytes_v8))
            using (BinaryReader v9Reader = new BinaryReader(msV9))
            using (BinaryReader v8Reader = new BinaryReader(msV8))
            {
                int hIndex = 0;
                while (v9Reader.BaseStream.Position != v9Reader.BaseStream.Length)
                {
                    for (int i = 0; i < 9; i++, hIndex++)
                        AlphaHeights[hIndex] = v9Reader.ReadSingle();

                    // If we reached the end, skip inner vertices.
                    if (v8Reader.BaseStream.Position != v8Reader.BaseStream.Length)
                        for (int j = 0; j < 8; j++, hIndex++)
                            AlphaHeights[hIndex] = v8Reader.ReadSingle();
                }
            }

            // Segmented.
            reader.BaseStream.Position -= (AlphaHeights.Length * 4);
            using (BinaryReader outerVerticesReader = new BinaryReader(new MemoryStream(reader.ReadBytes(324)))) // 81 floats * 4bytes
                for (int x = 0; x < 9; x++)
                    for (int y = 0; y < 9; y++)
                        V9[x, y] = outerVerticesReader.ReadSingle();

            using (BinaryReader innerVerticesReader = new BinaryReader(new MemoryStream(reader.ReadBytes(256)))) // 64 floats * 4bytes
                for (int x = 0; x < 8; x++)
                    for (int y = 0; y < 8; y++)
                        V8[x, y] = innerVerticesReader.ReadSingle();
        }

        /// <summary>
        /// 145 Floats for the 9 x 9 and 8 x 8 grid of height data.
        /// </summary>
        public float[] GetLowResMapArray()
        {
            var heights = new float[81];

            for (var r = 0; r < 17; r++)
            {
                if (r % 2 != 0) continue;
                for (var c = 0; c < 9; c++)
                {
                    var count = ((r / 2) * 9) + ((r / 2) * 8) + c;
                    heights[c + ((r / 2) * 8)] = heights[count];
                }
            }
            return heights;
        }

        private float[,] lowResHeightsMatrix;
        private float[,] highResHeightsMatrix;
        /// <summary>
        /// 145 Floats for the 9 x 9 and 8 x 8 grid of height data.
        /// </summary>
        public float[,] GetLowResMapMatrix()
        {
            if (lowResHeightsMatrix != null)
                return lowResHeightsMatrix;

            // *  1    2    3    4    5    6    7    8    9       Row 0
            // *    10   11   12   13   14   15   16   17         Row 1
            // *  18   19   20   21   22   23   24   25   26      Row 2
            // *    27   28   29   30   31   32   33   34         Row 3
            // *  35   36   37   38   39   40   41   42   43      Row 4
            // *    44   45   46   47   48   49   50   51         Row 5
            // *  52   53   54   55   56   57   58   59   60      Row 6
            // *    61   62   63   64   65   66   67   68         Row 7
            // *  69   70   71   72   73   74   75   76   77      Row 8
            // *    78   79   80   81   82   83   84   85         Row 9
            // *  86   87   88   89   90   91   92   93   94      Row 10
            // *    95   96   97   98   99   100  101  102        Row 11
            // * 103  104  105  106  107  108  109  110  111      Row 12
            // *   112  113  114  115  116  117  118  119         Row 13
            // * 120  121  122  123  124  125  126  127  128      Row 14
            // *   129  130  131  132  133  134  135  136         Row 15
            // * 137  138  139  140  141  142  143  144  145      Row 16

            // We only want even rows (starting at 0)
            lowResHeightsMatrix = new float[9, 9];

            var index = 0;
            for (var x = 0; x < 9; x++)
            {
                for (var y = 0; y < 9; y++)
                    lowResHeightsMatrix[x, y] = AlphaHeights[index++];
                index += 8;
            }

            return lowResHeightsMatrix;
        }

        public float[,] GetHighResMapMatrix()
        {
            if (highResHeightsMatrix != null)
                return highResHeightsMatrix;

            // *  1    2    3    4    5    6    7    8    9       Row 0
            // *    10   11   12   13   14   15   16   17         Row 1
            // *  18   19   20   21   22   23   24   25   26      Row 2
            // *    27   28   29   30   31   32   33   34         Row 3
            // *  35   36   37   38   39   40   41   42   43      Row 4
            // *    44   45   46   47   48   49   50   51         Row 5
            // *  52   53   54   55   56   57   58   59   60      Row 6
            // *    61   62   63   64   65   66   67   68         Row 7
            // *  69   70   71   72   73   74   75   76   77      Row 8
            // *    78   79   80   81   82   83   84   85         Row 9
            // *  86   87   88   89   90   91   92   93   94      Row 10
            // *    95   96   97   98   99   100  101  102        Row 11
            // * 103  104  105  106  107  108  109  110  111      Row 12
            // *   112  113  114  115  116  117  118  119         Row 13
            // * 120  121  122  123  124  125  126  127  128      Row 14
            // *   129  130  131  132  133  134  135  136         Row 15
            // * 137  138  139  140  141  142  143  144  145      Row 16

            // We only want odd rows (starting at 1).
            highResHeightsMatrix = new float[8, 8];

            var index = 9;
            for (var x = 0; x < 8; x++)
            {
                for (var y = 0; y < 8; y++)
                    highResHeightsMatrix[x, y] = AlphaHeights[index++];
                index += 9;
            }

            return highResHeightsMatrix;
        }

        private NonUniformArray<T> ReadVTNR<T>(BinaryReader br, bool isAlpha) where T : struct
        {
            if (isAlpha)
            {
                int[] cols = new[] { 9, 9, 9, 9, 9, 9, 9, 9, 9, 8, 8, 8, 8, 8, 8, 8, 8 };
                return new NonUniformArray<T>(br, 17, cols);
            }
            else
            {
                return new NonUniformArray<T>(br, 17, 9, 8);
            }
        }

        private List<(long Offset, string Token)> GetOffsets(long relativeEnd, bool isAlpha)
        {
            int offset = !isAlpha ? 8 : 0;

            var offsets = new List<(long Offset, string Token)>
            {
                (OffsHeight - offset, "MCVT"),
                (OffsNormal - offset, "MCNR"),
                (OffsLayer - offset, "MCLY"),
                (OffsRefs - offset, "MCRF"),
            };

            if (SizeAlpha > 0)
                offsets.Add((OffsAlpha - offset, "MCAL"));

            if (SizeShadow > 0)
                offsets.Add((OffsShadow - offset, "MCSH"));

            if (NSndEmitters > 0)
                offsets.Add((OffsSndEmitters - offset, "MCSE"));

            if ((Flags & MCNK_Flags.HasLiquid) != 0 || SizeLiquid > 8)
                offsets.Add((OffsLiquid - offset, "MCLQ"));

            offsets.RemoveAll(x => x.Offset >= relativeEnd);
            offsets.Sort((x, y) => x.Offset.CompareTo(y.Offset));

            return offsets;
        }

        private IEnumerable<MCNK_Flags> GetLiquidFlags()
        {
            for (int i = 0; i < 4; i++)
            {
                MCNK_Flags flag = (MCNK_Flags)(1 << (2 + i));
                if (Flags.HasFlag(flag))
                    yield return flag;
            }
        }
    }

    [Flags]
    public enum MCNK_Flags : uint
    {
        None = 0,
        HasBakedShadows = 1,
        Impassible = 2,
        IsRiver = 4,
        IsOcean = 8,
        IsMagma = 16,
        IsSlime = 32,
        HasVertexShading = 64,
        Unknown_0x80 = 128,
        DoNotRepairAlphaMaps = 32768,
        UsesHighResHoles = 65536,
        HasLiquid = IsRiver | IsOcean | IsMagma | IsSlime
    }
}
