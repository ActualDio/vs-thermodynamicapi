using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using ThermodynamicApi.ThermoDynamics;

namespace ThermodynamicApi.ApiHelper
{
    public class ThermodynamicHelper : ModSystem
    {
        private ICoreAPI api;

        //DO NOT CHANGE, if this number does not match up to one on Gas API github this helper is outdated
        const double VersionNumber = 1.0;

        public override void Start(ICoreAPI papi)
        {
            base.Start(api);

            api = papi;

            try
            {
                IAsset asset = api.Assets.Get("thermoapi:config/gases.json");
                LiteGasDict = asset.ToObject<Dictionary<string, GasInfoLite>>();
                if (LiteGasDict == null) LiteGasDict = new Dictionary<string, GasInfoLite>();
            }
            catch
            {
                LiteGasDict = new Dictionary<string, GasInfoLite>();
            }
        }

        static Dictionary<string, GasInfoLite> LiteGasDict;

        //Returns the thermodynamic info for the entire chunk; Does not create fluids or chunk data
        public Dictionary<int, Dictionary<string, MaterialStates>> GetThermodynamicInfoForChunk(BlockPos pos)
        {
            if (!api.ModLoader.IsModEnabled("thermoapi")) return null;
            byte[] data;

            IWorldChunk chunk = api.World.BlockAccessor.GetChunkAtBlockPos(pos);
            if (chunk == null) return null;

            data = chunk.GetModdata("thermoinfo");

            Dictionary<int, Dictionary<string, MaterialStates>> thermoInfoOfChunk = null;

            if (data != null)
            {
                try
                {
                    thermoInfoOfChunk = SerializerUtil.Deserialize<Dictionary<int, Dictionary<string, MaterialStates>>>(data);
                }
                catch (Exception)
                {
                    thermoInfoOfChunk = null;
                }
            }

            return thermoInfoOfChunk;
        }

        //Returns the thermodynamic info for the entire chunk; Does not create fluids or chunk data
        public Dictionary<int, Dictionary<string, MaterialStates>> GetThermodynamicInfoForChunk(IWorldChunk chunk)
        {
            if (!api.ModLoader.IsModEnabled("thermoapi")) return null;
            byte[] data;

            if (chunk == null) return null;

            data = chunk.GetModdata("thermoinfo");

            Dictionary<int, Dictionary<string, MaterialStates>> thermoInfoOfChunk = null;

            if (data != null)
            {
                try
                {
                    thermoInfoOfChunk = SerializerUtil.Deserialize<Dictionary<int, Dictionary<string, MaterialStates>>>(data);
                }
                catch (Exception)
                {
                    thermoInfoOfChunk = null;
                }
            }

            return thermoInfoOfChunk;
        }

        //Returns thermodynamic info for a particular block position
        public Dictionary<string, MaterialStates> GetThermodynamicInfo(BlockPos pos)
        {
            if (!api.ModLoader.IsModEnabled("thermoapi")) return null;
            Dictionary<int, Dictionary<string, MaterialStates>> thermoInfoOfChunk = GetThermodynamicInfoForChunk(pos);
            if (thermoInfoOfChunk == null) return null;

            int index3d = toLocalIndex(pos);
            if (!thermoInfoOfChunk.ContainsKey(index3d)) return null;

            return thermoInfoOfChunk[index3d];
        }

        //Returns the thermodynamic info of the specified fluid at a position if it is present
        public MaterialStates? GetFluidThermodynamicInfo(BlockPos pos, string name)
        {
            if (!api.ModLoader.IsModEnabled("thermoapi")) return null;
            Dictionary<string, MaterialStates> fluidsHere = GetThermodynamicInfo(pos);

            if (fluidsHere == null || !fluidsHere.ContainsKey(name)) return null;

            return fluidsHere[name];
        }

        //Serializes and sends a fluid spread event on the bus
        public void SendFluidSpread(BlockPos pos, Dictionary<string, MaterialStates> fluids = null)
        {
            if (!api.ModLoader.IsModEnabled("thermoapi") || api.Side != EnumAppSide.Server) return;
            (api as ICoreServerAPI)?.Event.PushEvent("spreadFluid", SerializeFluidTreeData(pos, fluids));
        }

        //Serializes a fluid spreading event
        public TreeAttribute SerializeFluidTreeData(BlockPos pos, Dictionary<string, MaterialStates> fluids)
        {
            if (!api.ModLoader.IsModEnabled("thermoapi")) return null;
            if (pos == null) return null;

            TreeAttribute tree = new TreeAttribute();

            tree.SetBlockPos("pos", pos);

            if (fluids != null && fluids.Count > 0)
            {
                TreeAttribute sFluids = new TreeAttribute();

                foreach (var fluid in fluids)
                {
                    sFluids.SetBytes(fluid.Key, SerializerUtil.Serialize(fluid.Value));
                }

                tree.SetAttribute("thermoinfo", sFluids);
            }

            return tree;
        }

        //Deserializes a fluid spreading event
        public static Dictionary<string, MaterialStates> DeserializeFluidTreeData(IAttribute data, out BlockPos pos)
        {
            TreeAttribute tree = data as TreeAttribute;
            pos = tree?.GetBlockPos("pos");
            ITreeAttribute thermoInfo = tree?.GetTreeAttribute("thermoinfo");

            if (pos == null) return null;
            Dictionary<string, MaterialStates> dFluids = null;

            if (thermoInfo != null)
            {
                dFluids = new Dictionary<string, MaterialStates>();

                foreach (var fluid in thermoInfo)
                {
                    MaterialStates? value = SerializerUtil.Deserialize<MaterialStates>(thermoInfo.GetBytes(fluid.Key));
                    if (value.GetValueOrDefault() == default(MaterialStates)) continue;

                    dFluids.Add(fluid.Key, value.Value);
                }
            }

            return dFluids;
        }

        //Cleanly merges a fluid into an already existing fluid dictionary
        public static void MergeFluidIntoDict(string fluidName, MaterialStates? fluidInfo, ref Dictionary<string, MaterialStates> dest)
        {
            if (fluidName == null || fluidInfo.GetValueOrDefault() == default || dest == null) return;

            if (!dest.ContainsKey(fluidName))
            {
                dest.Add(fluidName, fluidInfo);
            }
            else
            {
                dest[fluidName] += fluidInfo;
            }
        }

        //Cleanly merges two fluid dictionaries together
        public static void MergeGasDicts(Dictionary<string, float> source, ref Dictionary<string, float> dest)
        {
            if (source == null || dest == null) return;

            foreach (var gas in source)
            {
                if (gas.Key == "RADIUS")
                {
                    if (!dest.ContainsKey(gas.Key)) dest.Add(gas.Key, gas.Value);
                    else if (dest[gas.Key] < gas.Value) dest[gas.Key] = gas.Value;
                }
                else MergeFluidIntoDict(gas.Key, gas.Value, ref dest);
            }
        }

        //Returns the air quality for this position, ranging from 1 to -1. Postive values allow breathing, negative values suffocate
        public float GetAirAmount(BlockPos pos)
        {
            if (!api.ModLoader.IsModEnabled("thermoapi")) return 1;
            Dictionary<string, float> gasesHere = GetThermodynamicInfo(pos);

            if (gasesHere == null) return 1;

            float conc = 0;

            foreach (var gas in gasesHere)
            {
                if (LiteGasDict.ContainsKey(gas.Key))
                {
                    if (LiteGasDict[gas.Key] != null) conc += gas.Value * LiteGasDict[gas.Key].QualityMult; else conc += gas.Value;
                }
            }

            if (conc >= 2) return -1;
            if (conc < 0) return 1;

            return 1 - conc;
        }

        //Returns the aciditiy for an area between 0 and 1
        public float GetAcidity(BlockPos pos)
        {
            if (!api.ModLoader.IsModEnabled("thermoapi")) return 0;
            Dictionary<string, float> gasesHere = GetThermodynamicInfo(pos);

            if (gasesHere == null) return 0;

            float conc = 0;

            foreach (var gas in gasesHere)
            {
                if (LiteGasDict.ContainsKey(gas.Key))
                {
                    if (LiteGasDict[gas.Key] != null && LiteGasDict[gas.Key].Acidic) conc += gas.Value;
                    if (conc >= 1) return 1;
                }
            }

            return conc;
        }

        //Returns whether there is a flammable amount of fluid at this position
        public bool IsVolatile(BlockPos pos)
        {
            if (!api.ModLoader.IsModEnabled("thermoapi")) return false;
            Dictionary<string, float> gasesHere = GetThermodynamicInfo(pos);

            if (gasesHere == null) return false;

            foreach (var gas in gasesHere)
            {
                if (LiteGasDict.ContainsKey(gas.Key))
                {
                    if (LiteGasDict[gas.Key].FlammableAmount > 0 && gas.Value >= LiteGasDict[gas.Key].FlammableAmount) return true;
                }
            }

            return false;
        }

        //Returns whether there is enough explosive fluid here to explode
        public bool ShouldExplode(BlockPos pos)
        {
            if (!api.ModLoader.IsModEnabled("thermoapi")) return false;
            Dictionary<string, float> gasesHere = GetThermodynamicInfo(pos);

            if (gasesHere == null) return false;

            foreach (var gas in gasesHere)
            {
                if (LiteGasDict.ContainsKey(gas.Key))
                {
                    if (LiteGasDict[gas.Key].ExplosionAmount <= gas.Value) return true;
                }
            }

            return false;
        }

        //Determines if there is enough of the fluid to be toxic
        public bool IsToxic(string name, float amount)
        {
            if (!api.ModLoader.IsModEnabled("thermoapi")) return false;
            if (!LiteGasDict.ContainsKey(name)) return true;

            return amount > LiteGasDict[name].ToxicAt;
        }

        //Collects gases and voids them in the world and returns them as a table
        //Note: Because this happens on the main thread and fluid spreading happens on an off thread, it may be somewhat inaccurate
        public Dictionary<string, float> CollectGases(BlockPos pos, int radius, string[] gasFilter)
        {
            if (!api.ModLoader.IsModEnabled("thermoapi") || api.Side != EnumAppSide.Server) return null;
            
            IBlockAccessor blockAccessor = api.World.BlockAccessor;
            if (pos.Y < 1 || pos.Y > blockAccessor.MapSizeY) return null;

            Dictionary<string, float> result = new Dictionary<string, float>();
            Queue<Vec3i> checkQueue = new Queue<Vec3i>();
            Dictionary<Vec3i, IWorldChunk> chunks = new Dictionary<Vec3i, IWorldChunk>();
            Dictionary<Vec3i, Dictionary<int, Dictionary<string, float>>> gasChunks = new Dictionary<Vec3i, Dictionary<int, Dictionary<string, float>>>();
            HashSet<BlockPos> markedPositions = new HashSet<BlockPos>();
            Dictionary<int, Block> blocks = new Dictionary<int, Block>();
            Cuboidi bounds = new Cuboidi(pos.X - radius, pos.Y - radius, pos.Z - radius, pos.X + radius, pos.Y + radius, pos.Z + radius);
            BlockPos curPos = pos.Copy();
            BlockFacing[] faces = BlockFacing.ALLFACES;

            for (int x = bounds.MinX / blockAccessor.ChunkSize; x <= bounds.MaxX / blockAccessor.ChunkSize; x++)
            {
                for (int y = bounds.MinY / blockAccessor.ChunkSize; y <= bounds.MaxY / blockAccessor.ChunkSize; y++)
                {
                    for (int z = bounds.MinZ / blockAccessor.ChunkSize; z <= bounds.MaxZ / blockAccessor.ChunkSize; z++)
                    {
                        IWorldChunk chunk = blockAccessor.GetChunk(x, y, z);

                        Vec3i currentChunkPos = new Vec3i(x, y, z);
                        chunks.Add(currentChunkPos, chunk);
                        gasChunks.Add(currentChunkPos, GetGasesForChunk(chunk));

                    }
                }
            }
            if (chunks.Count < 1) return result;

            Vec3i originChunkVec = new Vec3i(pos.X / blockAccessor.ChunkSize, pos.Y / blockAccessor.ChunkSize, pos.Z / blockAccessor.ChunkSize);
            if (chunks[originChunkVec] == null) return null;
            checkQueue.Enqueue(pos.ToVec3i());
            markedPositions.Add(pos.Copy());
            Block starter = blockAccessor.GetBlock(pos);
            blocks.Add(starter.BlockId, starter);
            if (gasChunks[originChunkVec] != null && gasChunks[originChunkVec].ContainsKey(toLocalIndex(pos)))
            {
                Dictionary<string, float> gasesHere = gasChunks[originChunkVec][toLocalIndex(pos)];
                if (gasFilter == null) MergeGasDicts(gasesHere, ref result);
                else
                {
                    foreach (var gas in gasesHere)
                    {
                        if (gasFilter.Contains(gas.Key)) MergeFluidIntoDict(gas.Key, gas.Value, ref result);
                    }
                }
            }

            while (checkQueue.Count > 0)
            {
                Vec3i bpos = checkQueue.Dequeue();
                Vec3i parentChunkVec = new Vec3i(bpos.X / blockAccessor.ChunkSize, bpos.Y / blockAccessor.ChunkSize, bpos.Z / blockAccessor.ChunkSize);

                Block parent = null;
                IWorldChunk parentChunk = chunks[parentChunkVec];
                if (!blocks.ContainsKey(parentChunk.Unpack_AndReadBlock(toLocalIndex(bpos.AsBlockPos)))) continue;

                parent = blocks[parentChunk.Unpack_AndReadBlock(toLocalIndex(bpos.AsBlockPos))];

                foreach (BlockFacing facing in faces)
                {
                    if (SolidCheck(parent, facing)) continue;
                    curPos.Set(bpos.X + facing.Normali.X, bpos.Y + facing.Normali.Y, bpos.Z + facing.Normali.Z);
                    if (!bounds.Contains(curPos) || markedPositions.Contains(curPos)) continue;
                    if (curPos.Y < 0 || curPos.Y > blockAccessor.MapSizeY) continue;

                    Vec3i curChunkVec = new Vec3i(curPos.X / blockAccessor.ChunkSize, curPos.Y / blockAccessor.ChunkSize, curPos.Z / blockAccessor.ChunkSize);
                    int chunkBid = toLocalIndex(curPos);
                    Block atPos = null;
                    IWorldChunk chunk = chunks[curChunkVec];

                    if (chunk == null) continue;

                    int blockId = chunk.Unpack_AndReadBlock(toLocalIndex(curPos));

                    if (!blocks.TryGetValue(blockId, out atPos)) atPos = blocks[blockId] = blockAccessor.GetBlock(blockId);

                    if (SolidCheck(atPos, facing.Opposite)) continue;

                    if (gasChunks[curChunkVec] != null && gasChunks[curChunkVec].ContainsKey(chunkBid))
                    {
                        Dictionary<string, float> gasesHere = gasChunks[curChunkVec][chunkBid];
                        if (gasFilter == null) MergeGasDicts(gasesHere, ref result);
                        else
                        {
                            foreach (var gas in gasesHere)
                            {
                                if (gasFilter.Contains(gas.Key)) MergeFluidIntoDict(gas.Key, gas.Value, ref result);
                            }
                        }
                    }

                    markedPositions.Add(curPos.Copy());
                    checkQueue.Enqueue(curPos.ToVec3i());
                }
            }

            Dictionary<string, float> reverse = new Dictionary<string, float>();

            foreach (var gas in result) reverse.Add(gas.Key, -gas.Value);
            reverse.Add("IGNORELIQUIDS", -100);
            reverse.Add("RADIUS", radius);
            SendGasSpread(pos, reverse);

            return result;
        }

        //Returns the display name of the fluid if it has one
        public static string GetGasDisplayName(string gas)
        {
            string results = Lang.GetIfExists("thermoapi:fluid-" + gas);

            return results == null ? gas : results;
        }

        //Returns whether the block face is solid
        public bool SolidCheck(Block block, BlockFacing face)
        {
            if (block.Attributes?.KeyExists("thermosysSolidSides") == true)
            {
                return block.Attributes["thermosysSolidSides"].IsTrue(face.Code);
                return block.Attributes["thermosysSolidSides"].IsTrue(face.Code);
            }

            return block.SideSolid[face.Index];
        }

        //Gives the local index for a block in its chunk
        int toLocalIndex(BlockPos pos)
        {
            return MapUtil.Index3d(pos.X % api.World.BlockAccessor.ChunkSize, pos.Y % api.World.BlockAccessor.ChunkSize, pos.Z % api.World.BlockAccessor.ChunkSize, api.World.BlockAccessor.ChunkSize, api.World.BlockAccessor.ChunkSize);
        }


        //Internal class used to get the fluid information from the config.
        [JsonObject(MemberSerialization.OptIn)]
        public class GasInfoLite
        {
            [JsonProperty]
            public bool Light;

            [JsonProperty]
            public float VentilateSpeed = 0;

            [JsonProperty]
            public bool Pollutant;

            [JsonProperty]
            public bool Distribute;

            [JsonProperty]
            public float ExplosionAmount = 2;

            [JsonProperty]
            public float SuffocateAmount = 1;

            [JsonProperty]
            public float FlammableAmount = 2;

            [JsonProperty]
            public bool PlantAbsorb;

            [JsonProperty]
            public bool Acidic;

            [JsonProperty]
            public Dictionary<string, float> Effects;

            [JsonProperty]
            public string BurnInto;

            [JsonProperty]
            public float ToxicAt = 0f;

            public float QualityMult
            {
                get
                {
                    if (SuffocateAmount == 0) return 1;

                    return 1 / SuffocateAmount;
                }
            }
        }
    }
}
