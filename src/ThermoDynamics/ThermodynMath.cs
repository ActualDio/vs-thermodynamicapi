using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThermodynamicApi.ThermoDynamics
{
    /// <summary>
    /// The different state variables for the given material of a block
    /// </summary>
    public struct MaterialStates
    {
        float MolarDensity; // in moles per cubic meter
        float Pressure; // in Pascals
        float Temperature; // in Kelvin
        // Volume for a cube is assumed to be 1 cubic meter
        public MaterialStates(float quantity, float pressure, float temperature)
        {
            MolarDensity= quantity;
            Pressure= pressure;
            Temperature= temperature;
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

        static MaterialStates IdealGasLaw(MaterialStates state)
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
