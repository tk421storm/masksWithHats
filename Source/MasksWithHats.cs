using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace TKS_MasksWithHats
{
	[StaticConstructorOnStartup]
	public static class InsertHarmony
	{
		static InsertHarmony()
		{
			Harmony harmony = new Harmony("TKS_MasksWithHats");
			//Harmony.DEBUG = true;

			//print all that mod this function
			var functions = new object[] { typeof(ApparelUtility).GetMethod("CanWearTogether"), typeof(PawnRenderer).GetMethod("DrawBodyApparel", BindingFlags.NonPublic | BindingFlags.Instance), typeof(PawnRenderer).GetMethod("DrawHeadHair", BindingFlags.NonPublic | BindingFlags.Instance) };

			foreach (MethodBase original in functions)
			{
				if (original is null)
				{
					continue;
				}

				var patches = Harmony.GetPatchInfo(original);
				if (patches is null)
				{
					Log.Message("[TKS_MasksWithHats]MasksWithHats found no patches for " + original.Name);
					continue;
				};

				Log.Message("[TKS_MasksWithHats]MasksWithHats found patches for " + original.Name + ":");
				foreach (var patch in patches.Prefixes)
				{
					Log.Message("[TKS_MasksWithHats]index: " + patch.index);
					Log.Message("[TKS_MasksWithHats]owner: " + patch.owner);
					Log.Message("[TKS_MasksWithHats]patch method: " + patch.PatchMethod);
					Log.Message("[TKS_MasksWithHats]priority: " + patch.priority);
					Log.Message("[TKS_MasksWithHats]before: " + patch.before);
					Log.Message("[TKS_MasksWithHats]after: " + patch.after);
				}
			}

			harmony.PatchAll();

			//Harmony.DEBUG = false;
			Log.Message($"[TKS_MasksWithHats]: Patching finished");
		}

	}

	public class TKS_MasksWithHatsSettings : ModSettings
	{
		public bool debugPrint = false;

		public override void ExposeData()
		{
			Scribe_Values.Look(ref debugPrint, "debugPrint");

			base.ExposeData();
		}
	}

	public class TKS_MasksWithHatsMod : Mod
	{
		TKS_MasksWithHatsSettings settings;

		public static void DebugMessage(string message)
		{
			if (LoadedModManager.GetMod<TKS_MasksWithHatsMod>().GetSettings<TKS_MasksWithHatsSettings>().debugPrint)
			{
				Log.Message(message);
			}
		}


		public TKS_MasksWithHatsMod(ModContentPack content) : base(content)
		{
			this.settings = GetSettings<TKS_MasksWithHatsSettings>();
		}

		private string editBufferFloat;

		public override void DoSettingsWindowContents(Rect inRect)
		{
			Listing_Standard listingStandard = new Listing_Standard();
			listingStandard.Begin(inRect);
			listingStandard.CheckboxLabeled("TKSDebugPrint".Translate(), ref settings.debugPrint);
			listingStandard.End();
			base.DoSettingsWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "TKSMasksWithHatsName".Translate();
		}
	}


		[HarmonyPatch(typeof(ApparelUtility))]
	static class CanWearTogether
	{
		[HarmonyPatch(typeof(ApparelUtility), "CanWearTogether")]
		[HarmonyPrefix]
		static bool Prefix(ThingDef A, ThingDef B, BodyDef body, ref bool __result)
		{
			ApparelProperties aProps = A.apparel;
			ApparelProperties bProps = B.apparel;

			BodyPartGroupDef faceCover = TKS_BodyPartGroupDefOf.FaceCover;
			List<BodyPartRecord> faceCoverIncludes = new List<BodyPartRecord>(from x in body.AllParts where x.depth == BodyPartDepth.Outside && x.groups.Contains(faceCover) select x);

			TKS_MasksWithHatsMod.DebugMessage("checking CanWearTogether for (" + A.defName + ", " + B.defName + ")");

			//dont let a pawn choose two pieces of the same clothing (fixes bugs with odd pans types: hiver, android

			if (A.defName == B.defName)
            {
				TKS_MasksWithHatsMod.DebugMessage("not allowing two items of the same kind to be worn by pawn ("+A.defName+", "+B.defName+")");
				__result = false;
				return false;
            }

			
			//if (!aProps.bodyPartGroups.Contains(faceCover) && !bProps.bodyPartGroups.Contains(faceCover))
			if (!aProps.layers.Contains(TKS_ApparelLayerDefOf.FaceCover) && !bProps.layers.Contains(TKS_ApparelLayerDefOf.FaceCover))
			{
				//TKS_MasksWithHatsMod.DebugMessage("not checking body parts on " + A.defName + " or " + B.defName + " since neither include faceCover");
				return true;
			}

			if (aProps.layers.Contains(TKS_ApparelLayerDefOf.FaceCover) && bProps.layers.Contains(TKS_ApparelLayerDefOf.FaceCover))
			{
				TKS_MasksWithHatsMod.DebugMessage("not allowing " + A.defName + " with " + B.defName + " because they are both facecovers (" + A.defName + ", " + B.defName + ")");
				__result = false;
				return false;
			}

			if (bProps.layers.Contains(TKS_ApparelLayerDefOf.FaceCover))
			{
				//swap em
				ThingDef C = A;
				A = B;
				B = C;
			}
			TKS_MasksWithHatsMod.DebugMessage("Checking if " + A.defName + " can be worn with " + B.defName);
			aProps = A.apparel;
			bProps = B.apparel;
			TKS_MasksWithHatsMod.DebugMessage(A.defName + "has the following bodyPartGroups: " + aProps.bodyPartGroups.ToStringSafeEnumerable());
			foreach (BodyPartGroupDef group in aProps.bodyPartGroups)
			{
				TKS_MasksWithHatsMod.DebugMessage("Checking body part group " + group.defName);
				if (group.defName == "FaceCover")
				{
					//check that other apparel does not include bits that FaceCover includes
					TKS_MasksWithHatsMod.DebugMessage("faceCoverIncludes: " + faceCoverIncludes.ToStringSafeEnumerable());

					foreach (BodyPartGroupDef groupDef in bProps.bodyPartGroups)
					{
						List<BodyPartRecord> otherIncludes = new List<BodyPartRecord>(from x in body.AllParts where x.depth == BodyPartDepth.Outside && x.groups.Contains(groupDef) select x);
						foreach (BodyPartRecord bpr in otherIncludes)
						{
							if (bpr.Label != "head")
							{
								if (faceCoverIncludes.Contains(bpr))
								{
									TKS_MasksWithHatsMod.DebugMessage("Not allowing apparel " + A.defName.ToString() + " with " + B.defName.ToString() + " because faceCover already covers " + bpr.Label);
									__result = false;
									return false;
								}
							}
						}
						TKS_MasksWithHatsMod.DebugMessage("Allowing apparel " + A.defName.ToString() + " beacuse list " + aProps.GetCoveredOuterPartsString(body) + " doesn't include any of " + bProps.GetCoveredOuterPartsString(body));
					}
				}

			}
			return true;
		}

	}
	[HarmonyPatch(typeof(ApparelGraphicRecordGetter))]
	static class TryGetGraphicApparel
	{
		[HarmonyPatch(typeof(ApparelGraphicRecordGetter), "TryGetGraphicApparel")]
		[HarmonyPrefix]
		static bool Prefix_TryGetGraphicApparel(Apparel apparel, BodyTypeDef bodyType, ref ApparelGraphicRecord rec, ref bool __result)
		{

			if (bodyType == null)
			{
				Log.Error("[TKS_MasksWithHats]Getting apparel graphic with undefined body type.");
				bodyType = BodyTypeDefOf.Male;
			}
			if (apparel.WornGraphicPath.NullOrEmpty())
			{
				rec = new ApparelGraphicRecord(null, null);
				return false;
			}
			string path;
			if (apparel.def.apparel.LastLayer == TKS_MasksWithHats.TKS_ApparelLayerDefOf.FaceCover)
			{
				TKS_MasksWithHatsMod.DebugMessage("Getting apparel graphic for FaceMask object " + apparel.def.defName);
				path = apparel.WornGraphicPath;
				Shader shader = ShaderDatabase.Cutout;
				if (apparel.def.apparel.useWornGraphicMask)
				{
					shader = ShaderDatabase.CutoutComplex;
				}
				Graphic graphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, apparel.def.graphicData.drawSize, apparel.DrawColor);
				rec = new ApparelGraphicRecord(graphic, apparel);
				TKS_MasksWithHatsMod.DebugMessage("returning graphic for FaceMask object: " + graphic.ToString());
				__result = true;
				return false;
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(PawnApparelGenerator))]
	static class IsHeadgear
	{
		[HarmonyPatch(typeof(PawnApparelGenerator), "IsHeadgear")]
		[HarmonyPrefix]
		static bool IsHeadgear_prefix(ThingDef td, ref bool __result)
		{
			BodyPartGroupDef faceCover = TKS_BodyPartGroupDefOf.FaceCover;

			if (td.apparel.bodyPartGroups.Contains(faceCover))
			{
				__result = true;
				return false;
			}
			return true;
		}
	}

#if v1_4
	[HarmonyPatch(typeof(PawnRenderer), "DrawBodyApparel")]
	public static class DrawBodyApparel
	{
		[HarmonyPatch(typeof(PawnRenderer), "DrawBodyApparel")]
		[HarmonyPrefix]
		static bool DrawBodyApparel_Prefix(PawnRenderer __instance, Pawn ___pawn, Vector3 shellLoc, Vector3 utilityLoc, Mesh bodyMesh, float angle, Rot4 bodyFacing, PawnRenderFlags flags)
		{
			//if (bodyMesh is null) { Log.Warning(___pawn.Name+": DrawBodyApparel "+nameof(bodyMesh) + " is null!"); };

			List<ApparelGraphicRecord> apparelGraphics = __instance.graphics.apparelGraphics;

			BodyPartGroupDef faceCover = TKS_BodyPartGroupDefOf.FaceCover;
			bool containsFaceCover = false;

			foreach (ApparelGraphicRecord ag in apparelGraphics)
			{
				List<BodyPartGroupDef> parts = ag.sourceApparel?.def?.apparel?.bodyPartGroups;
				//if (parts is null) { Log.Warning(___pawn.Name + ": DrawBodyApparel apparelBodyPartGroups is null!"); };

				if (ag.sourceApparel.def.apparel.bodyPartGroups.Contains(faceCover))
				{
					containsFaceCover = true;
				}
			}

			if (!containsFaceCover)
			{
				return true;
			}

			MethodInfo overrideMaterialIfNeeded = typeof(PawnRenderer).GetMethod("OverrideMaterialIfNeeded", BindingFlags.NonPublic | BindingFlags.Instance);

			TKS_MasksWithHatsMod.DebugMessage("Overriding DrawBodyApparel for " + ___pawn + " due to facecover ");

			Quaternion quaternion = Quaternion.AngleAxis(angle, Vector3.up);
			for (int i = 0; i < apparelGraphics.Count; i++)
			{
				ApparelGraphicRecord apparelGraphicRecord = apparelGraphics[i];
				List<BodyPartGroupDef> parts = apparelGraphicRecord.sourceApparel?.def?.apparel?.bodyPartGroups;
				if (parts is null) { Log.Warning(___pawn.Name + ": DrawBodyApparel apparelBodyPartGroups is null!"); };

				if (parts.Contains(faceCover))
				{
					continue;
				}
				if (apparelGraphicRecord.sourceApparel.def.apparel.LastLayer == ApparelLayerDefOf.Shell && !apparelGraphicRecord.sourceApparel.def.apparel.shellRenderedBehindHead)
				{
					Material material = apparelGraphicRecord.graphic.MatAt(bodyFacing, null);
					var parameters = new object[] { material, ___pawn, flags.FlagSet(PawnRenderFlags.Portrait) };
					material = (flags.FlagSet(PawnRenderFlags.Cache) ? material : (Material)overrideMaterialIfNeeded.Invoke(__instance, parameters));
					Vector3 loc = shellLoc;
					if (apparelGraphicRecord.sourceApparel.def.apparel.shellCoversHead)
					{
						loc.y += 0.0028957527f;
					}
					GenDraw.DrawMeshNowOrLater(bodyMesh, loc, quaternion, material, flags.FlagSet(PawnRenderFlags.DrawNow));
				}
				if (PawnRenderer.RenderAsPack(apparelGraphicRecord.sourceApparel))
				{
					Material material2 = apparelGraphicRecord.graphic.MatAt(bodyFacing, null);
					var parameters = new object[] { material2, ___pawn, flags.FlagSet(PawnRenderFlags.Portrait) };
					material2 = (flags.FlagSet(PawnRenderFlags.Cache) ? material2 : (Material)overrideMaterialIfNeeded.Invoke(__instance, parameters));
					if (apparelGraphicRecord.sourceApparel.def.apparel.wornGraphicData != null)
					{
						Vector2 vector = apparelGraphicRecord.sourceApparel.def.apparel.wornGraphicData.BeltOffsetAt(bodyFacing, ___pawn.story.bodyType);
						Vector2 vector2 = apparelGraphicRecord.sourceApparel.def.apparel.wornGraphicData.BeltScaleAt(bodyFacing, ___pawn.story.bodyType);
						Matrix4x4 matrix = Matrix4x4.Translate(utilityLoc) * Matrix4x4.Rotate(quaternion) * Matrix4x4.Translate(new Vector3(vector.x, 0f, vector.y)) * Matrix4x4.Scale(new Vector3(vector2.x, 1f, vector2.y));
						GenDraw.DrawMeshNowOrLater(bodyMesh, matrix, material2, flags.FlagSet(PawnRenderFlags.DrawNow));
					}
					else
					{
						GenDraw.DrawMeshNowOrLater(bodyMesh, shellLoc, quaternion, material2, flags.FlagSet(PawnRenderFlags.DrawNow));
					}
				}
			}

			return false;
		}
	}


	[HarmonyPatch(typeof(PawnRenderer), "DrawHeadHair")]
	public static class DrawHeadHair
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			Log.Message($"[TKS_MasksWithHats] DrawHeadHair Transpiler beginning");

			var type = AccessTools.TypeByName("Verse.Log");

			Dictionary<string, MethodInfo> methods = new Dictionary<string, MethodInfo>();

			//var fieldList = AccessTools.GetFieldNames(type);
			var methodList = AccessTools.GetDeclaredMethods(type);
			foreach (var method in methodList)
			{
				if (method.Name == "Message")
				{
					methods[method.Name] = method;
					break;
				}
			}

			var debugMessage = methods["Message"];

			bool foundFirstOverhead = false;
			bool foundIfStatement = false;
			int statementStart = 0;
			int statementEnd = 0;
			bool insertedCode = false;

			var codes = new List<CodeInstruction>(instructions);
			for (int i = 0; i < codes.Count; i++)
			{
				bool yieldIt = true;

				if (!(codes[i].operand is null) && codes[i].operand.ToString().Contains("Overhead") && !foundIfStatement) //                  codes[i].operand == hatsOnlyOnMap (from GetGetMethod)
				{

					if (foundFirstOverhead)
					{
						Log.Message("[TKS_MasksWithHats]found if statement for copy");
						statementStart = i - 5;
						statementEnd = i + 3;

						foundIfStatement = true;
					}
					else
					{
						foundFirstOverhead = true;
					}

				}
				if (foundIfStatement && codes[i].opcode == OpCodes.Callvirt && !insertedCode)
				{
					Log.Message("[TKS_MasksWithHats]Inserting check for FaceCover at line " + i.ToString());

					for (int x = statementStart; x <= statementEnd; x++)
					{
						CodeInstruction statement = codes[x];

						if (!(statement.operand is null) && statement.operand.ToString().Contains("Overhead"))
						{
							FieldInfo faceCover = AccessTools.Field(typeof(TKS_ApparelLayerDefOf), nameof(TKS_ApparelLayerDefOf.FaceCover));
							CodeInstruction replacer = new CodeInstruction(OpCodes.Ldsfld, faceCover);

							yield return replacer;
						}
						else
						{
							yield return statement;
						}

						insertedCode = true;
					}
				}

				if (yieldIt)
				{
					yield return codes[i];
				}

			}
			Log.Message($"[TKS_MasksWithHats] DrawHeadHair Transpiler succeeded");

		}
	}

#endif
	[HarmonyPatch]
	public class PairOverlapsAnything
	{

		static System.Reflection.MethodBase TargetMethod()
        {
			return AccessTools.Method(typeof(PawnApparelGenerator).GetNestedType("PossibleApparelSet", BindingFlags.NonPublic | BindingFlags.Instance), "PairOverlapsAnything");

		}
		
		[HarmonyPrefix]
		public static bool PairOverlapsAnything_Prefix(List<ThingStuffPair> ___aps, HashSet<ApparelUtility.LayerGroupPair> ___lgps, BodyDef ___body, ThingStuffPair pair, ref bool __result)
		{
			if (!___lgps.Any<ApparelUtility.LayerGroupPair>())
			{
				return false;
			}

			
			BodyPartGroupDef faceCover = TKS_BodyPartGroupDefOf.FaceCover;

			if (pair.thing.apparel.bodyPartGroups.Contains(faceCover)) {

				bool overlaps = false;
				foreach (ThingStuffPair otherApparel in ___aps)
				{
					if (otherApparel != pair)
					{
						if (!ApparelUtility.CanWearTogether(DefDatabase<ThingDef>.GetNamed(pair.thing.defName), DefDatabase<ThingDef>.GetNamed(pair.thing.defName), ___body))
						{
							overlaps = true;
						}
					}
				}
				__result = overlaps;
				return false;
			}
			else
			{

				return true;
			}
		}
	}

#if v1_4
	[HarmonyPatch(typeof(PawnGraphicSet), "MatsBodyBaseAt")]
	public static class MatsBodyBaseAt
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			Log.Message($"[TKS_MasksWithHats] MatsBodyBaseAt Transpiler beginning");

			var type = AccessTools.TypeByName("Verse.Log");

			Dictionary<string, MethodInfo> methods = new Dictionary<string, MethodInfo>();

			//var fieldList = AccessTools.GetFieldNames(type);
			var methodList = AccessTools.GetDeclaredMethods(type);
			foreach (var method in methodList)
			{
				if (method.Name == "Message")
				{
					methods[method.Name] = method;
					break;
				}
			}

			var debugMessage = methods["Message"];

			bool foundIfStatement = false;
			int statementStart = 0;
			int statementEnd = 0;

			var codes = new List<CodeInstruction>(instructions);
			for (int i = 0; i < codes.Count; i++)
			{
				bool yieldIt = true;

				if (!(codes[i].operand is null) && codes[i].operand.ToString().Contains("Overhead") && !foundIfStatement) //                  codes[i].operand == hatsOnlyOnMap (from GetGetMethod)
				{

					Log.Message("found if statement for copy");
					statementStart = i + 1;
					statementEnd = i + 10;

					foundIfStatement = true;

					yield return codes[i];
					yieldIt = false;

					for (int x = statementStart; x<=statementEnd; x++)
                    {
						CodeInstruction statement = codes[x];

						if (!(statement.operand is null) && statement.operand.ToString().Contains("EyeCover"))
						{
							FieldInfo faceCover = AccessTools.Field(typeof(TKS_ApparelLayerDefOf), nameof(TKS_ApparelLayerDefOf.FaceCover));
							CodeInstruction replacer = new CodeInstruction(OpCodes.Ldsfld, faceCover);

							yield return replacer;
						}
						else
						{
							yield return statement;
						}
					}

				}
				if (yieldIt)
				{
					yield return codes[i];
				}

			}
			Log.Message($"[TKS_MasksWithHats] MatsBodyBaseAt Transpiler succeeded");

		}
	}
#endif
	//handle show hair under hats
	[HarmonyPatch]
	static class ShowHair_Patch
	{
		static MethodBase target;

		static Type type;

		static bool Prepare()
		{
			var mod = LoadedModManager.RunningMods.FirstOrDefault(m => m.PackageId == "cat2002.showhair");
			if (mod == null)
			{
				Log.Message("[TKS_MasksWithHats] can't patch Show Hair With Hats or Hide All Hats, can't find mod");
				return false;
			}

			type = mod.assemblies.loadedAssemblies
				.FirstOrDefault(a => a.GetName().Name == "ShowHair")?
				.GetType("ShowHair.Settings");

			if (type == null)
			{
				Log.Message("[TKS_MasksWithHats] can't patch Show Hair With Hats or Hide All Hats, can't find Settings");

				return false;
			}

			target = AccessTools.DeclaredMethod(type, "IsHeadwear");

			if (target == null)
			{
				Log.Message("[TKS_MasksWithHats] can't patch Show Hair With Hats or Hide All Hats, can't find Settings.IsHeadwear");

				return false;
			}

			Log.Message("[TKS_MasksWithHats] patched Show Hair With Hats or Hide All Hats");
			return true;
		}

		static MethodBase TargetMethod()
		{
			return target;
		}

		[HarmonyPrefix]
		public static bool IsHeadwear_Prefix(ApparelProperties apparelProperties, ref bool __result)
		{
			if (apparelProperties == null)
			{
				__result = false;
				return false;
			}

			if (apparelProperties.LastLayer == TKS_ApparelLayerDefOf.FaceCover)
			{
				__result = true;
				return false;
			}

			return true;
		}
	}


	//handle vanilla apparel expanded
	[HarmonyPatch]
	static class Need_Anonymity_Patch
    {
		static MethodBase target;

		static Type type;

		static bool Prepare()
        {
			var mod = LoadedModManager.RunningMods.FirstOrDefault(m => m.Name == "Vanilla Ideology Expanded - Memes and Structures");
			if (mod == null)
            {
				return false;
            }

			type = mod.assemblies.loadedAssemblies
				.FirstOrDefault(a => a.GetName().Name == "VanillaMemesExpanded")?
				.GetType("VanillaMemesExpanded.Need_Anonymity");

			if (type == null)
            {
				Log.Message("[TKS_MasksWithHats] can't patch Vanilla Ideology Memes, can't find Need_Anonymity");

				return false;
            }

			target = AccessTools.DeclaredMethod(type, "NeedInterval");

			if (target == null)
            {
				Log.Warning("[TKS_MasksWithHats] can't patch Vanilla Ideology Memes, can't find NeedInterval method");

				return false;
			}

			return true;
        }

		static MethodBase TargetMethod()
        {
			return target;
        }

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var codes = new List<CodeInstruction>(instructions);

			bool foundIfStatement = false;
			int statementStart = 0;
			int statementEnd = 0;

			for (int i = 0; i < codes.Count; i++)
            {
				bool yieldIt = true;

				if (!(codes[i].operand is null) && codes[i].operand.ToString().Contains("FullHead") && !foundIfStatement) //                  codes[i].operand == hatsOnlyOnMap (from GetGetMethod)
				{

					Log.Message("[TKS_MasksWithHats] found if statement to replace with FaceCover");
					statementStart = i + 1;
					statementEnd = i + 10;

					foundIfStatement = true;
					yieldIt = false;

					FieldInfo faceCover = AccessTools.Field(typeof(TKS_BodyPartGroupDefOf), nameof(TKS_BodyPartGroupDefOf.FaceCover));
					CodeInstruction replacer = new CodeInstruction(OpCodes.Ldsfld, faceCover);

					yield return replacer;
				}


				if (yieldIt)
				{
					yield return codes[i];
				}

			}
			Log.Message($"[TKS_MasksWithHats] Vanilla Ideology Expanded Transpiler succeeded");
		}
	}
}