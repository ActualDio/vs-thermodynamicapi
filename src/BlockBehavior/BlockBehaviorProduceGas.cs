﻿using Vintagestory.API.Common;
using Vintagestory.API;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using System.Collections.Generic;
using ThermodynamicApi.ThermoDynamics;

namespace ThermodynamicApi.BlockBehavior
{
    public class BlockBehaviorProduceGas : BlockBehaviorMatter
    {
        public Dictionary<string, MatterProperties> produceGas;

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            produceGas = properties["produceGas"].AsObject(new Dictionary<string, MatterProperties>());
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handling)
        {
            base.OnBlockPlaced(world, blockPos, ref handling);

            if (!ThermodynamicConfig.Loaded.GasesEnabled || world.Side != EnumAppSide.Server || produceGas == null || produceGas.Count < 1) return;

            world.Api.ModLoader.GetModSystem<ThermodynamicSystem>()?.QueueMatterChange(new Dictionary<string, MatterProperties>(produceGas), blockPos);
        }

        public BlockBehaviorProduceGas(Block block) : base(block)
        {
        }
    }
}
