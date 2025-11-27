// Source: vs/editor/common/model/textModel.ts
// - Decoration owner ID constants
// Ported: 2025-11-22
// Updated: 2025-11-28 (CL8-Phase1: ownerId=0 now means "no filter", matching TS behavior)

using System.Runtime.CompilerServices;

namespace PieceTree.TextBuffer.Decorations;

public static class DecorationOwnerIds
{
    /// <summary>
    /// When passed as filter, matches all decorations (equivalent to TS passing 0 or undefined).
    /// Also used as the default owner for global decorations visible to all editors.
    /// </summary>
    public const int Any = 0;

    /// <summary>
    /// Reserved owner ID for search highlight decorations.
    /// </summary>
    public const int SearchHighlights = 1;

    /// <summary>
    /// Start of allocatable owner IDs. Internal use only.
    /// AllocateDecorationOwnerId() returns values starting from this.
    /// </summary>
    internal const int FirstAllocatableOwnerId = 2;

    /// <summary>
    /// Check if the filter matches all owners (no filtering).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool FiltersAllOwners(int ownerFilter) => ownerFilter == Any;

    /// <summary>
    /// Owner IDs &lt;= 0 are global (shared) decorations that are always visible regardless of filters.
    /// Matches VS Code semantics where tracked ranges and default decorations use ownerId 0.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsGlobalOwner(int ownerId) => ownerId <= Any;

    /// <summary>
    /// Check if a decoration with the given ownerId matches the filter.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MatchesFilter(int ownerFilter, int ownerId)
    {
        if (FiltersAllOwners(ownerFilter))
        {
            return true;
        }

        if (IsGlobalOwner(ownerId))
        {
            // Global/implicit owners (0 or negative) always match owner-specific queries.
            return true;
        }

        return ownerId == ownerFilter;
    }
}
