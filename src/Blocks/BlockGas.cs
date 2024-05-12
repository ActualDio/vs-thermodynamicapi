﻿using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;
using System.Collections.Generic;
using ThermalDynamics.Thermodynamics;

namespace ThermalDynamics.Blocks
{
    public class BlockGas: Block
    {
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);

            if (world.Side != EnumAppSide.Server) return;
            
            Dictionary<string, MaterialProperties> tester = new Dictionary<string, MaterialProperties>();
            tester.Add(FirstCodePart(1), 1);

            world.Api.ModLoader.GetModSystem<ThermalDynamicsSystem>().QueueMatterChange(tester, blockPos);
            world.BlockAccessor.SetBlock(0, blockPos);
        }

        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, LCGRandom worldgenRandom)
        {
            Dictionary<string, float> tester = new Dictionary<string, float>();
            tester.Add(FirstCodePart(1), 1);

            api.ModLoader.GetModSystem<ThermalDynamicsSystem>().SetGases(pos, tester);
            blockAccessor.SetBlock(0, pos);

            return true;
        }
    }
}
