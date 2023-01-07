﻿using Vintagestory.API.Common;
using Vintagestory.API;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using System.Collections.Generic;

namespace ThermodynamicApi.BlockBehaviour
{
    public class BlockBehaviorPlaceGas : BlockBehaviorGas
    {
        public Dictionary<string, float> produceGas;

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            produceGas = properties["produceGas"].AsObject(new Dictionary<string, float>());
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handling)
        {
            base.OnBlockPlaced(world, blockPos, ref handling);

            if (!ThermodynamicConfig.Loaded.GasesEnabled || world.Side != EnumAppSide.Server || produceGas == null || produceGas.Count < 1) return;

            world.Api.ModLoader.GetModSystem<ThermodynamicSystem>()?.QueueGasExchange(new Dictionary<string, float>(produceGas), blockPos);
        }

        public BlockBehaviorPlaceGas(Block block) : base(block)
        {
        }
    }
}
