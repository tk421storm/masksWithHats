using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using System.Linq;
using System.Reflection;

namespace TKS_MasksWithHats
{
	[StaticConstructorOnStartup]
	public static class InsertHarmony
	{
		static InsertHarmony()
		{
			Harmony harmony = new Harmony("TKS_MasksWithHats");
			Harmony.DEBUG = true;
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
									Log.Message("Not allowing apparel " + A.defName.ToString() + " with " + B.defName.ToString() + " because faceCover already covers " + bpr.Label);
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
	/*
	[HarmonyPatch(typeof(PawnRenderer))]
	static class DrawBodyApparel
	{

		[HarmonyPatch(typeof(PawnRenderer), "DrawBodyApparel")]
		[HarmonyPrefix]
		static bool DrawBodyApparel_Prefix(PawnRenderer __instance, Pawn ___pawn, Vector3 shellLoc, Vector3 utilityLoc, Mesh bodyMesh, float angle, Rot4 bodyFacing, PawnRenderFlags flags)
		{
			if (!___pawn.RaceProps.Humanlike)
            {
				return true;
            }

			List<ApparelGraphicRecord> apparelGraphics = __instance?.graphics?.apparelGraphics;
			if (apparelGraphics != null)
			{
				string apparelString = "";

				foreach (ApparelGraphicRecord? apparelGraphicsRecord in apparelGraphics)
                {
					if (apparelGraphicsRecord is null)
                    {
						apparelString += "null, ";
                    }
					else
                    {
						ApparelGraphicRecord apparel = (ApparelGraphicRecord)apparelGraphicsRecord;
						apparelString += apparel.sourceApparel.def.defName + ", ";

                    }
                }
				Log.Message("drawing pawn " + ___pawn.Name + " apparel with apparel: " + apparelString);
				//return true;
				
			};

			Dictionary<string, object> args = new Dictionary<string, object>();
			args.Add("shellLoc", shellLoc);
			args.Add("utilityLoc", utilityLoc);
			args.Add("bodyFacing", bodyFacing);
			args.Add("angle", angle);
			args.Add("flags", flags);
			args.Add("bodyMesh", bodyMesh);

			foreach (KeyValuePair<string, object> item in args)
			{
				if (item.Value is null)
				{
					Log.Warning("found null in DrawBodyApparel: " + item.Key + ", this will probably fail!");
				}
			}
			//Log.Message("DrawBodyApparel recieved non-null arguments ok");

			MethodInfo overrideMaterialIfNeeded = __instance.GetType().GetMethod("OverrideMaterialIfNeeded", BindingFlags.NonPublic | BindingFlags.Instance);

			Quaternion quaternion = Quaternion.AngleAxis(angle, Vector3.up);
			for (int i = 0; i < apparelGraphics.Count; i++)
			{
				ApparelGraphicRecord apparelGraphicRecord = apparelGraphics[i];
				Log.Message("Drawing graphics for apparelGraphic " + apparelGraphicRecord.ToString());
				if (apparelGraphicRecord.sourceApparel.def.apparel.LastLayer == ApparelLayerDefOf.Shell && !apparelGraphicRecord.sourceApparel.def.apparel.shellRenderedBehindHead)
				{
					Material material = apparelGraphicRecord.graphic.MatAt(bodyFacing, null);
					var parameters = new object[] { material, ___pawn, flags.FlagSet(PawnRenderFlags.Portrait) };
					material = (flags.FlagSet(PawnRenderFlags.Cache) ? material : overrideMaterialIfNeeded.Invoke(__instance, parameters) as Material);
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
					material2 = (flags.FlagSet(PawnRenderFlags.Cache) ? material2 : overrideMaterialIfNeeded.Invoke(__instance, parameters) as Material);
					if (apparelGraphicRecord.sourceApparel.def.apparel.wornGraphicData != null)
					{
						Vector2 vector = apparelGraphicRecord.sourceApparel.def.apparel.wornGraphicData.BeltOffsetAt(bodyFacing, ___pawn.story.bodyType);
						Vector2 vector2 = apparelGraphicRecord.sourceApparel.def.apparel.wornGraphicData.BeltScaleAt(___pawn.story.bodyType);
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
	
	[HarmonyPatch(typeof(PawnRenderer))]
	static class DrawPawnBody 
	{
		[HarmonyPatch(typeof(PawnRenderer), "DrawPawnBody")]
		[HarmonyPrefix]
		static bool DrawPawnBody_Prefix(Vector3 rootLoc, float angle, Rot4 facing, RotDrawMode bodyDrawType, PawnRenderFlags flags, Mesh bodyMesh)
		{
			Dictionary<string, object> args = new Dictionary<string, object>();
			args.Add("rootLoc", rootLoc);
			args.Add("angle", angle);
			args.Add("bodyDrawType", bodyDrawType);
			args.Add("flags", flags);
			//args.Add("bodyMesh", bodyMesh);

			foreach (KeyValuePair<string, object> item in args)
			{
				if (item.Value is null)
				{
					Log.Warning("found null in DrawPawnBody: "+item.Key+", this will probably fail!");
				}
			}
			//Log.Message("DrawPawnBody recieved non-null arguments ok");
			return true;
		}
	}
	*/
	[HarmonyPatch(typeof(PawnApparelGenerator))]
	static class IsHeadgear
    {
		[HarmonyPatch(typeof(PawnApparelGenerator), "IsHeadgear")]
		[HarmonyPrefix]
		static bool IsHeadgear_prefix(ThingDef td, ref bool __result)
        {
			BodyPartGroupDef faceCover = TKS_MasksWithHats.BodyPartGroupDefOf.FaceCover;

			if (td.apparel.bodyPartGroups.Contains(faceCover)) {
				__result = true;
				return false;
            }
			return true;
        }
    }

	[HarmonyPatch(typeof(PawnRenderer), "CanUseTargetWorker")]
	public static class Debug_Patch
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			bool transpilersucceeded = false;

			Log.Message($"[TKSDebug] Debug_Patch Transpiler beginning");

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
			
			ParameterInfo[] parameterInfos = debugMessage.GetParameters();

			int length = parameterInfos.Length;

			List<object> defaultArgs = new List<object>(length);

			for (int x = 0; x < length; x++)
			{
				defaultArgs.Add(parameterInfos[x].DefaultValue);
			}
			

			//list of variables to report
			List<object> opcodes = new List<object>();
			opcodes.Add(OpCodes.Ldarg_0);
			opcodes.Add(OpCodes.Ldarg_1);
			opcodes.Add(OpCodes.Ldarg_2);

			var codes = new List<CodeInstruction>(instructions);
			for (int i = 0; i < codes.Count; i++)
			{
				bool yieldIt = true;

				//ADD DEBUG OF VARIABLES
				if (opcodes.Contains(codes[i].opcode))
                {
					string variable = codes[i].operand.ToString();
					string message = "Function called with " + variable;

					yield return codes[i];
					yieldIt = false;

					yield return new CodeInstruction(OpCodes.Ldstr, message);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Call, debugMessage);

				}

				// ADD DEBUG TO ALL CALLS
				
				if (codes[i].opcode == OpCodes.Call) //                  codes[i].operand == hatsOnlyOnMap (from GetGetMethod)
				{
					string caller = codes[i].operand.ToString();
					string message = "Succesfully found call " + caller;
					Log.Message(message);

					yield return codes[i];
					yieldIt = false;

					string debugMsg = "Succesfully ran call " + caller;

					//defaultArgs[0] = debugMsg;
					
					//foreach (var argument in defaultArgs)
                    //{
					//	yield return new CodeInstruction().LoadField(typeof(argument), );

					//}
					
					yield return new CodeInstruction(OpCodes.Ldstr, debugMsg);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Call, debugMessage);

					transpilersucceeded = true;
				}
				
				if (yieldIt)
                {
					yield return codes[i];
				}
				
			}
			Log.Message($"[TKSDebug] Debug_Patch Transpiler succeeded");

		}
	}
}