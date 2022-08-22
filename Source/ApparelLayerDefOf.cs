using RimWorld;
using Verse;

namespace TKS_MasksWithHats
{
    [DefOf]
    public static class ApparelLayerDefOf
    {
        public static ApparelLayerDef FaceCover;

        static ApparelLayerDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ApparelLayerDefOf));
        }
    }
}
