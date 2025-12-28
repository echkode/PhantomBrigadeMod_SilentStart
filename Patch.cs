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
		static IEnumerable<CodeInstruction> Cu_ConfirmExecutionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) =>
			SettingsGuardTranspiler(instructions, generator, CodeInstruction.LoadField(typeof(GameSettings), nameof(GameSettings.enableExecuteSound)));

		[HarmonyPatch(typeof(CIViewCombatExecution), nameof(CIViewCombatExecution.CheckAndAttemptExecution))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Civce_CheckAndAttemptExecutionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Stash setting in separate variable so the dialog can discriminate where it was called from.
			var openMethodInfo = AccessTools.DeclaredMethod(typeof(CIViewDialogConfirmation), nameof(CIViewDialogConfirmation.Open));
			var openMatch = new CodeMatch(OpCodes.Callvirt, openMethodInfo);
			var loadInstanceMatch = new CodeMatch(CodeInstruction.LoadField(typeof(CIViewDialogConfirmation), nameof(CIViewDialogConfirmation.ins)));
			var loadSetting = CodeInstruction.LoadField(typeof(GameSettings), nameof(GameSettings.enableExecuteSound));
			var storeSetting = CodeInstruction.StoreField(typeof(Patch), nameof(Patch.playSoundInDialog));

			FileLog.Log("open: " + openMatch);
			FileLog.Log("load: " + loadInstanceMatch);
			var cm = new CodeMatcher(instructions, generator);
			cm.End();
			cm.MatchStartBackwards(openMatch)
				.MatchStartBackwards(loadInstanceMatch)
				.InsertAndAdvance(loadSetting)
				.InsertAndAdvance(storeSetting);

			return cm.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(CIViewDialogConfirmation), nameof(CIViewDialogConfirmation.SetAudioEvents))]
		[HarmonyPostfix]
		static void Civdc_SetAudioEventsPostfix(CIViewDialogConfirmation __instance)
		{
			if (__instance.buttonConfirm.audio != null && __instance.buttonConfirm.audio.onClick != null)
			{
				__instance.buttonConfirm.audio.onClick.enabled = playSoundInDialog;
			}
		}

		[HarmonyPatch(typeof(CIViewDialogConfirmation), nameof(CIViewDialogConfirmation.TryEntry))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Civdc_TryEntryTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) =>
			SettingsGuardTranspiler(instructions, generator, CodeInstruction.LoadField(typeof(Patch), nameof(Patch.playSoundInDialog)));

		[HarmonyPatch(typeof(CIViewDialogConfirmation), nameof(CIViewDialogConfirmation.TryExit))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Civdc_TryExitTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) =>
			SettingsGuardTranspiler(instructions, generator, CodeInstruction.LoadField(typeof(Patch), nameof(Patch.playSoundInDialog)));

		[HarmonyPatch(typeof(CIViewDialogConfirmation), nameof(CIViewDialogConfirmation.TryExit))]
		[HarmonyPostfix]
		static void Civdc_TryExitPostfix()
		{
			playSoundInDialog = true;
		}

		static IEnumerable<CodeInstruction> SettingsGuardTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, CodeInstruction loadSetting)
		{
			// Guard call to play sound with settings check.
			var createAudioEventMatch = new CodeMatch(CodeInstruction.Call(typeof(AudioUtility), nameof(AudioUtility.CreateAudioEvent), new[]
			{
				typeof(string)
			}));

			var cm = new CodeMatcher(instructions, generator);
			cm.Start();
			cm.MatchEndForward(createAudioEventMatch)
				.Advance(2)
				.CreateLabel(out var skipLabel)
				.Advance(-3)
				.InsertAndAdvance(loadSetting)
				.InsertAndAdvance(new CodeInstruction(OpCodes.Brfalse_S, skipLabel));

			return cm.InstructionEnumeration();
		}

		public static bool playSoundInDialog = true;
	}
}
