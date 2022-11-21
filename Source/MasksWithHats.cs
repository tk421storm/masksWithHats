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
					Log.Message("MasksWithHats found no patches for " + original.Name);
					continue;
				};

				Log.Message("MasksWithHats found patches for " + original.Name + ":");
				foreach (var patch in patches.Prefixes)
				{
					Log.Message("index: " + patch.index);
					Log.Message("owner: " + patch.owner);
					Log.Message("patch method: " + patch.PatchMethod);
					Log.Message("priority: " + patch.priority);
					Log.Message("before: " + patch.before);
					Log.Message("after: " + patch.after);
				}
			}

			harmony.PatchAll();

			//Harmony.DEBUG = false;
			Log.Message($"MasksWithHats: Patching finished");
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

			BodyPartGroupDef faceCover = TKS_MasksWithHats.BodyPartGroupDefOf.FaceCover;
			List<BodyPartRecord> faceCoverIncludes = new List<BodyPartRecord>(from x in body.AllParts where x.depth == BodyPartDepth.Outside && x.groups.Contains(faceCover) select x);


			if (!aProps.bodyPartGroups.Contains(faceCover) && !bProps.bodyPartGroups.Contains(faceCover))
			{
				//Log.Message("not checking body parts on " + A.defName + " or " + B.defName + " since neither include faceCover");
				return true;
			}

			if (aProps.bodyPartGroups.Contains(faceCover) && bProps.bodyPartGroups.Contains(faceCover))
			{
				//Log.Message("not allowing "+A.defName+" with "+B.defName+" because they are both facecovers!")
				__result = false;
				return false;
			}

			if (bProps.bodyPartGroups.Contains(faceCover))
			{
				//swap em
				ThingDef C = A;
				A = B;
				B = C;
			}
			//Log.Message("Checking if " + A.defName + " can be worn with " + B.defName);
			aProps = A.apparel;
			bProps = B.apparel;
			//Log.Message(A.defName + "has the following bodyPartGroups: " + aProps.bodyPartGroups.ToStringSafeEnumerable());
			foreach (BodyPartGroupDef group in aProps.bodyPartGroups)
			{
				//Log.Message("Checking body part group " + group.defName);
				if (group.defName == "FaceCover")
				{
					//check that other apparel does not include bits that FaceCover includes
					//Log.Message("faceCoverIncludes: " + faceCoverIncludes.ToStringSafeEnumerable());

					foreach (BodyPartGroupDef groupDef in bProps.bodyPartGroups)
					{
						List<BodyPartRecord> otherIncludes = new List<BodyPartRecord>(from x in body.AllParts where x.depth == BodyPartDepth.Outside && x.groups.Contains(groupDef) select x);
						foreach (BodyPartRecord bpr in otherIncludes)
						{
							if (bpr.Label != "head")
							{
								if (faceCoverIncludes.Contains(bpr))
								{
									//Log.Message("Not allowing apparel " + A.defName.ToString() + " with " + B.defName.ToString() + " because faceCover already covers " + bpr.Label);
									__result = false;
									return false;
								}
							}
						}
						//Log.Message("Allowing apparel " + A.defName.ToString() + " beacuse list " + aProps.GetCoveredOuterPartsString(body) + " doesn't include any of " + bProps.GetCoveredOuterPartsString(body));
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
				Log.Error("Getting apparel graphic with undefined body type.");
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
				//Log.Message("Getting apparel graphic for FaceMask object " + apparel.def.defName);
				path = apparel.WornGraphicPath;
				Shader shader = ShaderDatabase.Cutout;
				if (apparel.def.apparel.useWornGraphicMask)
				{
					shader = ShaderDatabase.CutoutComplex;
				}
				Graphic graphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, apparel.def.graphicData.drawSize, apparel.DrawColor);
				rec = new ApparelGraphicRecord(graphic, apparel);
				//Log.Message("returning graphic for FaceMask object: " + graphic.ToString());
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
			BodyPartGroupDef faceCover = TKS_MasksWithHats.BodyPartGroupDefOf.FaceCover;

			if (td.apparel.bodyPartGroups.Contains(faceCover))
			{
				__result = true;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(PawnRenderer), "DrawBodyApparel")]
	public static class DrawBodyApparel
	{
		[HarmonyPatch(typeof(PawnRenderer), "DrawBodyApparel")]
		[HarmonyPrefix]
		static bool DrawBodyApparel_Prefix(PawnRenderer __instance, Pawn ___pawn, Vector3 shellLoc, Vector3 utilityLoc, Mesh bodyMesh, float angle, Rot4 bodyFacing, PawnRenderFlags flags)
		{
			//if (bodyMesh is null) { Log.Warning(___pawn.Name+": DrawBodyApparel "+nameof(bodyMesh) + " is null!"); };

			List<ApparelGraphicRecord> apparelGraphics = __instance.graphics.apparelGraphics;

			BodyPartGroupDef faceCover = TKS_MasksWithHats.BodyPartGroupDefOf.FaceCover;
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

			//Log.Message("Overriding DrawBodyApparel for " + ___pawn + " due to facecover ");

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
			Log.Message($"[MasksWithHats] DrawHeadHair Transpiler beginning");

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
						Log.Message("found if statement for copy");
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
					Log.Message("Inserting check for FaceCover at line " + i.ToString());

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
			Log.Message($"[MasksWithHats] DrawHeadHair Transpiler succeeded");

		}
	}

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

			
			BodyPartGroupDef faceCover = TKS_MasksWithHats.BodyPartGroupDefOf.FaceCover;

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

	[HarmonyPatch(typeof(PawnGraphicSet), "MatsBodyBaseAt")]
	public static class MatsBodyBaseAt
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			Log.Message($"[MasksWithHats] MatsBodyBaseAt Transpiler beginning");

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
			Log.Message($"[MasksWithHats] MatsBodyBaseAt Transpiler succeeded");

		}
	}
}