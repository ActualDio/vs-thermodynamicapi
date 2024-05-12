﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ThermalDynamics.Thermodynamics;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

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

    public class BlockMaterials
    {
        private OrderedDictionary<string, MaterialProperties> Gases;
        private Dictionary<string, MaterialProperties> Liquids;
        private OrderedDictionary<string, MaterialProperties> Solids;

        public bool HasGas()
        {
            return Gases.Count > 0;
        }
        public bool HasLiquid()
        {
            return Liquids.Count > 0;
        }
        public bool HasSolid()
        {
            return Solids.Count > 0;
        }
        public void AddGas(MaterialProperties gas)
        {
            if (gas.State != EnumMatterState.Gas) return;
            if (!HasGas())
            {
                Gases.Add(gas.Name, gas);
                return;
            }
            else
            {
                int index = -1;
                foreach (string value in Gases.Keys)
                {
                    if (Gases[value].Info.CriticalPoint["temperature"] < gas.Info.CriticalPoint["temperature"])
                    {
                        index = Gases.IndexOfKey(value);
                    }
                    else continue;
                }
                if (index == -1)
                {
                    Gases.Add(gas.Name, gas);
                    return;
                }
                else
                {
                    Gases.Insert(index, gas.Name, gas);
                    return;
                }
            }

        }
        public void RemoveGas()
        {

        }
        public void AddSolid()
        {

        }
        public void RemoveSolid()
        {

        }

        public void DetectPhaseChange()
        {
            if (HasGas())
            {
                foreach(MaterialProperties gas in Gases.Values)
                {

                }
            }
        }
    }
    static class ThermodynamicsMath
    {
        public const double Boltzman_Const = 1.380649E-23; // J * K^-1
        public const float StephBoltz_Const = 5.670374419E-8f; // W * m^-2 * K^-4
        public const float IdealGas_Const = 8.31446261815324f; // J * K^-1 * mol^-1
        public const float Gravity_Const = 9.80665f; // m * s^-2
        public const float OneAtmoInPascal = 101325.0f;

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

        /// <summary>
        /// Converts different temeprature units into oneanother
        /// </summary>
        static Temperature TempConversion(
            Temperature temp,
            TemperatureUnits desiredUnit)
        {
            if (temp.unit == desiredUnit)
            {
                return temp;
            }
            else
            {
                switch (desiredUnit)
                {
                    case TemperatureUnits.KELVIN:
                        switch (temp.unit)
                        {
                            case TemperatureUnits.CELCIUS:
                                temp.value -= 273.15f;
                                temp.unit = TemperatureUnits.KELVIN;
                                return temp;
                            case TemperatureUnits.FAHRENHEIT:
                                temp.value = 1.8f * (temp.value - 273.15f) + 32f;
                                temp.unit = TemperatureUnits.KELVIN;
                                return temp;
                            default:
                                //Not a valid conversion
                                return temp;
                        }
                    case TemperatureUnits.CELCIUS:
                        switch (temp.unit)
                        {
                            case TemperatureUnits.KELVIN:
                                temp.value += 273.15f;
                                temp.unit = TemperatureUnits.CELCIUS;
                                return temp;
                            case TemperatureUnits.FAHRENHEIT:
                                temp.value = 1.8f * temp.value + 32f;
                                temp.unit = TemperatureUnits.CELCIUS;
                                return temp;
                            default:
                                //Not a valid conversion
                                return temp;
                        }
                    case TemperatureUnits.FAHRENHEIT:
                        switch (temp.unit)
                        {
                            case TemperatureUnits.KELVIN:
                                temp.value = (temp.value - 32f) / 1.8f + 273.15f;
                                temp.unit = TemperatureUnits.FAHRENHEIT;
                                return temp;
                            case TemperatureUnits.CELCIUS:
                                temp.value = (temp.value - 32f) / 1.8f;
                                temp.unit = TemperatureUnits.FAHRENHEIT;
                                return temp;
                            default:
                                //Not a valid conversion
                                return temp;
                        }
                    default:
                        return temp;
                }
            }
        }
        /// <summary>
        /// Converts different pressure units into oneanother
        /// </summary>
        static Pressure PressureConversion(
            Pressure press,
            PressureUnits desiredUnit)
        {
            if (press.unit == desiredUnit)
            {
                return press;
            }
            else
            {
                switch (desiredUnit)
                {
                    case PressureUnits.PASCAL:
                        switch (press.unit)
                        {
                            case PressureUnits.PSI:
                                press.value *= 6894.7572931783f;
                                press.unit = PressureUnits.PASCAL;
                                return press;
                            case PressureUnits.BAR:
                                press.value *= 100000;
                                press.unit = PressureUnits.PASCAL;
                                return press;
                            default:
                                //Not a valid conversion
                                return press;
                        }
                    case PressureUnits.BAR:
                        switch (press.unit)
                        {
                            case PressureUnits.PASCAL:
                                press.value *= 0.00001f;
                                press.unit = PressureUnits.BAR;
                                return press;
                            case PressureUnits.PSI:
                                press.value *= 0.0689475729f;
                                press.unit = PressureUnits.BAR;
                                return press;
                            default:
                                //Not a valid conversion
                                return press;
                        }
                    case PressureUnits.PSI:
                        switch (press.unit)
                        {
                            case PressureUnits.PASCAL:
                                press.value *= 0.00014504f;
                                press.unit = PressureUnits.PSI;
                                return press;
                            case PressureUnits.BAR:
                                press.value *= 14.503773773f;
                                press.unit = PressureUnits.PSI;
                                return press;
                            default:
                                //Not a valid conversion
                                return press;
                        }
                    default:
                        return press;
                }
            }
        }

        static float IdealGasStateEquation(
            float density = float.NaN,
            float temperature = float.NaN,
            float pressure = float.NaN)
        {
            int statement = Convert.ToInt32(density != float.NaN)
                + (Convert.ToInt32(temperature != float.NaN) * 2)
                + (Convert.ToInt32(pressure != float.NaN) * 4);

            float value = float.NaN;
            switch (statement)
            {
                case 3: // Solve for pressure
                    value = IdealGas_Const * density * temperature;
                    break;

                case 5: // Solve for temperature
                    value = pressure / (IdealGas_Const * density);
                    break;

                case 6: // Solve for density
                    value = pressure / (IdealGas_Const * temperature);
                    break;

                case 7: // All values were given, nothing to find
                default:
                    //Return a NaN value
                    break;
            }
            return value;
        }

    }
}
