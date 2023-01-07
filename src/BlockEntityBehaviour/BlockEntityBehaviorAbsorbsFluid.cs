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
    public class BlockEntityBehaviorAbsorbsFluid : BlockEntityBehavior
    {
        ThermodynamicSystem fluidHandler;
        public Dictionary<string, MaterialStates> fluidScrub;
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
            fluidScrub = new Dictionary<string, MaterialStates>();
            fluidScrub.Add("THISISAPLANT", scrubAmount);
        }

        public void RemoveFluid(float dt)
        {
            if (Api.Side != EnumAppSide.Server) return;

            BlockEntity bpc;
            if (bpc == null || Api.World.BlockAccessor.GetLightLevel(Blockentity.Pos, EnumLightLevelType.TimeOfDaySunLight) < 13) return;

            if (bpc.Inventory[0].Empty || bpc.Inventory[0].Itemstack.Block?.BlockMaterial != EnumBlockMaterial.Plant || bpc.Inventory[0].Itemstack.Collectible.Code.Path.StartsWith("mushroom")) return;

            fluidHandler.QueueGasExchange(new Dictionary<string, MaterialStates>(fluidScrub), blockPos);
        }

        public BlockEntityBehaviorAbsorbsFluid(BlockEntity blockentity) : base(blockentity)
        {
        }
    }
}
