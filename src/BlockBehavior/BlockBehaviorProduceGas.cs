using Vintagestory.API.Common;
using Vintagestory.API;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using System.Collections.Generic;
using ThermodynamicApi.ThermoDynamics;

namespace ThermodynamicApi.BlockBehavior
{
    public class BlockBehaviorProduceGas : BlockBehaviorGas
    {
        public Dictionary<string, MaterialStates> produceGas;

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            produceGas = properties["produceGas"].AsObject(new Dictionary<string, MaterialStates>());
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handling)
        {
            base.OnBlockPlaced(world, blockPos, ref handling);

            if (!ThermodynamicConfig.Loaded.GasesEnabled || world.Side != EnumAppSide.Server || produceGas == null || produceGas.Count < 1) return;

            world.Api.ModLoader.GetModSystem<ThermodynamicSystem>()?.QueueGasExchange(new Dictionary<string, MaterialStates>(produceGas), blockPos);
        }

        public BlockBehaviorProduceGas(Block block) : base(block)
        {
        }
    }
}
