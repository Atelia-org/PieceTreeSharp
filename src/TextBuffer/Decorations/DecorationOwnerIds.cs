// Source: vs/editor/common/model/textModel.ts
// - Decoration owner ID constants
// Ported: 2025-11-22

namespace PieceTree.TextBuffer.Decorations;

public static class DecorationOwnerIds
{
    public const int Default = 0;
    public const int SearchHighlights = 1;
    public const int Any = -1;

    public static bool FiltersAllOwners(int ownerFilter)
        => ownerFilter == Any || ownerFilter == Default;

    public static bool MatchesFilter(int ownerFilter, int ownerId)
    {
        if (FiltersAllOwners(ownerFilter))
        {
            return true;
        }

        if (ownerId == Default)
        {
            // Global decorations are visible to every owner
            return true;
        }

        return ownerId == ownerFilter;
    }
}
