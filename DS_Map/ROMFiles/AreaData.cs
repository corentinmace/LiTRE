using System.IO;
using static LiTRE.RomInfo;

namespace LiTRE.ROMFiles {
    /// <summary>
    /// Class to store area data in Pokémon NDS games
    /// </summary>
    public class AreaData : RomFile {
        internal static readonly byte TYPE_INDOOR = 0;
        internal static readonly byte TYPE_OUTDOOR = 1;

        #region Fields (2)
        public ushort buildingsTileset;
        public ushort mapBaseTileset;
        public byte mapTilesetSpring;
        public byte mapTilesetSummer;
        public byte mapTilesetFall;
        public byte mapTilesetWinter;
        public ushort lightType; //using an overabundant size. HGSS only needs a byte

   
        public byte areaType = TYPE_OUTDOOR; //HGSS ONLY
        public ushort dynamicTextureType;
      
        #endregion

        #region Constructors (1)
        public AreaData(Stream data)
        {
            var tempBuildingTileset = 0;
            var tempMapBaseTileset = 0;
            var tempMapTilesetSummer = 0;
            var tempMapTilesetFall = 0;
            var tempMapTilesetWinter = 0;
            var tempLightType = 0;
            using (BinaryReader reader = new BinaryReader(data)) {
                tempBuildingTileset = reader.ReadUInt16();
                tempMapBaseTileset = reader.ReadByte();
                

                if (RomInfo.gameFamily == GameFamilies.HGSS) {
                    dynamicTextureType = reader.ReadUInt16();
                    areaType = reader.ReadByte();
                    lightType = reader.ReadByte();
                } else {
                    tempMapTilesetSummer = reader.ReadByte();
                    tempMapTilesetFall = reader.ReadByte();
                    tempMapTilesetWinter = reader.ReadByte();
                    tempLightType = reader.ReadUInt16();
                }
            }
            
            buildingsTileset = (ushort)tempBuildingTileset;
            lightType = (ushort)tempLightType;
            if (tempMapTilesetWinter == 255)
            {
                mapBaseTileset = (ushort)((tempMapTilesetSummer << 8) | tempMapBaseTileset);
                mapTilesetSpring = 0;
                mapTilesetSummer = 0;
                
            }
            else
            {
                mapBaseTileset = 0;
                mapTilesetSpring = (byte)tempMapBaseTileset;
                mapTilesetSummer = (byte)tempMapTilesetSummer;
            }
            
            mapTilesetFall = (byte)tempMapTilesetFall;
            mapTilesetWinter = (byte)tempMapTilesetWinter;
        }
        public AreaData (byte ID) : this(new FileStream(RomInfo.gameDirs[DirNames.areaData].unpackedDir + "//" + ID.ToString("D4"), FileMode.Open)) {}
        #endregion

        #region Methods (1)
        public override byte[] ToByteArray() {
            MemoryStream newData = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(newData)) {
                writer.Write(buildingsTileset);
                if (mapTilesetWinter == byte.MaxValue)
                {
                    writer.Write((ushort)mapBaseTileset);
                }
                else
                {
                    writer.Write((sbyte)mapTilesetSpring);
                    writer.Write((sbyte)mapTilesetSummer);
                }


                if (RomInfo.gameFamily == GameFamilies.HGSS) {
                    writer.Write(dynamicTextureType);
                    writer.Write(areaType);
                    writer.Write((byte)lightType);
                } else {
                    writer.Write((sbyte)mapTilesetFall);
                    writer.Write((sbyte)mapTilesetWinter);
                    writer.Write((ushort)lightType);
                }
            }
            return newData.ToArray();
        }

        public void SaveToFileDefaultDir(int IDtoReplace, bool showSuccessMessage = true) {
            SaveToFileDefaultDir(DirNames.areaData, IDtoReplace, showSuccessMessage);
        }

        public void SaveToFileExplorePath(string suggestedFileName, bool showSuccessMessage = true) {
            SaveToFileExplorePath("Gen IV Area Data File", "bin", suggestedFileName, showSuccessMessage);
        }
        #endregion
    }
}