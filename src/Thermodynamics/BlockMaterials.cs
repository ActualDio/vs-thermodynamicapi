using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace ThermalDynamics.Thermodynamics
{
    public interface IBlockMaterials
    {
        OrderedDictionary<string, MaterialProperties> Materials { get; }
        public void AddMaterial(MaterialProperties material);
        public void RemoveMaterial();
        static public bool IsEmpty(OrderedDictionary<string, MaterialProperties> list)
        {
            return list.Count == 0;
        }
    }
    public class BlockGases : IBlockMaterials
    {
        public OrderedDictionary<string, MaterialProperties> Gases => IBlockMaterials.Materials;

        public void AddMaterial(MaterialProperties gas)
        {
            if (gas.State != EnumMatterState.Gas) return;
            if (IBlockMaterials.IsEmpty(Gases))
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

        public void RemoveMaterial()
        {
            throw new NotImplementedException();
        }
    }
    
}
