using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThermodynamicApi.ThermoDynamics 
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SolidInfo
    {
        [JsonProperty]
        public float FluidSpecificHeatCapacity = 1;

        [JsonProperty]
        public float FluidMolarMass = 1;
    }
}
