using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ThermalDynamics.ApiHelper;
using ThermalDynamics.BlockBehavior;
using ThermalDynamics.BlockEntityBehaviour;
using ThermalDynamics.Blocks;
using ThermalDynamics.EntityBehavior;
using ThermalDynamics.SystemControl;
using ThermalDynamics.Thermodynamics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ThermalDynamics
{
    public class ThermalDynamicsSystem : ModSystem
    {
        private ICoreServerAPI sapi;
        private Dictionary<BlockPos, Dictionary<string, MaterialProperties>> matterDynamicsQueue = new Dictionary<BlockPos, Dictionary<string, MaterialProperties>>();
        private Dictionary<BlockPos, Dictionary<string, MaterialProperties>> heatDynamicsQueue = new Dictionary<BlockPos, Dictionary<string, MaterialProperties>>();
        private Dictionary<BlockPos, Dictionary<string, MaterialProperties>> pressureDynamicsQueue = new Dictionary<BlockPos, Dictionary<string, MaterialProperties>>();
        public static object matterDynamicsLock = new object();
        public static object heatDynamicsLock = new object();
        public static object pressureDynamicsLock = new object();
        //private Dictionary<BlockPos, Dictionary<string, MatterProperties>> ExplosionQueue = new Dictionary<BlockPos, Dictionary<string, MatterProperties>>();
        //private Dictionary<Vec2i, Dictionary<string, double>> PollutionPerChunk = new Dictionary<Vec2i, Dictionary<string, double>>();
        EntityPartitioning entityUtil;

        ICoreAPI api;

        IClientNetworkChannel clientChannel;
        static IServerNetworkChannel serverChannel;

        public static Dictionary<string, MatterInfo> MatterDictionary;

        private ThermodynamicThread thermoThread;
        private Harmony harmony;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return true;
        }

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);

            try
            {
                ThermalDynamicsConfig FromDisk;
                if ((FromDisk = api.LoadModConfig<ThermalDynamicsConfig>("ThermodynamicConfig.json")) == null)
                {
                    api.StoreModConfig<ThermalDynamicsConfig>(ThermalDynamicsConfig.Loaded, "ThermodynamicConfig.json");
                }
                else ThermalDynamicsConfig.Loaded = FromDisk;
            }
            catch
            {
                api.StoreModConfig<ThermalDynamicsConfig>(ThermalDynamicsConfig.Loaded, "ThermodynamicConfig.json");
            }

            api.World.Config.SetBool("GAgasesEnabled", ThermalDynamicsConfig.Loaded.GasesEnabled);
        }

        public override void Start(ICoreAPI api)
        {
            this.api = api;

            api.RegisterBlockBehaviorClass("Matter", typeof(BlockBehaviorMatter));
            //api.RegisterBlockBehaviorClass("SparkGas", typeof(BlockBehaviorSparkGas));
            //api.RegisterBlockBehaviorClass("MineGas", typeof(BlockBehaviorMineGas));
            //api.RegisterBlockBehaviorClass("ExplosionGas", typeof(BlockBehaviorExplosionGas));

            api.RegisterBlockClass("BlockGas", typeof(BlockGas));
            api.RegisterBlockClass("BlockLiquid", typeof(BlockLiquid));
            api.RegisterBlockClass("BlockMixture", typeof(BlockMixture));
            api.RegisterBlockClass("BlockSolid", typeof(BlockGas));

            api.RegisterEntityBehaviorClass("gasinteract", typeof(EntityBehaviorGas));
            api.RegisterEntityBehaviorClass("air", typeof(EntityBehaviorAir));

            api.RegisterBlockEntityBehaviorClass("ProduceFluid", typeof(BlockEntityBehaviorProduceFluid));
            api.RegisterBlockEntityBehaviorClass("ConsumeFluid", typeof(BlockEntityBehaviorConsumeFluid));

            IAsset matter = api.Assets.Get("thermoapi:config/materials.json");
            MatterDictionary = matter.ToObject<Dictionary<string, MatterInfo>>();
            if (MatterDictionary == null) MatterDictionary = new Dictionary<string, MatterInfo>();
            entityUtil = api.ModLoader.GetModSystem<EntityPartitioning>();

            harmony = new Harmony("com.actualdio.thermoapi.thermodynamicapi");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override void Dispose()
        {
            harmony.UnpatchAll(harmony.Id);
            base.Dispose();
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            if (ThermalDynamicsConfig.Loaded.PlayerBreathingEnabled)
            {
                HudElementAirBar airBar = new HudElementAirBar(api);
                airBar.TryOpen();
            }

            clientChannel = api.Network
                .RegisterChannel("thermodinamics")
                .RegisterMessageType(typeof(ChunkThermoData))
                .SetMessageHandler<ChunkThermoData>(OnChunkData)
            ;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            this.sapi = api;

            api.Event.ServerRunPhase(EnumServerRunPhase.ModsAndConfigReady, AddMatterBehavior);
            api.Event.SaveGameLoaded += OnSaveGameLoaded;
            api.Event.GameWorldSave += OnGameGettingSaved;
            api.Event.RegisterEventBusListener(OnMatterChangeBus, 10000, "matterChange");

            serverChannel = api.Network
                .RegisterChannel("thermodinamics")
                .RegisterMessageType(typeof(ChunkThermoData))
            ;

            /*api.RegisterCommand("thermosys", "Manipulates the thermodynamics system", "Thermodynamics System Check", (IServerPlayer player, int groupId, CmdArgs args) =>
            {
                string order = args.PopWord();

                switch (order)
                {
                    case "queue":
                        player.SendMessage(GlobalConstants.GeneralChatGroup, "Current Queue Count: " + spreadGasQueue.Count, EnumChatType.CommandSuccess);
                        break;
                    case "reset":
                        lock (spreadFluidLock)
                        {
                            Dictionary<BlockPos, Dictionary<string, float>> backup = new Dictionary<BlockPos, Dictionary<string, float>>();

                            foreach (var pos in spreadGasQueue)
                            {
                                if (!backup.ContainsKey(pos.Key)) backup.Add(pos.Key, pos.Value);
                            }

                            spreadGasQueue = backup;
                        }
                        break;
                    case "find":
                        lock (spreadFluidLock)
                        {
                            int count = 1;
                            foreach (var pos in spreadGasQueue)
                            {
                                player.SendMessage(GlobalConstants.GeneralChatGroup, String.Format("Position {0} in queue: X: {1}, Y: {2}, Z: {3}", count, pos.Key.X, pos.Key.Y, pos.Key.Z), EnumChatType.CommandSuccess);
                                count++;
                            }
                        }
                        break;
                    case "stop":
                        fluidSpreader.Stopping = true;
                        break;
                    case "start":
                        fluidSpreader.Stopping = false;
                        fluidSpreader.Start(spreadGasQueue);
                        break;
                    case "cleanstart":
                        lock (spreadFluidLock)
                        {
                            Dictionary<BlockPos, Dictionary<string, float>> backup = new Dictionary<BlockPos, Dictionary<string, float>>();

                            foreach (var pos in spreadGasQueue)
                            {
                                if (!backup.ContainsKey(pos.Key)) backup.Add(pos.Key, pos.Value);
                            }

                            spreadGasQueue = backup;
                        }
                        fluidSpreader.Stopping = false;
                        fluidSpreader.Start(spreadGasQueue);
                        break;
                    case "toggle":
                        fluidSpreader.Paused = !fluidSpreader.Paused;
                        break;
                    case "pollution":
                        Vec2i cpos = new Vec2i(player.Entity.ServerPos.AsBlockPos.X / 32, player.Entity.ServerPos.AsBlockPos.Z / 32);
                        StringBuilder info = new StringBuilder();

                        info.AppendLine(String.Format("Pollution in Chunk Column at positon X: {0}, Z: {1}", cpos.X, cpos.Y));
                        if (PollutionPerChunk != null && PollutionPerChunk.ContainsKey(cpos))
                            foreach (var matter in PollutionPerChunk[cpos]) info.AppendLine(Lang.Get("gasapi:matter-" + matter.Key) + ": " + matter.Value.ToString("#.#"));

                        player.SendMessage(GlobalConstants.GeneralChatGroup, info.ToString(), EnumChatType.CommandSuccess);
                        break;
                }

            }, Privilege.time);*/

            api.World.RegisterGameTickListener((dt) =>
            {

                if (thermoThread?.Stopping == true)
                {
                    lock (matterDynamicsLock)
                    {
                        Dictionary<BlockPos, Dictionary<string, MaterialProperties>> backup = new Dictionary<BlockPos, Dictionary<string, MaterialProperties>>();

                        foreach (var pos in matterDynamicsQueue)
                        {
                            if (!backup.ContainsKey(pos.Key)) backup.Add(pos.Key, pos.Value);
                        }

                        matterDynamicsQueue = backup;
                    }
                    thermoThread.Stopping = false;
                    thermoThread.Start(matterDynamicsQueue);
                }
            }, 30);
        }

        private void OnMatterChangeBus(string eventName, ref EnumHandling handling, IAttribute data)
        {
            if (eventName != "matterChange" || data == null) return;

            Dictionary<string, MaterialProperties> fluids = ThermodynamicHelper.DeserializeThermoTreeData(data, out BlockPos spreadPos);

            if (spreadPos == null) return;

            QueueMatterChange(fluids, spreadPos);
        }

        private void OnSaveGameLoaded()
        {
            matterDynamicsQueue = DeserializeQueue("matterDynamicsQueue");
            heatDynamicsQueue = DeserializeQueue("heatDynamicsQueue");
            pressureDynamicsQueue = DeserializeQueue("pressureDynamicsQueue");
            //PollutionPerChunk = deserializePollution("pollutionChunks");
            thermoThread = new ThermodynamicThread(sapi, this);
            thermoThread.Start(matterDynamicsQueue);
        }

        private void OnGameGettingSaved()
        {
            lock (matterDynamicsLock)
            {
                sapi.WorldManager.SaveGame.StoreData("matterDynamicsQueue", SerializerUtil.Serialize(matterDynamicsQueue));
            }
            lock (heatDynamicsLock)
            {
                sapi.WorldManager.SaveGame.StoreData("heatDynamicsQueue", SerializerUtil.Serialize(heatDynamicsQueue));
            }
            lock (pressureDynamicsLock)
            {
                sapi.WorldManager.SaveGame.StoreData("pressureDynamicsQueue", SerializerUtil.Serialize(pressureDynamicsQueue));
            }
        }

        private Dictionary<BlockPos, Dictionary<string, MaterialProperties>> DeserializeQueue(string name)
        {
            try
            {
                byte[] data = sapi.WorldManager.SaveGame.GetData(name);
                if (data != null)
                {
                    return SerializerUtil.Deserialize<Dictionary<BlockPos, Dictionary<string, MaterialProperties>>>(data);
                }
            }
            catch (Exception e)
            {
                sapi.World.Logger.Error("Failed loading Queue.{0}. Resetting. Exception: {1}", name, e);
            }
            return new Dictionary<BlockPos, Dictionary<string, MaterialProperties>>();
        }

        /*private Dictionary<Vec2i, Dictionary<string, double>> deserializePollution(string name)
        {
            try
            {
                byte[] data = sapi.WorldManager.SaveGame.GetData(name);
                if (data != null)
                {
                    return SerializerUtil.Deserialize<Dictionary<Vec2i, Dictionary<string, double>>>(data);
                }
            }
            catch (Exception e)
            {
                sapi.World.Logger.Error("Failed loading Pollution.{0}. Resetting. Exception: {1}", name, e);
            }
            return new Dictionary<Vec2i, Dictionary<string, double>>();
        }*/

        private void OnChunkData(ChunkThermoData msg)
        {
            IWorldChunk chunk = api.World.BlockAccessor.GetChunk(msg.chunkX, msg.chunkY, msg.chunkZ);
            chunk?.SetModdata("thermoinfo", msg.Data);
        }

        void SaveThermodynamicData(Dictionary<int, Dictionary<string, MaterialProperties>> thermoData, BlockPos pos)
        {
            int chunksize = api.World.BlockAccessor.ChunkSize;
            int chunkX = pos.X / chunksize;
            int chunkY = pos.Y / chunksize;
            int chunkZ = pos.Z / chunksize;

            byte[] data = SerializerUtil.Serialize(thermoData);

            IWorldChunk chunk = api.World.BlockAccessor.GetChunk(chunkX, chunkY, chunkZ);
            chunk.SetModdata("thermoinfo", data);

            // Todo: Send only to players that have this chunk in their loaded range
            serverChannel?.BroadcastPacket(new ChunkThermoData() { chunkX = chunkX, chunkY = chunkY, chunkZ = chunkZ, Data = data });
        }

        Dictionary<int, Dictionary<string, MaterialProperties>> GetOrCreateMatterAt(BlockPos pos)
        {
            byte[] data;

            IWorldChunk chunk = api.World.BlockAccessor.GetChunkAtBlockPos(pos);
            if (chunk == null) return null;

            data = chunk.GetModdata("thermoinfo");

            Dictionary<int, Dictionary<string, MaterialProperties>> matterOfChunk;
            if (data != null)
            {
                try
                {
                    matterOfChunk = SerializerUtil.Deserialize<Dictionary<int, Dictionary<string, MaterialProperties>>>(data);
                }
                catch (Exception)
                {
                    matterOfChunk = new Dictionary<int, Dictionary<string, MaterialProperties>>();
                }
            }
            else
            {
                matterOfChunk = new Dictionary<int, Dictionary<string, MaterialProperties>>();
            }

            return matterOfChunk;
        }

        static Dictionary<int, Dictionary<string, MaterialProperties>> GetOrCreateMatterAt(IWorldChunk chunk)
        {
            byte[] data;

            data = chunk.GetModdata("thermoinfo");

            Dictionary<int, Dictionary<string, MaterialProperties>> matterOfChunk;
            if (data != null)
            {
                try
                {
                    matterOfChunk = SerializerUtil.Deserialize<Dictionary<int, Dictionary<string, MaterialProperties>>>(data);
                }
                catch (Exception)
                {
                    matterOfChunk = new Dictionary<int, Dictionary<string, MaterialProperties>>();
                }
            }
            else
            {
                matterOfChunk = new Dictionary<int, Dictionary<string, MaterialProperties>>();
            }

            return matterOfChunk;
        }

        private void AddMatterBehavior()
        {
            if (!ThermalDynamicsConfig.Loaded.GasesEnabled) return;
            foreach (Block block in api.World.Blocks)
            {
                if (block.BlockId != 0)
                {
                    block.BlockBehaviors = block.BlockBehaviors.Append(new BlockBehaviorMatter(block));
                    block.CollectibleBehaviors = block.CollectibleBehaviors.Append(new BlockBehaviorMatter(block));
                }
            }

        }

        public Dictionary<string, MaterialProperties> GetMatter(BlockPos pos)
        {
            Dictionary<int, Dictionary<string, MaterialProperties>> matterOfChunk = GetOrCreateMatterAt(pos);
            if (matterOfChunk == null) return null;

            int index3d = ToLocalIndex(pos);
            if (!matterOfChunk.ContainsKey(index3d)) return null;

            return matterOfChunk[index3d];
        }

        public MaterialProperties GetMatter(BlockPos pos, string name)
        {
            Dictionary<string, MaterialProperties> matterHere = GetMatter(pos);

            if (matterHere == null || !matterHere.ContainsKey(name)) return default;

            return matterHere[name];
        }

        public Dictionary<string, MaterialProperties> RemoveMatter(BlockPos pos)
        {
            Dictionary<int, Dictionary<string, MaterialProperties>> matterOfChunk = GetOrCreateMatterAt(pos);
            if (matterOfChunk == null) return null;

            int index3d = ToLocalIndex(pos);
            if (!matterOfChunk.ContainsKey(index3d) || matterOfChunk[index3d] == null) return null;

            Dictionary<string, MaterialProperties> result = new Dictionary<string, MaterialProperties>(matterOfChunk[index3d]);

            if (matterOfChunk.Remove(index3d))
            {
                SaveThermodynamicData(matterOfChunk, pos);
                return result;
            }
            return null;
        }

        public void SetMatter(BlockPos pos, Dictionary<string, MaterialProperties> matterputhere)
        {
            Dictionary<int, Dictionary<string, MaterialProperties>> matterOfChunk = GetOrCreateMatterAt(pos);
            if (matterOfChunk == null) return;

            int index3d = ToLocalIndex(pos);
            if (!matterOfChunk.ContainsKey(index3d))
            {
                matterOfChunk.Add(index3d, matterputhere);
            }
            else
            {
                matterOfChunk[index3d] = matterputhere;
            }

            SaveThermodynamicData(matterOfChunk, pos);
        }

        public float GetTotalGasMass(BlockPos pos)
        {
            Dictionary<string, MaterialProperties> matterHere = GetMatter(pos);
            if (matterHere == null) return default;
            float mass = 0;

            foreach (var matter in matterHere)
            {
                if (GasDictionary.ContainsKey(matter.Key) && matter.Value != default && matter.Value.MolarDensity != float.NaN)
                {
                    if (GasDictionary[matter.Key] != null) mass += matter.Value.MolarDensity * GasDictionary[matter.Key].MolarMass;
                }
            }
            return mass;
        }

        /*public float GetAcidity(BlockPos pos)
        {
            Dictionary<string, float> gasesHere = GetMatter(pos);

            if (gasesHere == null) return 0;

            float conc = 0;

            foreach (var matter in gasesHere)
            {
                if (GasDictionary.ContainsKey(matter.Key))
                {
                    if (GasDictionary[matter.Key] != null && GasDictionary[matter.Key].Acidic) conc += matter.Value;
                    if (conc >= 1) return 1;
                }
            }

            return conc;
        }*/

        /*public bool IsVolatile(BlockPos pos)
        {
            Dictionary<string, float> gasesHere = GetMatter(pos);

            if (gasesHere == null) return false;

            foreach (var matter in gasesHere)
            {
                if (GasDictionary.ContainsKey(matter.Key))
                {
                    if (GasDictionary[matter.Key].FlammableAmount > 0 && matter.Value >= GasDictionary[matter.Key].FlammableAmount) return true;
                }
            }

            return false;
        }*/

        /*public bool ShouldExplode(BlockPos pos)
        {
            Dictionary<string, float> gasesHere = GetMatter(pos);

            if (gasesHere == null) return false;

            foreach (var matter in gasesHere)
            {
                if (GasDictionary.ContainsKey(matter.Key))
                {
                    if (GasDictionary[matter.Key].ExplosionAmount <= matter.Value) return true;
                }
            }

            return false;
        }*/

        /*public bool IsToxic(string name, float amount)
        {
            if (!GasDictionary.ContainsKey(name)) return true;

            return amount > GasDictionary[name].ToxicAt;
        }*/

        /*public void SetupExplosion(BlockPos pos, int radius)
        {
            if (pos == null || radius < 0) return;

            if (!ExplosionQueue.ContainsKey(pos))
            {
                Dictionary<string, float> dict = new Dictionary<string, float>();
                dict.Add("THISISANEXPLOSION", -100);
                int blocks = getBlockInRadius(radius);
                dict.Add("nitrogendioxide", 0.3f * blocks);
                dict.Add("carbonmonoxide", 0.01f * blocks);
                ExplosionQueue[pos] = dict;
            }
            else if (ExplosionQueue[pos].ContainsKey("RADIUS") && ExplosionQueue[pos]["RADIUS"] < radius)
            {
                ExplosionQueue[pos]["RADIUS"] = radius;
            }
        }*/

        /*public void EnqueueExplosion(BlockPos pos)
        {
            if (pos == null) return;

            if (!ExplosionQueue.ContainsKey(pos)) return;

            QueueMatterChange(ExplosionQueue[pos], pos);
            ExplosionQueue.Remove(pos);
        }*/

        /*public void AddToExplosion(BlockPos pos, Dictionary<string, float> gases)
        {
            if (pos == null || !ExplosionQueue.ContainsKey(pos)) return;

            Dictionary<string, float> dest = ExplosionQueue[pos];

            ThermodynamicHelper.MergeGasDicts(gases, ref dest);

            ExplosionQueue[pos] = dest;
        }*/

        /*public void AddPollution(BlockPos pos, string matter, float value)
        {
            if (pos == null || matter == null || value == 0) return;

            Vec2i columm = new Vec2i(pos.X / api.World.BlockAccessor.ChunkSize, pos.Z / api.World.BlockAccessor.ChunkSize);

            if (!PollutionPerChunk.ContainsKey(columm))
            {
                PollutionPerChunk.Add(columm, new Dictionary<string, double>());
                PollutionPerChunk[columm].Add(matter, value);
            }
            else
            {
                if (!PollutionPerChunk[columm].ContainsKey(matter))
                {
                    PollutionPerChunk[columm].Add(matter, value);
                }
                else
                {
                    PollutionPerChunk[columm][matter] += value;
                }
            }

            if (PollutionPerChunk[columm][matter] < 0) PollutionPerChunk[columm][matter] = 0;
        }*/

        public void QueueMatterChange(Dictionary<string, MaterialProperties> change, BlockPos pos)
        {
            if (change == null) change = new Dictionary<string, MaterialProperties>();

            BlockPos temp = pos.Copy();
            MaterialProperties tmp; 
            lock (matterDynamicsLock)
            {
                if (!matterDynamicsQueue.ContainsKey(temp)) matterDynamicsQueue.Add(temp, change);
                else
                {
                    foreach (var matter in change)
                    {
                        if (!matterDynamicsQueue[temp].ContainsKey(matter.Key)) matterDynamicsQueue[temp].Add(matter.Key, matter.Value);
                        else
                        {
                            tmp = matterDynamicsQueue[temp][matter.Key];
                            tmp.MolarDensity =+ matter.Value.MolarDensity;
                            matterDynamicsQueue[temp][matter.Key] = tmp;
                        }
                    }
                }
            }
            lock (heatDynamicsLock)
            {
                if (!heatDynamicsQueue.ContainsKey(temp)) heatDynamicsQueue.Add(temp, change);
                else
                {
                    foreach (var matter in change)
                    {
                        if (!heatDynamicsQueue[temp].ContainsKey(matter.Key)) heatDynamicsQueue[temp].Add(matter.Key, matter.Value);
                        else
                        {
                            tmp = heatDynamicsQueue[temp][matter.Key];
                            tmp.Temperature =+ matter.Value.Temperature;
                            heatDynamicsQueue[temp][matter.Key] = tmp;
                        }
                    }
                }
            }
            lock (pressureDynamicsLock)
            {
                if (!pressureDynamicsQueue.ContainsKey(temp)) pressureDynamicsQueue.Add(temp, change);
                else
                {
                    foreach (var matter in change)
                    {
                        if (!pressureDynamicsQueue[temp].ContainsKey(matter.Key)) pressureDynamicsQueue[temp].Add(matter.Key, matter.Value);
                        else
                        {
                            tmp = pressureDynamicsQueue[temp][matter.Key];
                            tmp.Pressure =+ matter.Value.Pressure;
                            pressureDynamicsQueue[temp][matter.Key] = tmp;
                        }
                    }
                }
            }
        }

        static int ToLocalIndex(BlockPos pos)
        {
            return MapUtil.Index3d(pos.X % 32, pos.Y % 32, pos.Z % 32, 32, 32);
        }

        bool FindPosInLayers(BlockPos pos, HashSet<BlockPos>[] layers)
        {
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].Contains(pos)) return true;
            }

            return false;
        }

        int GetBlockInRadius(int radius)
        {
            Vec4i[] comp = new Vec4i[] { new Vec4i(1, 0, 0, 1), new Vec4i(-1, 0, 0, 1), new Vec4i(0, -1, 0, 1), new Vec4i(0, 1, 0, 1), new Vec4i(0, 0, 1, 1), new Vec4i(0, 0, -1, 1) };

            List<Vec4i> counter = new List<Vec4i>();
            Queue<Vec4i> next = new Queue<Vec4i>();
            Vec4i origin = new Vec4i(0, 0, 0, 0);
            counter.Add(origin);
            next.Enqueue(origin);

            while (next.Count > 0)
            {
                Vec4i current = next.Dequeue();

                foreach (Vec4i side in comp)
                {
                    Vec4i test = new Vec4i(side.X + current.X, side.Y + current.Y, side.Z + current.Z, side.W + current.W);
                    if (test.W <= radius && !counter.Contains(test))
                    {
                        next.Enqueue(test);
                        counter.Add(test);
                    }
                }
            }

            return counter.Count;
        }
        class ThermodynamicThread
        {
            readonly int thermoTick = 50;
            IBlockAccessor blockAccessor;
            readonly ICoreServerAPI sapi;
            Dictionary<BlockPos, Dictionary<string, MaterialProperties>> checkMatter;
            readonly ThermalDynamicsSystem thermoSys;

            public bool Stopping { get; set; }
            public bool Paused { get; set; }

            public ThermodynamicThread(ICoreServerAPI sapi, ThermalDynamicsSystem gassys)
            {
                this.sapi = sapi;
                thermoSys = gassys;
            }

            public void Start(Dictionary<BlockPos, Dictionary<string, MaterialProperties>> checkMatter)
            {
                this.checkMatter = checkMatter;

                Thread thread = new Thread(() =>
                {
                    while (!sapi.Server.IsShuttingDown && !Stopping)
                    {
                        if (!Paused && ThermalDynamicsConfig.Loaded.GasesEnabled)
                        {
                            blockAccessor = sapi.World.BlockAccessor;
                            lock (matterDynamicsLock)
                            {
                                foreach (var block in checkMatter)
                                {
                                    if (block.Key == null || block.Value == null) continue;
                                    BlockPos current = block.Key;
                                    Dictionary<string, MaterialProperties> matter = block.Value;
                                    checkMatter.Remove(current);
                                    AddDistibuteMatterMatter(matter, current);
                                }
                            }
                            Thread.Sleep(thermoTick);
                        }
                    }
                });

                thread.IsBackground = true;
                thread.Name = "CheckGasSpread";
                thread.Start();
            }
            public void AddDistibuteMatterMatter(Dictionary<string, MaterialProperties> adds, BlockPos pos)
            {
                if (pos.Y < 1 || pos.Y > blockAccessor.MapSizeY) return;

                Dictionary<string, MaterialProperties> collectedMatter = adds ?? new Dictionary<string, MaterialProperties>();
                Queue<Vec3i> checkQueue = new Queue<Vec3i>();
                List<MaterialsChunk> chunks = new List<MaterialsChunk>();
                Dictionary<int, Block> blocks = new Dictionary<int, Block>();
                float windspeed = -1;
                int chunksize = blockAccessor.ChunkSize;
                int totalBlockCount = 1;
                bool openAir = false;

                for (int x = bounds.MinX / blockAccessor.ChunkSize; x <= bounds.MaxX / blockAccessor.ChunkSize; x++)
                {
                    for (int y = bounds.MinY / blockAccessor.ChunkSize; y <= bounds.MaxY / blockAccessor.ChunkSize; y++)
                    {
                        for (int z = bounds.MinZ / blockAccessor.ChunkSize; z <= bounds.MaxZ / blockAccessor.ChunkSize; z++)
                        {
                            IWorldChunk chunk = blockAccessor.GetChunk(x, y, z);

                            if (chunk != null)
                            {
                                chunks.Add(new MaterialsChunk(chunk, GetOrCreateMatterAt(chunk), x, y, z));
                            }
                        }
                    }
                }
                if (chunks.Count < 1) return;

                checkQueue.Enqueue(pos.ToVec3i());
                layers[pos.Y - bounds.MinY].Add(pos);
                Block starter = blockAccessor.GetBlock(pos);
                blocks.Add(starter.BlockId, starter);

                MaterialsChunk originChunk = null;

                foreach (MaterialsChunk chunk in chunks)
                {
                    if (chunk.Compare(pos, chunksize))
                    {
                        originChunk = chunk;
                        break;
                    }
                }

                if (originChunk == null) return;

                originChunk.TakeGas(ref collectedMatter, ToLocalIndex(pos));

                BlockFacing[] faces = BlockFacing.ALLFACES;
                BlockPos curPos = new BlockPos();

                while (checkQueue.Count > 0)
                {
                    //Gets Parent info
                    Vec3i bpos = checkQueue.Dequeue();

                    Block parent = null;
                    MaterialsChunk parentChunk = null;

                    foreach (MaterialsChunk chunk in chunks)
                    {
                        if (chunk.Compare(bpos.AsBlockPos, chunksize))
                        {
                            parentChunk = chunk;
                            break;
                        }
                    }

                    if (!blocks.ContainsKey(parentChunk.Chunk.Unpack_AndReadBlock(ToLocalIndex(bpos.AsBlockPos)))) continue;

                    parent = blocks[parentChunk.Chunk.Unpack_AndReadBlock(ToLocalIndex(bpos.AsBlockPos))];

                    //Process Children
                    foreach (BlockFacing facing in faces)
                    {
                        //Checks to see if this is a valid pos
                        if (!ignoreCheck && SolidCheck(parent, facing)) continue;
                        curPos.Set(bpos.X + facing.Normali.X, bpos.Y + facing.Normali.Y, bpos.Z + facing.Normali.Z);
                        if (!bounds.Contains(curPos) || layers[curPos.Y - bounds.MinY].Contains(curPos)) continue;
                        if (curPos.Y < 0 || curPos.Y > blockAccessor.MapSizeY) continue;

                        MaterialsChunk localArea = null;
                        int chunkBid = ToLocalIndex(curPos);
                        Block atPos = null;

                        foreach (MaterialsChunk chunk in chunks)
                        {
                            if (chunk.Compare(curPos, blockAccessor.ChunkSize))
                            {
                                localArea = chunk;
                                break;
                            }
                        }

                        if (localArea == null) continue;

                        int blockId = localArea.Chunk.Unpack_AndReadBlock(ToLocalIndex(curPos));

                        if (!blocks.TryGetValue(blockId, out atPos)) atPos = blocks[blockId] = blockAccessor.GetBlock(blockId);

                        if (!ignoreCheck && SolidCheck(atPos, facing.Opposite)) continue;
                        bool mediumComp = ignoreLiquid || !atPos.IsLiquid() || (parent.IsLiquid() && atPos.IsLiquid());
                        if (!mediumComp) continue;

                        //Confirmed this is a valid pos, now check other things
                        localArea.TakeGas(ref adds, chunkBid);

                        if (blockAccessor.GetRainMapHeightAt(curPos) < curPos.Y)
                        {
                            openAir = true;
                            windspeed = GetWindspeed(blockAccessor.GetWindSpeedAt(curPos.ToVec3d()), windspeed);
                        }

                        if (IsPlant(atPos)) plantNear++;
                        layers[curPos.Y - bounds.MinY].Add(curPos.Copy());
                        checkQueue.Enqueue(curPos.ToVec3i());
                        totalBlockCount++;
                    }
                }

                //Finished getting positions, now deal with gases
                Dictionary<string, float> modifier = new Dictionary<string, float>(collectedMatter);

                //Convert gases to their burned state, if this is an explosion
                if (combusted)
                {
                    foreach (var gas in collectedMatter)
                    {
                        if (GasDictionary.ContainsKey(gas.Key) && (GasDictionary[gas.Key].FlammableAmount <= 1 || GasDictionary[gas.Key].ExplosionAmount <= 1))
                        {
                            if (GasDictionary[gas.Key].BurnInto != null) ThermodynamicHelper.MergeFluidIntoDict(GasDictionary[gas.Key].BurnInto, gas.Value, ref modifier);

                            modifier.Remove(gas.Key);
                        }
                    }

                    collectedMatter = new Dictionary<string, float>(modifier);
                }

                //Spread gases
                foreach (var gas in collectedMatter)
                {
                    bool light = false;
                    bool plant = false;
                    bool distribute = false;
                    float wind = 0;
                    bool acid = false;
                    bool pollutant = false;

                    if (GasDictionary.ContainsKey(gas.Key) && GasDictionary[gas.Key] != null)
                    {
                        light = GasDictionary[gas.Key].Light;
                        plant = GasDictionary[gas.Key].PlantAbsorb;
                        distribute = GasDictionary[gas.Key].Distribute;
                        wind = GasDictionary[gas.Key].VentilateSpeed;
                        acid = GasDictionary[gas.Key].Acidic;
                        pollutant = GasDictionary[gas.Key].Pollutant;
                    }

                    if (plant && plantNear > 0)
                    {
                        modifier[gas.Key] -= plantNear;
                    }

                    if (openAir && windspeed >= wind)
                    {
                        thermoSys.AddPollution(pos, gas.Key, gas.Value);

                        continue;
                    }

                    if (modifier[gas.Key] <= 0) continue;

                    if (distribute)
                    {
                        float giveaway = Math.Min(gas.Value / totalBlockCount, 1);
                        for (int i = layers.Length - 1; i > 0; i--)
                        {
                            MaterialsChunk localArea = null;

                            foreach (BlockPos pil in layers[i])
                            {
                                if (localArea == null || !localArea.Compare(pil, blockAccessor.ChunkSize))
                                {
                                    foreach (MaterialsChunk chunk in chunks)
                                    {
                                        if (chunk.Compare(pil, blockAccessor.ChunkSize))
                                        {
                                            localArea = chunk;
                                            break;
                                        }
                                    }
                                }

                                localArea.SetGas(gas.Key, giveaway, ToLocalIndex(pil));
                            }
                        }
                    }
                    else if (light) //Distribute light gases
                    {
                        for (int i = layers.Length - 1; i > 0; i--)
                        {
                            if (layers[i].Count < 1) continue;
                            float giveaway = 1;
                            if (modifier[gas.Key] < layers[i].Count) giveaway = modifier[gas.Key] / layers[i].Count; else giveaway = 1;

                            MaterialsChunk localArea = null;

                            foreach (BlockPos pil in layers[i])
                            {
                                if (localArea == null || !localArea.Compare(pil, blockAccessor.ChunkSize))
                                {
                                    foreach (MaterialsChunk chunk in chunks)
                                    {
                                        if (chunk.Compare(pil, blockAccessor.ChunkSize))
                                        {
                                            localArea = chunk;
                                            break;
                                        }
                                    }
                                }

                                localArea.SetGas(gas.Key, giveaway, ToLocalIndex(pil));
                            }

                            modifier[gas.Key] -= layers[i].Count;
                            if (modifier[gas.Key] <= 0) break;
                        }
                    }
                    else //Distribute heavy gases
                    {

                        for (int i = 0; i < layers.Length; i++)
                        {
                            if (layers[i].Count < 1) continue;
                            float giveaway = 1;
                            if (modifier[gas.Key] < layers[i].Count) giveaway = modifier[gas.Key] / layers[i].Count; else giveaway = 1;

                            MaterialsChunk localArea = null;

                            foreach (BlockPos pil in layers[i])
                            {
                                if (localArea == null || !localArea.Compare(pil, blockAccessor.ChunkSize))
                                {
                                    foreach (MaterialsChunk chunk in chunks)
                                    {
                                        if (chunk.Compare(pil, blockAccessor.ChunkSize))
                                        {
                                            localArea = chunk;
                                            break;
                                        }
                                    }
                                }

                                localArea.SetGas(gas.Key, giveaway, ToLocalIndex(pil));
                            }

                            modifier[gas.Key] -= layers[i].Count;
                            if (modifier[gas.Key] <= 0) break;
                        }
                    }
                }

                //Save Time!!!
                foreach (MaterialsChunk chunk in chunks)
                {
                    chunk.SaveChunk(serverChannel);
                }
            }

            public bool SolidCheck(Block block, BlockFacing face)
            {
                if (block.Attributes?.KeyExists("thermosysSolidSides") == true)
                {
                    return block.Attributes["thermosysSolidSides"].IsTrue(face.Code);
                }

                return block.SideSolid[face.Index];
            }

            public bool IsPlant(Block block)
            {
                if (block.Attributes?.KeyExists("gassysPlant") == true) return block.Attributes["gassysPlant"].AsBool();

                return block.BlockMaterial == EnumBlockMaterial.Plant || block.BlockMaterial == EnumBlockMaterial.Leaves;
            }

            public float GetWindspeed(Vec3d windVec, float current)
            {
                float newwind = current;
                float x = (float)Math.Abs(windVec.X);
                float y = (float)Math.Abs(windVec.Y);
                float z = (float)Math.Abs(windVec.Z);

                newwind = x > newwind ? x : newwind;
                newwind = y > newwind ? y : newwind;
                newwind = z > newwind ? z : newwind;

                return newwind;
            }
        }
    }
}
