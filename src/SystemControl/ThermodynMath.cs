using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gasapi.src.SystemControl
{
    //The different state variables for a given material inside a block
    public struct MaterialStates
    {
        float MolarDensity; // in moles per cubic meter
        float Pressure; // in Pascals
        float Temperature; // in Kelvin
        // Volume for a cube is assumed to be 1 cubic meter
        public MaterialStates(float quantity, float pressure, float temperature)
        {
            Material
            MolarDensity= quantity;
            Pressure= pressure;
            Temperature= temperature;
        }
    }
    static class ThermodynMath
    {
        public const double Boltzman_Const = 1.380649E-23; // J * K^-1
        public const float StephBoltz_Const = 5.670374419E-8f; // W * m^-2 * K^-4
        public const float IdealGas_Const = 8.31446261815324f; // J * K^-1 * mol^-1
        public const float Gravity_Const = 9.80665f; // m * s^-2

        static float Exponential(int n, float x)
        {
            // initialize sum of series
            float sum = 1;

            for (int i = n - 1; i > 0; --i)
                sum = 1 + x * sum / i;

            return sum;
        }

        static MaterialStates IdealGasLaw(MaterialStates state)
        {

        }
    }
}
