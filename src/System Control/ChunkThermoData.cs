using ProtoBuf;

namespace ThermodynamicApi
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ChunkThermoData
    {
        public byte[] Data;
        public int chunkX, chunkY, chunkZ;
    }
}
