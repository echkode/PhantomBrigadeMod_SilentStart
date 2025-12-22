// Copyright (c) 2025 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using HarmonyLib;

using PhantomBrigade.Data;

namespace EchKode.PBMods.SilentStart
{
    public static class GameSettings
    {
        public static void AddDelegates()
        {
            var implementationsFieldInfo = AccessTools.DeclaredField(typeof(SettingUtility), "implementations");
            if (implementationsFieldInfo == null)
            {
                return;
            }
            var initializedFieldInfo = AccessTools.DeclaredField(typeof(SettingUtility), "initialized");
            if (initializedFieldInfo == null)
            {
                return;
            }
            if (!(bool)initializedFieldInfo.GetValue(null))
            {
                SettingUtility.Initialize();
            }

            var implementations = (Dictionary<string, SettingImplementationDelegate>)implementationsFieldInfo.GetValue(null);
            if (implementations == null)
            {
                return;
            }
            implementations[keyEnableExecuteSound] = ExecuteStartSound;
        }

        static void ExecuteStartSound(DataContainerGameSetting definition, string valueRaw)
        {
            enableExecuteSound = SettingUtility.TryParseBool(valueRaw);
        }

        public static bool enableExecuteSound = true;

        // This is the key of the entry in the game settings config database.
        const string keyEnableExecuteSound = "game_combat_execute_start_sound";
    }
}
