using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace ThermalDynamics.Thermodynamics
{
    /// <summary>
    /// The different state variables for the given materials of a block
    /// </summary>
    public struct MaterialProperties
    {
        // Volume for a cube is assumed to be 1 cubic meter
        public string Name { get; }
        public MatterInfo Info { get; }
        public float Density { get; set; } // in moles per cubic meter
        public float Pressure { get; set; } // in Pascals
        public float Temperature { get; set; } // in Kelvin
        public EnumMatterState State { get; } //Solid, Liquid and Gas
        public MaterialProperties(
            float density,
            float press,
            float temp,
            string name,
            EnumMatterState state = EnumMatterState.Plasma)
        {
            Name = name;
            Density = density;
            Pressure = press;
            Temperature = temp;
            State = state;
            Info = ThermalDynamicsSystem.MatterDictionary[name];
        }


        public static bool operator ==(MaterialProperties? a, MaterialProperties? b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            MaterialProperties _a = a.Value; MaterialProperties _b = b.Value;

            return _a.Density == _b.Density && _a.Temperature == _b.Temperature && _a.Pressure == _b.Pressure && _a.State == _b.State && _a.Name == _b.Name;
        }
        public static bool operator !=(MaterialProperties? a, MaterialProperties? b)
        {
            return !(a == b);
        }
        public static MaterialProperties operator +(MaterialProperties? a, MaterialProperties? b)
        {
            if (a == default) return b;
            if (b == default) return a;
            if (a.State != b.State) return default;
            a.Pressure = +b.Pressure; a.Temperature = +b.Temperature; a.MolarDensity = +b.MolarDensity;
            return a;
        }
        public override bool Equals(object obj)
        {
            return this == (MaterialProperties)obj;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}

