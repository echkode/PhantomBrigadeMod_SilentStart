// Copyright (c) 2025 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection.Emit;

using HarmonyLib;

using PhantomBrigade;

namespace EchKode.PBMods.SilentStart
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(CombatUtilities), nameof(CombatUtilities.ConfirmExecution))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Cu_ConfirmExecutionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Guard call to play sound with settings check.

			var cm = new CodeMatcher(instructions, generator);
			var createAudioEventMethodInfo = AccessTools.DeclaredMethod(typeof(AudioUtility), nameof(AudioUtility.CreateAudioEvent), new[]
			{
				typeof(string)
			});
			var createAudioEventMatch = new CodeMatch(OpCodes.Call, createAudioEventMethodInfo);
			var loadSetting = CodeInstruction.LoadField(typeof(GameSettings), nameof(GameSettings.enableExecuteSound));

			cm.Start();
			cm.MatchEndForward(createAudioEventMatch)
				.Advance(2)
				.CreateLabel(out var skipLabel)
				.Advance(-3)
				.InsertAndAdvance(loadSetting)
				.InsertAndAdvance(new CodeInstruction(OpCodes.Brfalse_S, skipLabel));

			return cm.InstructionEnumeration();
		}
	}
}
