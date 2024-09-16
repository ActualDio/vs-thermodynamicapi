using Vintagestory.API.Common;
using Vintagestory.API;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using System.Collections.Generic;
using ThermalDynamics.Thermodynamics;

namespace ThermalDynamics.BlockBehavior
{
    public class BlockBehaviorExplosionGas : BlockBehaviorMatter
    {
        public Dictionary<string, MaterialProperties> produceGas;

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            produceGas = properties["produceGas"].AsObject(new Dictionary<string, MaterialProperties>());
        }

        public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType, ref EnumHandling handling)
        {
            base.OnBlockExploded(world, pos, explosionCenter, blastType, ref handling);
            
            if (!ThermalDynamicsConfig.Loaded.GasesEnabled || produceGas == null || produceGas.Count < 1) return;
            
            world.Api.ModLoader.GetModSystem<ThermalDynamicsSystem>()?.AddToExplosion(explosionCenter, produceGas);
        }


        public BlockBehaviorExplosionGas(Block block) : base(block)
        {
        }
    }
}
