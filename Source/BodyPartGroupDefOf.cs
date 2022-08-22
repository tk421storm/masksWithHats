using RimWorld;
using Verse;

namespace TKS_MasksWithHats
{
    [DefOf]
    class BodyPartGroupDefOf
    {
        static BodyPartGroupDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(BodyPartGroupDefOf));
        }

        public static BodyPartGroupDef FaceCover;
    }
}
