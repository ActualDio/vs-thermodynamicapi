using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace ThermalDynamics.Thermodynamics

{
    [JsonObject(MemberSerialization.OptIn)]
    public class MatterInfo
    //All Temperatures are in Kelvin, all pressures are in Pascal and all mass is in Grams
    {
        [JsonProperty]
        public float SpecificHeatCapacity = 1;

        [JsonProperty]
        public float MolarMass = 1;

        [JsonProperty]
        public float MeltingPoint = 0; // at 1 atmosphere of pressure

        /// <summary>
        /// Energy released/consumed in a state transition without a temperature change
        /// </summary>
        [JsonProperty]
        public Dictionary<string, float> Enthalpy = new Dictionary<string, float>()
        {
            { "Vaporization", 0 },
            { "Fusion", 0 },
            { "Sublimation", 0 }
        };

        /// <summary>
        /// Value of temperature and pressure above which the material acts like a supercritical fluid
        /// </summary>
        [JsonProperty]
        public Dictionary<string, float> CriticalPoint = new Dictionary<string, float>()
        { 
            { "Temperature", 0 },
            { "Pressure", 0 } 
        };

        /// <summary>
        /// Value of temperature and pressure where all 3 states of matter can exist at the same time
        /// </summary>
        [JsonProperty]
        public Dictionary<string, float> TriplePoint = new Dictionary<string, float>()
        { 
            { "Temperature", 0 },
            { "Pressure", 0 } 
        };

        [JsonProperty]
        public Dictionary<string, float> Conductivity = new Dictionary<string, float>()
        {
            { "Gas", 0 },
            { "Liquid", 0 },
            { "Solid", 0 }
        };

        public float SpecificGasConstant;

        public Dictionary<string, float> PhaseGraph = new Dictionary<string, float>()
        {
            { "VaporizationConst", 0 },
            { "SublimationConst", 0 },
            { "FusionSlope", 0 },
            { "FusionOffset", 0 }

        };

        //[JsonProperty]
        //public Dictionary<string, float> Effects;

        /*public float QualityMult
        {
            get
            {
                if (SuffocateAmount == 0) return 1;

                return 1 / SuffocateAmount;
            }
        }*/
        public MatterInfo()
        {
            SpecificGasConstant = ThermodynamicsMath.IdealGas_Const / MolarMass;
            PhaseGraph = SolvePhaseGraph();
        }
        public Dictionary<string, float> SolvePhaseGraph()
        {
            Dictionary<string, float> graph = new Dictionary<string, float>()
            {
                { "VaporizationConst", 0 },
                { "SublimationConst", 0 },
                { "FusionSlope", 0 },
                { "FusionOffset", 0 }

            };
            Dictionary<string, float> crit_p = CriticalPoint;
            Dictionary<string, float> triple_p = TriplePoint;
            Dictionary<string, float> melt_p = new Dictionary<string, float>()
            { { "pressure", ThermodynamicsMath.OneAtmoInPascal }, { "temperature", MeltingPoint } };

            float pressure = crit_p["pressure"];
            float temp = crit_p["temperature"];
            float enthalpy = Enthalpy["Vaporization"];

            // Right side and left side of the Clausius-Clapeyron relation equation
            double right_side = Math.Log((double)pressure);
            double left_side = (enthalpy / SpecificGasConstant) * (1 / temp);

            graph["VaporizationConst"] = (float)(left_side + right_side);

            pressure = triple_p["pressure"];
            temp = triple_p["temperature"];
            enthalpy = Enthalpy["Sublimation"];

            right_side = Math.Log((double)pressure);
            left_side = (enthalpy / SpecificGasConstant) * (1 / temp);

            graph["SublimationConst"] = (float)(left_side + right_side);

            graph["FusionSlope"] = (melt_p["temperature"] - triple_p["temperature"]) / (melt_p["pressure"] - triple_p["pressure"]);
            graph["FusionOffset"] = melt_p["pressure"] - (melt_p["temperature"] * graph["FusionSlope"]);

            return graph;
        }
    }
}

