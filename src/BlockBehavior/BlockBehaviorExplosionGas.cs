using Vintagestory.API.Common;
using Vintagestory.API;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using System.Collections.Generic;
using ThermodynamicApi.ThermoDynamics;

namespace ThermodynamicApi.BlockBehavior
{
    public class BlockBehaviorExplosionGas : BlockBehaviorMatter
    {
        public Dictionary<string, MatterProperties> produceGas;

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            produceGas = properties["produceGas"].AsObject(new Dictionary<string, MatterProperties>());
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
