using Newtonsoft.Json;
using System.Collections.Generic;
using Vintagestory.API.Datastructures;

namespace ThermodynamicApi.ThermoDynamics

{
    [JsonObject(MemberSerialization.OptIn)]
    public class FluidInfo
    {
        [JsonProperty]
        public float FluidSpecificHeatCapacity = 1;

        [JsonProperty]
        public float FluidMolarMass = 1;

        [JsonProperty]
        public float StateChangeTemp = 0;

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
