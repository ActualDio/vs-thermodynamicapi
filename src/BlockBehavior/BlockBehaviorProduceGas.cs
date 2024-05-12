using Vintagestory.API.Common;
using Vintagestory.API;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using System.Collections.Generic;
using ThermalDynamics.Thermodynamics;

namespace ThermalDynamics.BlockBehavior
{
    public class BlockBehaviorProduceGas : BlockBehaviorMatter
    {
        public Dictionary<string, MaterialProperties> produceGas;

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            produceGas = properties["produceGas"].AsObject(new Dictionary<string, MaterialProperties>());
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handling)
        {
            base.OnBlockPlaced(world, blockPos, ref handling);

            if (!ThermalDynamicsConfig.Loaded.GasesEnabled || world.Side != EnumAppSide.Server || produceGas == null || produceGas.Count < 1) return;

            world.Api.ModLoader.GetModSystem<ThermalDynamicsSystem>()?.QueueMatterChange(new Dictionary<string, MaterialProperties>(produceGas), blockPos);
        }

        public BlockBehaviorProduceGas(Block block) : base(block)
        {
        }
    }
}
