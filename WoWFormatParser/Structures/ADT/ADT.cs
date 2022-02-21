﻿using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using WoWFormatParser.Helpers;
using WoWFormatParser.Structures.Interfaces;

namespace WoWFormatParser.Structures.ADT
{
    public sealed class ADT : Format, IVersioned
    {
        [JsonIgnore]
        private readonly bool IsAlphaFormat = false;
        [JsonIgnore]
        internal readonly long Offset;

        public uint Version;
        public string[] WorldModelFileNames;
        public string[] TextureFileNames;
        public string[] ModelFileNames;
        public uint[] ModelFileNameIndices;
        public uint[] WorldModelFileNameIndices;
        public WDT.MODF[] MapObjDefinitions;
        public MDDF[] MapModelDefinitions;
        public MHDR MapHeader;
        public MCIN[,] ChunkInfo;
        public MCNK[,] MapChunks;

        public ADT(BinaryReader br, uint build, long offset = 0)
        {
            IsAlphaFormat = build < 3592;
            Offset = offset;

            MCNK[,] _MapChunks = null;
            var chunk_x = 0;
            var chunk_y = 0;

            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                var (Token, Size) = br.ReadIffChunk(true);
                if (Size <= 0)
                    continue;

                switch (Token)
                {
                    case "MVER":
                        Version = br.ReadUInt32();
                        break;
                    case "MWMO":
                        WorldModelFileNames = br.ReadString(Size).Split('\0');
                        break;
                    case "MTEX":
                        TextureFileNames = br.ReadString(Size).Split('\0');
                        break;
                    case "MMDX":
                        ModelFileNames = br.ReadString(Size).Split('\0');
                        break;
                    case "MMID":
                        ModelFileNameIndices = br.ReadStructArray<uint>(Size / 4);
                        break;
                    case "MWID":
                        WorldModelFileNameIndices = br.ReadStructArray<uint>(Size / 4);
                        break;
                    case "MODF":
                        MapObjDefinitions = br.ReadArray(Size / 64, () => new WDT.MODF(br));
                        break;
                    case "MHDR":
                        MapHeader = br.ReadStruct<MHDR>();
                        break;
                    case "MCIN":
                        ChunkInfo = br.ReadJaggedArray(16, 16, () => br.ReadStruct<MCIN>());
                        break;
                    case "MDDF":
                        MapModelDefinitions = br.ReadArray(Size / 0x24, () => new MDDF(br));
                        break;
                    case "MCNK":
                        if (_MapChunks == null)
                            _MapChunks = new MCNK[16, 16];

                        // Place chunks on their correct position.
                        _MapChunks[chunk_x, chunk_y] = new MCNK(br, build, Size);
                        if (chunk_y++ == 15)
                        {
                            chunk_x++;
                            chunk_y = 0;
                        }
                        break;
                    default:
                        throw new NotImplementedException("Unknown token " + Token);
                }
            }

            ValidateIsRead(br, br.BaseStream.Length);

            if (_MapChunks != null)
                MapChunks = _MapChunks;
        }

        private void ValidateIsRead(BinaryReader br, long length)
        {
            if (br.BaseStream.Position != length)
            {
                if (!IsAlphaFormat)
                {
                    do
                    {
                        if (br.ReadIffChunk().Size > 0)
                            throw new UnreadContentException();
                    }
                    while (br.BaseStream.Position < length);
                }
                else
                {
                    throw new UnreadContentException();
                }
            }
        }
    }
}
