using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace ThermodynamicApi.ThermoDynamics
{
    public struct Nullable<MatterProperties> {}
    /// <summary>
    /// The different state variables for the given material of a block
    /// </summary>
    public struct MatterProperties
    {
        // Volume for a cube is assumed to be 1 cubic meter
        public float? MolarDensity { get; set; } // in moles per cubic meter
        public float? Pressure { get; set; } // in Pascals
        public float? Temperature { get; set; } // in Kelvin
        public EnumMatterState? State { get; set; } //Solid, Liquid and Gas
        public MatterProperties(float? density = null, float? press = null, float? temp = null, EnumMatterState? state = null)
        {
            MolarDensity = density;
            Pressure = press;
            Temperature = temp;
            State = state;
        }
        public static bool operator ==(MatterProperties a, MatterProperties b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            return (a.MolarDensity == b.MolarDensity && a.Temperature == b.Temperature && a.Pressure == b.Pressure && a.State == b.State);
        }
        public static bool operator !=(MatterProperties a, MatterProperties b)
        {
            return !(a == b);
        }
        public static MatterProperties operator +(MatterProperties a, MatterProperties b)
        {
            if (a == default) return b;
            if (b == default) return a;
            if (a.State != b.State) return default;
            a.Pressure =+ b.Pressure; a.Temperature =+ b.Temperature; a.MolarDensity =+ b.MolarDensity;
            return a;
        }
        public override bool Equals(object obj)
        {
            return this == (MatterProperties)obj;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public enum TemperatureUnits
    {
        KELVIN,
        FAHRENHEIT,
        CELCIUS
    }
    static class ThermodynMath
    {
        public const double Boltzman_Const = 1.380649E-23; // J * K^-1
        public const float StephBoltz_Const = 5.670374419E-8f; // W * m^-2 * K^-4
        public const float IdealGas_Const = 8.31446261815324f; // J * K^-1 * mol^-1
        public const float Gravity_Const = 9.80665f; // m * s^-2

        /// <summary>
        /// Calculates an exponential of e with adjustable levels of precision with the taylor series
        /// </summary>
        /// <param name="n">degree of precision</param>
        /// <param name="x">exponent to use</param>
        static float Exponential(int n, float x)
        {
            // initialize sum of taylor series
            float sum = 1;

            for (int i = n - 1; i > 0; --i)
                sum = 1 + x * sum / i;

            return sum;
        }

        static MatterProperties IdealGasLaw(MatterProperties state)
        {
            return state;
        }
        /// <summary>
        /// Converts different temeprature units into oneanother
        /// </summary>
        static float TempConversion(float temp, TemperatureUnits tempUnit, TemperatureUnits desiredUnit)
        {
            if(tempUnit == desiredUnit)
            {
                return temp;
            }
            else if(tempUnit == TemperatureUnits.KELVIN){ 

                if(desiredUnit == TemperatureUnits.CELCIUS) 
                {
                    return temp - 273.15f;
                }
                else if(desiredUnit == TemperatureUnits.FAHRENHEIT)
                {
                    return 1.8f * (temp - 273.15f) + 32f;
                }
                else
                {
                    //Not a valid conversion
                    return temp;
                }
            }
            else if (tempUnit == TemperatureUnits.CELCIUS)
            {
                if (desiredUnit == TemperatureUnits.KELVIN)
                {
                    return temp + 273.15f;
                }
                else if (desiredUnit == TemperatureUnits.FAHRENHEIT)
                {
                    return 1.8f * temp + 32f;
                }
                else
                {
                    //Not a valid conversion
                    return temp;
                }
            }
            else if(tempUnit == TemperatureUnits.FAHRENHEIT)
            {
                if (desiredUnit == TemperatureUnits.KELVIN)
                {
                    return (temp - 32f) / 1.8f + 273.15f;
                }
                else if (desiredUnit == TemperatureUnits.CELCIUS)
                {
                    return (temp - 32f) / 1.8f;
                }
                else
                {
                    //Not a valid conversion
                    return temp;
                }
            }
            else
            {
                //Not a valid conversion
                return temp;
            }  
        }
    }
}
