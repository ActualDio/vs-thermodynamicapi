using gasapi.src.SystemControl;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace ThermodynamicApi
{
    public class MaterialsChunk
    {
        public IWorldChunk Chunk;
        public Dictionary<int, Dictionary<string, MaterialStates>> Materials;

        public int X;
        public int Y;
        public int Z;

        bool shouldSave;

        public MaterialsChunk(IWorldChunk newChunk, Dictionary<int, Dictionary<string, MaterialStates>> newMaterials, int x, int y, int z)
        {
            Chunk = newChunk;
            Materials = newMaterials;
            X = x;
            Y = y;
            Z = z;
        }

        public bool Compare(BlockPos pos, int chunksize)
        {
            return pos.X / chunksize == X && pos.Y / chunksize == Y && pos.Z / chunksize == Z;
        }

        public void SaveChunk(IServerNetworkChannel serverChannel)
        {
            if (!shouldSave) return;
            
            byte[] data = SerializerUtil.Serialize(Materials);

            Chunk.SetModdata("gases", data);
            // Todo: Send only to players that have this chunk in their loaded range
            serverChannel.BroadcastPacket(new ChunkThermoData() { chunkX = X, chunkY = Y, chunkZ = Z, Data = data });
        }

        public void TakeGas(ref Dictionary<string, float> taker, int point)
        {
            if (Materials == null || !Materials.ContainsKey(point)) return;
            
            Dictionary<string, float> takeFrom = Materials[point];
            if (takeFrom == null || takeFrom.Count < 1) return;

            foreach (var gas in takeFrom)
            {
                if (taker.ContainsKey(gas.Key))
                {
                    taker[gas.Key] += gas.Value;
                }
                else taker[gas.Key] = gas.Value;
            }

            Materials[point] = null;
            shouldSave = true;
            
        }

        public void SetGas(string gasName, float amount, int point)
        {
            if (Materials == null) return;
            
            Dictionary<string, float> gasAtPoint;
            Materials.TryGetValue(point, out gasAtPoint);
            if (gasAtPoint == null) gasAtPoint = new Dictionary<string, float>();

            if (!gasAtPoint.ContainsKey(gasName)) gasAtPoint.Add(gasName, GameMath.Clamp(amount, 0, 1)); else gasAtPoint[gasName] = GameMath.Clamp(gasAtPoint[gasName] + amount, 0, 1);
            if (Materials.ContainsKey(point)) Materials[point] = gasAtPoint; else Materials.Add(point, gasAtPoint);
            
            shouldSave = true;
            
        }
    }
}
