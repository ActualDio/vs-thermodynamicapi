﻿using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using System.Collections.Generic;
using Vintagestory.API.Server;
using ThermalDynamics.Thermodynamics;

namespace ThermalDynamics.BlockEntityBehaviour
{
    public class BlockEntityBehaviorProduceFluid : BlockEntityBehavior
    {
        ThermalDynamicsSystem thermoHandler;
        public Dictionary<string, MaterialProperties> produceFluid;
        int updateTimeInMS;
        double updateTimeInHours;
        double lastTimeProduced;

        BlockPos blockPos
        {
            get { return Blockentity.Pos; }
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            thermoHandler = api.ModLoader.GetModSystem<ThermalDynamicsSystem>();
            produceFluid = properties["produceGas"].AsObject(new Dictionary<string, MaterialProperties>());
            updateTimeInMS = properties["updateMS"].AsInt(10000);
            updateTimeInHours = properties["updateHours"].AsDouble();
            Blockentity.RegisterGameTickListener(ProduceGas, updateTimeInMS);
        }

        public virtual void ProduceGas(float dt)
        {
            if (Blockentity.Api.World.Calendar.TotalHours - lastTimeProduced < updateTimeInHours) return;
            if (Api.Side != EnumAppSide.Server || produceFluid == null || produceFluid.Count < 1) return;

            lastTimeProduced = Blockentity.Api.World.Calendar.TotalHours;

            thermoHandler.QueueMatterChange(new Dictionary<string, MaterialProperties>(produceFluid), blockPos);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetDouble("gassyslastProduced", lastTimeProduced);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            lastTimeProduced = tree.GetDouble("gassyslastProduced");
        }

        public BlockEntityBehaviorProduceFluid(BlockEntity blockentity) : base(blockentity)
        {
        }
    }
}
