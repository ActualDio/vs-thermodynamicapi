using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Energy released/consumed in a Liquid to Gas state change without a temperature change
        /// </summary>
        [JsonProperty]
        public float EnthalpyVaporization = 0;

        /// <summary>
        /// Energy released/consumed in a Solid to Gas state change without a temperature change
        /// </summary>
        [JsonProperty]
        public float EnthalpySublimation = 0;

        /// <summary>
        /// Energy released/consumed in a Solid to Liquid state change without a temperature change
        /// </summary>
        [JsonProperty]
        public float EnthalpyFusion = 0;

        /// <summary>
        /// Value of temperature and pressure above which the material acts like a supercritical fluid
        /// </summary>
        [JsonProperty]
        public Dictionary<string, float> CriticalPoint = new Dictionary<string, float>(){ { "temperature", 0 }, { "pressure", 0 } };

        /// <summary>
        /// Value of temperature and pressure where all 3 states of matter can exist at the same time
        /// </summary>
        [JsonProperty]
        public Dictionary<string, float> TriplePoint = new Dictionary<string, float>(){ { "temperature", 0 }, { "pressure", 0 } };

        [JsonProperty]
        public float MeltingPoint = 0; // at 1 atmosphere of pressure

        [JsonProperty]
        public float Conductivity = 0;

        public float SpecificGasConstant;
        public float VaporizationConst;
        public float SublimationConst;
        public float[] SoldifyBoudaryLine;

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
            VaporizationConst = SolveVaporizationConst();
            SublimationConst = SolveSublimationConst();
            SoldifyBoudaryLine = SolveSolidifyBoudary();
        }
        public float SolveVaporizationConst()
        {
            Dictionary<string, float> crit_p = CriticalPoint;
            float pressure = crit_p["pressure"];
            float temp = crit_p["temperature"];
            float enthalpy = EnthalpyVaporization;

            // Right side and left side of the Clausius-Clapeyron relation equation
            double right_side = Math.Log((double)pressure);
            double left_side = (enthalpy / SpecificGasConstant) * (1 / temp);

            return (float)(left_side + right_side);
        }

        public float SolveSublimationConst()
        {
            Dictionary<string, float> triple_p = TriplePoint;
            float pressure = triple_p["pressure"];
            float temp = triple_p["temperature"];
            float enthalpy = EnthalpySublimation;

            // Right side and left side of the Clausius-Clapeyron relation equation
            double right_side = Math.Log((double)pressure);
            double left_side = (enthalpy / SpecificGasConstant) * (1 / temp);

            return (float)(left_side + right_side);
        }

        public float[] SolveSolidifyBoudary()
        {
            Dictionary<string, float> triple_p = TriplePoint;
            Dictionary<string, float> melt_p = new Dictionary<string, float>()
            { { "pressure", ThermodynamicsMath.OneAtmoInPascal }, { "temperature", MeltingPoint } };

            float slope = (melt_p["temperature"] - triple_p["temperature"]) / (melt_p["pressure"] - triple_p["pressure"]);
            float offset = melt_p["pressure"] - (melt_p["temperature"] * slope);
            float[] boundary = { slope, offset };
            return boundary;
        }
    }
}
}
