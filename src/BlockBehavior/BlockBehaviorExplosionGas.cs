﻿using Vintagestory.API.Common;
using Vintagestory.API;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using System.Collections.Generic;

namespace ThermodynamicApi.BlockBehaviour
{
    public class BlockBehaviorExplosionGas : BlockBehavior
    {
        public Dictionary<string, float> produceGas;

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            produceGas = properties["produceGas"].AsObject(new Dictionary<string, float>());
        }

        public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType, ref EnumHandling handling)
        {
            base.OnBlockExploded(world, pos, explosionCenter, blastType, ref handling);
            
            if (!ThermodynamicConfig.Loaded.GasesEnabled || produceGas == null || produceGas.Count < 1) return;
            
            world.Api.ModLoader.GetModSystem<ThermodynamicSystem>()?.AddToExplosion(explosionCenter, produceGas);
        }


        public BlockBehaviorExplosionGas(Block block) : base(block)
        {
        }
    }
}
