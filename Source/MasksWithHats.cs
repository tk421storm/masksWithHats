using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using System.Linq;

[StaticConstructorOnStartup]
public static class TKS_MasksWithHats_C
{
	static TKS_MasksWithHats_C()
	{
		Harmony harmony = new Harmony("MasksWithHats");
		Harmony.DEBUG = true;
		harmony.PatchAll();
		Harmony.DEBUG = false;
		Log.Message($"MasksWithHats: Patching finished");
	}
}

[HarmonyPatch(typeof(ApparelUtility), "CanWearTogether")]
public static class CanWearTogether_IncludeFacecover
{
	static bool Prefix(ThingDef A, ThingDef B, BodyDef body, ref bool __result)
    {
		ApparelProperties aProps = A.apparel;
		ApparelProperties bProps = B.apparel;

		BodyPartGroupDef faceCover = TKS_MasksWithHats.BodyPartGroupDefOf.FaceCover;
		List<BodyPartRecord> faceCoverIncludes = new List<BodyPartRecord>(from x in body.AllParts where x.depth == BodyPartDepth.Outside && x.groups.Contains(faceCover) select x);


		if (!aProps.bodyPartGroups.Contains(faceCover) && !bProps.bodyPartGroups.Contains(faceCover)) {
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


public static class Include_FaceCover
{
	[HarmonyPatch(typeof(ApparelGraphicRecordGetter), "TryGetGraphicApparel")]
	[HarmonyPrefix]
	static bool Prefix_TryGetGraphicApparel(Apparel apparel, BodyTypeDef bodyType, ref ApparelGraphicRecord rec)
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
		if (apparel.def.apparel.LastLayer == TKS_MasksWithHats.ApparelLayerDefOf.FaceCover)
		{
			Log.Message("Getting apparel graphic for FaceMask object " + apparel.def.defName);
			path = apparel.WornGraphicPath;
			Shader shader = ShaderDatabase.Cutout;
			if (apparel.def.apparel.useWornGraphicMask)
			{
				shader = ShaderDatabase.CutoutComplex;
			}
			Graphic graphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, apparel.def.graphicData.drawSize, apparel.DrawColor);
			rec = new ApparelGraphicRecord(graphic, apparel);
			return false;
		}

		return true;
    }
	/*
	[HarmonyPatch(typeof(PawnRenderer), "DrawBodyApparel")]
	static bool DrawBodyApparel_Prefix(PawnRenderer __instance, Vector3 shellLoc, Vector3 utilityLoc, Mesh bodyMesh, float angle, Rot4 bodyFacing, PawnRenderFlags flags)
    {
		List<ApparelGraphicRecord> apparelGraphics = __instance?.graphics?.apparelGraphics;
		if (apparelGraphics  != null) {
			//check for nulls inside too
			if (!apparelGraphics.Any()) {
				Log.Warning("passed apparel graphics list contains nulls!");
				return true;
            }
			Log.Message("drawing pawn apparel with layers: " + apparelGraphics.ToStringSafeEnumerable());
		};
		return true;
    }

	[HarmonyPatch(typeof(PawnRenderer), "DrawPawnBody")]
	static bool DrawPawnBody(Vector3 rootLoc, float angle, Rot4 facing, RotDrawMode bodyDrawType, PawnRenderFlags flags, Mesh bodyMesh, List<object> __args)
    {
		foreach (object item in __args)
        {
			if (item is null)
            {
				Log.Warning("found null in DrawPawnBody args, this will probably fail!");
            }
        }

		return true;
    }
	*/
	
}