﻿using System;
using System.Collections.Generic;
using System.IO;
using BlockTypes.Builtin;
using ChatCommands.Implementations;
using Pandaros.Settlers.Entities;
using Pandaros.Settlers.Jobs;
using Pandaros.Settlers.Jobs.Roaming;
using Pandaros.Settlers.Managers;
using Pipliz;
using Pipliz.JSON;
using Time = Pipliz.Time;

namespace Pandaros.Settlers.Items.Machines
{
    public class TeleportPadRegister : IRoamingJobObjective
    {
        public string Name => nameof(TeleportPad);
        public float WorkTime => 10;
        public ushort ItemIndex => TeleportPad.Item.ItemIndex;
        public Dictionary<string, IRoamingJobObjectiveAction> ActionCallbacks { get; } = new Dictionary<string, IRoamingJobObjectiveAction>()
        {
            { MachineConstants.REFUEL, new RefuelTeleportPad() },
            { MachineConstants.REPAIR, new RepairTeleportPad() },
            { MachineConstants.RELOAD, new ReloadTeleportPad() }
        };

        public string ObjectiveCategory => MachineConstants.MECHANICAL;

        public void DoWork(Players.Player player, RoamingJobState state)
        {
            TeleportPad.DoWork(player, state);
        }
    }

    public class RepairTeleportPad : IRoamingJobObjectiveAction
    {
        public string Name => MachineConstants.REPAIR;

        public float TimeToPreformAction => 10;

        public string AudoKey => GameLoader.NAMESPACE + ".HammerAudio";

        public ushort ObjectiveLoadEmptyIcon => GameLoader.Repairing_Icon;

        public ushort PreformAction(Players.Player player, RoamingJobState state)
        {
            return TeleportPad.Repair(player, state);
        }
    }

    public class ReloadTeleportPad : IRoamingJobObjectiveAction
    {
        public string Name => MachineConstants.RELOAD;

        public float TimeToPreformAction => 5;

        public string AudoKey => GameLoader.NAMESPACE + ".ReloadingAudio";

        public ushort ObjectiveLoadEmptyIcon => GameLoader.Reload_Icon;

        public ushort PreformAction(Players.Player player, RoamingJobState state)
        {
            return TeleportPad.Reload(player, state);
        }
    }

    public class RefuelTeleportPad : IRoamingJobObjectiveAction
    {
        public string Name => MachineConstants.REFUEL;

        public float TimeToPreformAction => 4;

        public string AudoKey => GameLoader.NAMESPACE + ".ReloadingAudio";

        public ushort ObjectiveLoadEmptyIcon => GameLoader.Reload_Icon;

        public ushort PreformAction(Players.Player player, RoamingJobState state)
        {
            return TeleportPad.Refuel(player, state);
        }
    }


    [ModLoader.ModManager]
    public static class TeleportPad
    {
        private static readonly Dictionary<Vector3Int, Vector3Int> _paired = new Dictionary<Vector3Int, Vector3Int>();
        private static readonly Dictionary<Players.Player, int> _cooldown = new Dictionary<Players.Player, int>();

        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        public static ushort Repair(Players.Player player, RoamingJobState machineState)
        {
            var retval = GameLoader.Repairing_Icon;
            var ps     = PlayerState.GetPlayerState(player);

            if (machineState.ActionLoad[MachineConstants.REPAIR] < .75f)
            {
                var repaired       = false;
                var requiredForFix = new List<InventoryItem>();
                var stockpile      = Stockpile.GetStockPile(player);

                requiredForFix.Add(new InventoryItem(BuiltinBlocks.StoneBricks, 5));
                requiredForFix.Add(new InventoryItem(BuiltinBlocks.ScienceBagBasic, 2));

                if (machineState.ActionLoad[MachineConstants.REPAIR] < .10f)
                {
                    requiredForFix.Add(new InventoryItem(BuiltinBlocks.ScienceBagAdvanced, 2));
                    requiredForFix.Add(new InventoryItem(BuiltinBlocks.ScienceBagColony, 2));
                    requiredForFix.Add(new InventoryItem(BuiltinBlocks.Crystal, 2));
                }
                else if (machineState.ActionLoad[MachineConstants.REPAIR] < .30f)
                {
                    requiredForFix.Add(new InventoryItem(BuiltinBlocks.ScienceBagAdvanced, 1));
                    requiredForFix.Add(new InventoryItem(BuiltinBlocks.ScienceBagColony, 2));
                    requiredForFix.Add(new InventoryItem(BuiltinBlocks.Crystal, 2));
                }
                else if (machineState.ActionLoad[MachineConstants.REPAIR] < .50f)
                {
                    requiredForFix.Add(new InventoryItem(BuiltinBlocks.ScienceBagAdvanced, 1));
                    requiredForFix.Add(new InventoryItem(BuiltinBlocks.Crystal, 1));
                }

                if (stockpile.Contains(requiredForFix))
                {
                    stockpile.TryRemove(requiredForFix);
                    repaired = true;
                }
                else
                {
                    foreach (var item in requiredForFix)
                        if (!stockpile.Contains(item))
                        {
                            retval = item.Type;
                            break;
                        }
                }

                if (repaired)
                {
                    machineState.ActionLoad[MachineConstants.REPAIR] = RoamingJobState.GetMaxLoad(MachineConstants.REPAIR, player, MachineConstants.MECHANICAL);

                    if (_paired.ContainsKey(machineState.Position) &&
                        GetPadAt(_paired[machineState.Position], out var ms))
                        ms.ActionLoad[MachineConstants.REPAIR] = RoamingJobState.GetMaxLoad(MachineConstants.REPAIR, player, MachineConstants.MECHANICAL);
                }
            }

            return retval;
        }

        public static ushort Reload(Players.Player player, RoamingJobState machineState)
        {
            return GameLoader.Waiting_Icon;
        }

        public static ushort Refuel(Players.Player player, RoamingJobState machineState)
        {
            var ps = PlayerState.GetPlayerState(player);

            if (machineState.ActionLoad[MachineConstants.REFUEL] < .75f)
            {
                RoamingJobState paired = null;

                if (_paired.ContainsKey(machineState.Position))
                    GetPadAt(_paired[machineState.Position], out paired);

                var stockpile = Stockpile.GetStockPile(player);

                while (stockpile.TryRemove(Mana.Item.ItemIndex) &&
                       machineState.ActionLoad[MachineConstants.REFUEL] < RoamingJobState.GetMaxLoad(MachineConstants.REFUEL, player, MachineConstants.MECHANICAL))
                {
                    machineState.ActionLoad[MachineConstants.REFUEL] += 0.20f;

                    if (paired != null)
                        paired.ActionLoad[MachineConstants.REFUEL] += 0.20f;
                }

                if (machineState.ActionLoad[MachineConstants.REFUEL] < RoamingJobState.GetMaxLoad(MachineConstants.REFUEL, player, MachineConstants.MECHANICAL))
                    return Mana.Item.ItemIndex;
            }

            return GameLoader.Refuel_Icon;
        }

        public static void DoWork(Players.Player player, RoamingJobState machineState)
        {
            if (!Configuration.TeleportPadsRequireMachinists)
                return;

            if (!player.IsConnected && Configuration.OfflineColonies || player.IsConnected)
                if (_paired.ContainsKey(machineState.Position) &&
                    GetPadAt(_paired[machineState.Position], out var ms) &&
                    machineState.ActionLoad[MachineConstants.REPAIR] > 0 &&
                    machineState.ActionLoad[MachineConstants.REFUEL] > 0 &&
                    machineState.NextTimeForWork < Time.SecondsSinceStartDouble)
                {
                    machineState.ActionLoad[MachineConstants.REPAIR] -= 0.01f;
                    machineState.ActionLoad[MachineConstants.REFUEL] -= 0.05f;

                    if (machineState.ActionLoad[MachineConstants.REPAIR] < 0)
                        machineState.ActionLoad[MachineConstants.REPAIR] = 0;

                    if (machineState.ActionLoad[MachineConstants.REFUEL] <= 0)
                        machineState.ActionLoad[MachineConstants.REFUEL] = 0;

                    machineState.NextTimeForWork = machineState.RoamingJobSettings.WorkTime + Time.SecondsSinceStartDouble;
                }
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.Machines.TeleportPad.RegisterTeleportPad")]
        public static void RegisterTeleportPad()
        {
            var rivets  = new InventoryItem(BuiltinBlocks.IronRivet, 6);
            var steel   = new InventoryItem(BuiltinBlocks.SteelIngot, 5);
            var sbb     = new InventoryItem(BuiltinBlocks.ScienceBagBasic, 20);
            var sbc     = new InventoryItem(BuiltinBlocks.ScienceBagColony, 20);
            var sba     = new InventoryItem(BuiltinBlocks.ScienceBagAdvanced, 20);
            var crystal = new InventoryItem(BuiltinBlocks.Crystal, 5);
            var stone   = new InventoryItem(BuiltinBlocks.StoneBricks, 50);
            var mana    = new InventoryItem(Mana.Item.ItemIndex, 100);

            var recipe = new Recipe(Item.name,
                                    new List<InventoryItem> {crystal, steel, rivets, sbb, sbc, sba, crystal, stone},
                                    new InventoryItem(Item.ItemIndex),
                                    6);

            RecipeStorage.AddOptionalLimitTypeRecipe(AdvancedCrafterRegister.JOB_NAME, recipe);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterSelectedWorld, GameLoader.NAMESPACE + ".Items.Machines.TeleportPad.AddTextures")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.registertexturemappingtextures")]
        public static void AddTextures()
        {
            var TeleportPadTextureMapping = new ItemTypesServer.TextureMapping(new JSONNode());
            TeleportPadTextureMapping.AlbedoPath   = GameLoader.BLOCKS_ALBEDO_PATH + "TeleportPad.png";
            TeleportPadTextureMapping.EmissivePath = GameLoader.BLOCKS_EMISSIVE_PATH + "TeleportPad.png";

            ItemTypesServer.SetTextureMapping(GameLoader.NAMESPACE + ".TeleportPad", TeleportPadTextureMapping);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterAddingBaseTypes,  GameLoader.NAMESPACE + ".Items.Machines.TeleportPad.AddTeleportPad")]
        [ModLoader.ModCallbackDependsOn("pipliz.blocknpcs.addlittypes")]
        public static void AddTeleportPad(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            var TeleportPadName = GameLoader.NAMESPACE + ".TeleportPad";
            var TeleportPadNode = new JSONNode();
            TeleportPadNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "TeleportPad.png");
            TeleportPadNode["isPlaceable"] = new JSONNode(true);
            TeleportPadNode.SetAs("onRemoveAmount", 1);
            TeleportPadNode.SetAs("onPlaceAudio", "stonePlace");
            TeleportPadNode.SetAs("onRemoveAudio", "stoneDelete");
            TeleportPadNode.SetAs("isSolid", false);
            TeleportPadNode.SetAs("sideall", "SELF");
            TeleportPadNode.SetAs("mesh", GameLoader.MESH_PATH + "TeleportPad.obj");

            var categories = new JSONNode(NodeType.Array);
            categories.AddToArray(new JSONNode("machine"));
            TeleportPadNode.SetAs("categories", categories);


            var TeleportPadCollidersNode = new JSONNode();
            var TeleportPadBoxesNode     = new JSONNode();
            var TeleportPadBoxesMinNode  = new JSONNode(NodeType.Array);
            TeleportPadBoxesMinNode.AddToArray(new JSONNode(-0.5));
            TeleportPadBoxesMinNode.AddToArray(new JSONNode(-0.5));
            TeleportPadBoxesMinNode.AddToArray(new JSONNode(-0.5));
            var TeleportPadBoxesMaxNode = new JSONNode(NodeType.Array);
            TeleportPadBoxesMaxNode.AddToArray(new JSONNode(0.5));
            TeleportPadBoxesMaxNode.AddToArray(new JSONNode(-0.3));
            TeleportPadBoxesMaxNode.AddToArray(new JSONNode(0.5));

            TeleportPadBoxesNode.SetAs("min", TeleportPadBoxesMinNode);
            TeleportPadBoxesNode.SetAs("max", TeleportPadBoxesMaxNode);
            TeleportPadCollidersNode.SetAs("boxes", TeleportPadBoxesNode);
            TeleportPadNode.SetAs("Colliders", TeleportPadCollidersNode);

            var TeleportPadCustomNode = new JSONNode();
            TeleportPadCustomNode.SetAs("useEmissiveMap", true);

            var torchNode  = new JSONNode();
            var aTorchnode = new JSONNode();

            aTorchnode.SetAs("color", "#236B94");
            aTorchnode.SetAs("intensity", 8);
            aTorchnode.SetAs("range", 6);
            aTorchnode.SetAs("volume", 0.5);

            torchNode.SetAs("a", aTorchnode);

            TeleportPadCustomNode.SetAs("torches", torchNode);
            TeleportPadNode.SetAs("customData", TeleportPadCustomNode);

            Item = new ItemTypesServer.ItemTypeRaw(TeleportPadName, TeleportPadNode);
            items.Add(TeleportPadName, Item);
        }

        public static bool GetPadAt(Vector3Int pos, out RoamingJobState state)
        {
            try
            {
                if (pos != null)
                    lock (RoamingJobManager.Objectives)
                    {
                        foreach (var p in RoamingJobManager.Objectives)
                            if (p.Value.ContainsKey(pos))
                                if (p.Value[pos].RoamObjective == nameof(TeleportPad))
                                {
                                    state = p.Value[pos];
                                    return true;
                                }
                    }
            }
            catch (Exception ex)
            {
                PandaLogger.LogError(ex);
            }

            state = null;
            return false;
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnAutoSaveWorld, GameLoader.NAMESPACE + ".Items.Machines.Teleportpad.OnAutoSaveWorld")]
        public static void OnAutoSaveWorld()
        {
            Save();
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnQuitEarly, GameLoader.NAMESPACE + ".Items.Machines.Teleportpad.OnQuitEarly")]
        public static void OnQuitEarly()
        {
            Save();
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerDisconnected, GameLoader.NAMESPACE + ".Items.Machines.Teleportpad.OnPlayerDisconnected")]
        public static void OnPlayerDisconnected(Players.Player p)
        {
            Save();
        }

        private static void Save()
        {
            JSONNode n = null;

            if (File.Exists(RoamingJobManager.MACHINE_JSON))
                JSON.Deserialize(RoamingJobManager.MACHINE_JSON, out n);

            if (n == null)
                n = new JSONNode();

            if (n.HasChild(GameLoader.NAMESPACE + ".Teleportpads"))
                n.RemoveChild(GameLoader.NAMESPACE + ".Teleportpads");

            var teleporters = new JSONNode(NodeType.Array);

            foreach (var pad in _paired)
            {
                var kvpNode = new JSONNode();
                kvpNode.SetAs("Key", (JSONNode) pad.Key);
                kvpNode.SetAs("Value", (JSONNode) pad.Value);
                teleporters.AddToArray(kvpNode);
            }

            n[GameLoader.NAMESPACE + ".Teleportpads"] = teleporters;

            using (var writer = File.CreateText(RoamingJobManager.MACHINE_JSON))
            {
                n.Serialize(writer, 1, 1);
            }
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterWorldLoad, GameLoader.NAMESPACE + ".Items.Machines.Teleportpad.AfterWorldLoad")]
        public static void AfterWorldLoad()
        {
            if (File.Exists(RoamingJobManager.MACHINE_JSON) &&
                JSON.Deserialize(RoamingJobManager.MACHINE_JSON, out var n) &&
                n.TryGetChild(GameLoader.NAMESPACE + ".Teleportpads", out var teleportPads))
                foreach (var pad in teleportPads.LoopArray())
                    _paired[(Vector3Int) pad.GetAs<JSONNode>("Key")] = (Vector3Int) pad.GetAs<JSONNode>("Value");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerMoved, GameLoader.NAMESPACE + ".Items.Machines.Teleportpad.OnPlayerMoved")]
        public static void OnPlayerMoved(Players.Player p)
        {
            var posBelow = new Vector3Int(p.Position);

            if (GetPadAt(posBelow, out var machineState) &&
                _paired.ContainsKey(machineState.Position) &&
                GetPadAt(_paired[machineState.Position], out var paired))
            {
                var startInt = Time.SecondsSinceStartInt;

                if (!_cooldown.ContainsKey(p))
                    _cooldown.Add(p, 0);

                if (_cooldown[p] <= startInt)
                {
                    Teleport.TeleportTo(p, paired.Position.Vector);

                    ServerManager.SendAudio(machineState.Position.Vector,
                                            GameLoader.NAMESPACE + ".TeleportPadMachineAudio");

                    ServerManager.SendAudio(paired.Position.Vector, GameLoader.NAMESPACE + ".TeleportPadMachineAudio");

                    _cooldown[p] = Configuration.GetorDefault("TeleportPadCooldown", 15) + startInt;
                }
            }
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnTryChangeBlock,  GameLoader.NAMESPACE + ".Items.Machines.Teleportpad.OnTryChangeBlockUser")]
        public static void OnTryChangeBlockUser(ModLoader.OnTryChangeBlockData d)
        {
            if (d.CallbackState == ModLoader.OnTryChangeBlockData.ECallbackState.Cancelled)
                return;

            if (d.TypeNew == Item.ItemIndex && d.TypeOld == BuiltinBlocks.Air)
            {
                var ps = PlayerState.GetPlayerState(d.RequestedByPlayer);
                var ms = new RoamingJobState(d.Position, d.RequestedByPlayer, nameof(TeleportPad));

                if (ps.TeleporterPlaced == Vector3Int.invalidPos)
                {
                    ps.TeleporterPlaced = d.Position;

                    PandaChat.Send(d.RequestedByPlayer, $"Place one more teleportation pad to link to.",
                                   ChatColor.orange);
                }
                else
                {
                    if (GetPadAt(ps.TeleporterPlaced, out var machineState))
                    {
                        _paired[ms.Position]           = machineState.Position;
                        _paired[machineState.Position] = ms.Position;
                        PandaChat.Send(d.RequestedByPlayer, $"Teleportation pads linked!", ChatColor.orange);
                        ps.TeleporterPlaced = Vector3Int.invalidPos;
                    }
                    else
                    {
                        ps.TeleporterPlaced = d.Position;

                        PandaChat.Send(d.RequestedByPlayer, $"Place one more teleportation pad to link to.",
                                       ChatColor.orange);
                    }
                }

                RoamingJobManager.RegisterRoamingJobState(d.RequestedByPlayer, ms);
            }
        }

        private static void MachineManager_MachineRemoved(object sender, EventArgs e)
        {
            var machineState = sender as RoamingJobState;

            if (machineState != null &&
                machineState.RoamObjective == nameof(TeleportPad))
            {
                var ps = PlayerState.GetPlayerState(machineState.Owner);

                if (_paired.ContainsKey(machineState.Position) &&
                    GetPadAt(_paired[machineState.Position], out var paired))
                {
                    if (_paired.ContainsKey(machineState.Position))
                        _paired.Remove(machineState.Position);

                    if (_paired.ContainsKey(paired.Position))
                        _paired.Remove(paired.Position);

                    RoamingJobManager.RemoveObjective(machineState.Owner, paired.Position, false);
                    ServerManager.TryChangeBlock(paired.Position, BuiltinBlocks.Air);

                    if (!Inventory.GetInventory(machineState.Owner).TryAdd(Item.ItemIndex))
                    {
                        var stockpile = Stockpile.GetStockPile(machineState.Owner);
                        stockpile.Add(Item.ItemIndex);
                    }
                }

                if (machineState.Position == ps.TeleporterPlaced)
                    ps.TeleporterPlaced = Vector3Int.invalidPos;
            }
        }
    }
}