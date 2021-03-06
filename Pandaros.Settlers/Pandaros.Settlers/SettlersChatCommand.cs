﻿using System;
using System.Collections.Generic;
using ChatCommands;
using Pandaros.Settlers.Entities;
using Pipliz;
using Pipliz.JSON;

namespace Pandaros.Settlers
{
    [ModLoader.ModManager]
    public class SettlersChatCommand : IChatCommand
    {
        private static string _Setters = GameLoader.NAMESPACE + ".Settlers";

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnConstructWorldSettingsUI, GameLoader.NAMESPACE + "Settlers.AddSetting")]
        public static void AddSetting(Players.Player player, NetworkUI.NetworkMenu menu)
        {
            menu.Items.Add(new NetworkUI.Items.DropDown("Settlers", _Setters, new List<string>() { "Disabled", "Enabled" }));
            var ps = PlayerState.GetPlayerState(player);

            if (ps != null)
                menu.LocalStorage.SetAs(_Setters, Convert.ToInt32(ps.SettlersEnabled));
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerChangedNetworkUIStorage, GameLoader.NAMESPACE + "Settlers.ChangedSetting")]
        public static void ChangedSetting(TupleStruct<Players.Player, JSONNode, string> data)
        {
            switch (data.item3)
            {
                case "world_settings":
                    var ps = PlayerState.GetPlayerState(data.item1);
                    var maxToggleTimes = Configuration.GetorDefault("MaxSettlersToggle", 4);

                    if (ps != null && data.item2.GetAsOrDefault(_Setters, Convert.ToInt32(ps.SettlersEnabled)) != Convert.ToInt32(ps.SettlersEnabled))
                    {
                        if (!Configuration.GetorDefault("SettlersEnabled", true))
                            PandaChat.Send(data.item1, "The server administrator had disabled the changing of Settlers.", ChatColor.red);
                        else if (!HasToggeledMaxTimes(maxToggleTimes, ps, data.item1))
                            TurnSettlersOn(data.item1, ps, maxToggleTimes, data.item2.GetAsOrDefault(_Setters, Convert.ToInt32(ps.SettlersEnabled)) != 0);

                        PandaChat.Send(data.item1, "Settlers! Mod Settlers are now " + (ps.SettlersEnabled ? "on" : "off"), ChatColor.green);
                    }

                    break;
            }
        }

        public bool IsCommand(string chat)
        {
            return chat.StartsWith("/settlers", StringComparison.OrdinalIgnoreCase);
        }

        public bool TryDoCommand(Players.Player player, string chat)
        {
            if (player == null || player.ID == NetworkID.Server)
                return true;

            var array          = CommandManager.SplitCommand(chat);
            var colony         = Colony.Get(player);
            var state          = PlayerState.GetPlayerState(player);
            var maxToggleTimes = Configuration.GetorDefault("MaxSettlersToggle", 4);

            if (maxToggleTimes == 0 && !Configuration.GetorDefault("SettlersEnabled", true))
            {
                PandaChat.Send(player, "The server administrator had disabled the changing of Settlers.",
                               ChatColor.red);

                return true;
            }

            if (HasToggeledMaxTimes(maxToggleTimes, state, player))
                return true;

            if (array.Length == 1)
            {
                PandaChat.Send(player, "Settlers! Settlers are {0}. You have toggled this {1} out of {2} times.",
                               ChatColor.green, state.SettlersEnabled ? "on" : "off",
                               state.SettlersToggledTimes.ToString(), maxToggleTimes.ToString());

                return true;
            }

            if (array.Length == 2 && state.SettlersToggledTimes <= maxToggleTimes)
            {
                TurnSettlersOn(player, state, maxToggleTimes, array[1].ToLower().Trim() == "on" || array[1].ToLower().Trim() == "true");
            }

            return true;
        }

        private static bool HasToggeledMaxTimes(int maxToggleTimes, PlayerState state, Players.Player player)
        {
            if (state.SettlersToggledTimes >= maxToggleTimes)
            {
                PandaChat.Send(player,
                               $"To limit abuse of the /settlers command you can no longer toggle settlers on or off. You have used your alloted {maxToggleTimes} times.",
                               ChatColor.red);

                return true;
            }

            return false;
        }

        private static void TurnSettlersOn(Players.Player player, PlayerState state, int maxToggleTimes, bool enabled)
        {
            if (!state.SettlersEnabled)
                state.SettlersToggledTimes++;

            state.SettlersEnabled = enabled;

            PandaChat.Send(player,
                           $"Settlers! Mod Settlers are now on. You have toggled this {state.SettlersToggledTimes} out of {maxToggleTimes} times.",
                           ChatColor.green);

            NetworkUI.NetworkMenuManager.SendWorldSettingsUI(player);
        }
    }
}