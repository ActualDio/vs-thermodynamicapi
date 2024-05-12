using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThermalDynamics.Thermodynamics
{
    public enum TemperatureUnits
    {
        KELVIN,
        FAHRENHEIT,
        CELCIUS
    }

    public enum PressureUnits
    {
        PASCAL,
        BAR,
        PSI
    }

    public enum DensityUnits
    {
        MOLAR,
        MASS
    }

    public struct Temperature
    {
        public float value;
        public TemperatureUnits unit;

        public Temperature()
        {
            value = 0;
            unit = TemperatureUnits.KELVIN;
        }
        public Temperature(float _value, TemperatureUnits _unit)
        {
            value = _value;
            unit = _unit;
        }
        public static bool operator ==(Temperature a, Temperature b)
        {
            return a.value == b.value && a.unit == b.unit;
        }
        public static bool operator !=(Temperature a, Temperature b)
        {
            return !(a == b);
        }
        public override bool Equals(object obj)
        {
            return this == (Temperature)obj;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public struct Pressure
    {
        public float value;
        public PressureUnits unit;

        public Pressure()
        {
            value = 0;
            unit = PressureUnits.PASCAL;
        }
        public Pressure(float _value, PressureUnits _unit)
        {
            value = _value;
            unit = _unit;
        }
        public static bool operator ==(Pressure a, Pressure b)
        {
            return a.value == b.value && a.unit == b.unit;
        }
        public static bool operator !=(Pressure a, Pressure b)
        {
            return !(a == b);
        }
        public override bool Equals(object obj)
        {
            return this == (Pressure)obj;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public struct Density
    {
        public float value;
        public DensityUnits unit;

        public Density()
        {
            value = 0;
            unit = DensityUnits.MOLAR;
        }
        public Density(float _value, DensityUnits _unit)
        {
            value = _value;
            unit = _unit;
        }
        public static bool operator ==(Density a, Density b)
        {
            return a.value == b.value && a.unit == b.unit;
        }
        public static bool operator !=(Density a, Density b)
        {
            return !(a == b);
        }
        public override bool Equals(object obj)
        {
            return this == (Density)obj;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
