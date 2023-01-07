using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace ThermodynamicApi.BlockBehaviour
{
    public class BlockBehaviorGas : BlockBehavior
    {
        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            if (!ThermodynamicConfig.Loaded.GasesDebugEnabled) return null;
            StringBuilder dsc = new StringBuilder();
            dsc.AppendLine("Materials at Position:");
            ThermodynamicSystem gasworks = world.Api.ModLoader.GetModSystem<ThermodynamicSystem>();
            if (gasworks == null) return null;

            Dictionary<string, float> gasesHere = gasworks.GetGases(pos);

            if (gasesHere == null || gasesHere.Count < 1) return null;

            foreach (var gas in gasesHere)
            {
                string name = Lang.GetIfExists("gasapi:gas-" + gas.Key) ?? gas.Key;
                dsc.AppendLine(name + " : " + (gas.Value * 100).ToString("0.0") + "%");
            }

            return dsc.ToString();
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
        {
            base.OnBlockRemoved(world, pos, ref handling);

            if (world.Side != EnumAppSide.Server || block.GetBehavior<BlockBehaviorMineGas>() != null || world.Rand.NextDouble() > ThermodynamicConfig.Loaded.SpreadGasOnBreakChance) return;

            ThermodynamicSystem gasHandler = world.Api.ModLoader.GetModSystem<ThermodynamicSystem>();

            gasHandler.QueueGasExchange(null, pos);
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handling)
        {
            base.OnBlockPlaced(world, blockPos, ref handling);

            if (world.Side != EnumAppSide.Server || world.Rand.NextDouble() > ThermodynamicConfig.Loaded.SpreadGasOnPlaceChance) return;

            ThermodynamicSystem gasHandler = world.Api.ModLoader.GetModSystem<ThermodynamicSystem>();

            gasHandler.QueueGasExchange(null, blockPos);
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
        {
            base.OnNeighbourBlockChange(world, pos, neibpos, ref handling);

            if (world.Side == EnumAppSide.Server && world.Rand.NextDouble() <= ThermodynamicConfig.Loaded.UpdateSpreadGasChance)
            {
                world.Api.ModLoader.GetModSystem<ThermodynamicSystem>()?.QueueGasExchange(null, pos);
            }
        }

        public BlockBehaviorGas(Block block) : base(block)
        {
        }
    }
}
