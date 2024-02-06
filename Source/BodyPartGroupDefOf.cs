using RimWorld;
using Verse;

namespace TKS_MasksWithHats
{
    [DefOf]
    public static class TKS_BodyPartGroupDefOf
    {
        static TKS_BodyPartGroupDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(BodyPartGroupDefOf));
        }

        public static BodyPartGroupDef FaceCover;
    }
}
