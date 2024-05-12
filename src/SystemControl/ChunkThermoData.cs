using ProtoBuf;

namespace ThermalDynamics.SystemControl
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ChunkThermoData
    {
        public byte[] Data;
        public int chunkX, chunkY, chunkZ;
    }
}
