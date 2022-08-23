using RimWorld;
using Verse;

namespace TKS_MasksWithHats
{
    [DefOf]
    public static class TKS_ApparelLayerDefOf
    {
        public static ApparelLayerDef FaceCover;

        static TKS_ApparelLayerDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ApparelLayerDefOf));
        }
    }
}
