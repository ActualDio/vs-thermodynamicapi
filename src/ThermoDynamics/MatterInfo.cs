using Newtonsoft.Json;
using System.Collections.Generic;
using Vintagestory.API.Datastructures;

namespace ThermodynamicApi.ThermoDynamics

{
    [JsonObject(MemberSerialization.OptIn)]
    public class MatterInfo
    {
        [JsonProperty]
        public float SpecificHeatCapacity = 1;

        [JsonProperty]
        public float FluidMolarMass = 1;

        [JsonProperty]
        public float StateChangeTemp = 0;

        [JsonProperty]
        public float Conductivity = 0;

        [JsonProperty]
        public Dictionary<string, float> Effects;

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
