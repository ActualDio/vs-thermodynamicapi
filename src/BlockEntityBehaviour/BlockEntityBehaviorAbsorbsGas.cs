using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using System.Collections.Generic;
using Vintagestory.API.Server;
using ThermodynamicApi.ThermoDynamics;

namespace ThermodynamicApi.BlockEntityBehaviour
{
    public class BlockEntityBehaviorAbsorbsGas : BlockEntityBehavior
    {
        ThermodynamicSystem fluidHandler;
        public Dictionary<string, MaterialStates> gasScrub;
        public MaterialStates scrubAmount;
        BlockPos blockPos
        {
            get { return Blockentity.Pos; }
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            fluidHandler = api.ModLoader.GetModSystem<ThermodynamicSystem>();
            Blockentity.RegisterGameTickListener(RemoveFluid, 5000);
            scrubAmount = properties["scrubAmount"].AsObject<MaterialStates>();
            gasScrub = new Dictionary<string, MaterialStates>();
            gasScrub.Add("THISISAPLANT", scrubAmount);
        }

        public void RemoveFluid(float dt)
        {
            if (Api.Side != EnumAppSide.Server) return;

            BlockEntity bpc;
            if (bpc == null || Api.World.BlockAccessor.GetLightLevel(Blockentity.Pos, EnumLightLevelType.TimeOfDaySunLight) < 13) return;

            if (bpc.Inventory[0].Empty || bpc.Inventory[0].Itemstack.Block?.BlockMaterial != EnumBlockMaterial.Plant || bpc.Inventory[0].Itemstack.Collectible.Code.Path.StartsWith("mushroom")) return;

            fluidHandler.QueueGasExchange(removeInstructions, blockPos);
        }

        public BlockEntityBehaviorAbsorbsGas(BlockEntity blockentity) : base(blockentity)
        {
        }
    }
}
