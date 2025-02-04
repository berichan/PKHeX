using System;
using System.Collections.Generic;

namespace PKHeX.Core;

/// <summary>
/// Logic for applying ribbons.
/// </summary>
public static class RibbonApplicator
{
    private static List<string> GetAllRibbonNames(PKM pk) => RibbonInfo.GetRibbonInfo(pk).ConvertAll(z => z.Name);

    /// <summary>
    /// Gets a list of valid ribbons for the <see cref="pk"/>.
    /// </summary>
    /// <param name="pk">Entity to fetch the list for.</param>
    /// <param name="allRibbons">All ribbon names.</param>
    /// <returns>List of all valid ribbon names.</returns>
    public static IReadOnlyList<string> GetValidRibbons(PKM pk, IList<string> allRibbons)
    {
        var clone = pk.Clone();
        return SetAllValidRibbons(allRibbons, clone);
    }

    /// <summary>
    /// Gets a list of valid ribbons for the <see cref="pk"/>.
    /// </summary>
    /// <param name="pk">Entity to fetch the list for.</param>
    /// <returns>List of all valid ribbon names.</returns>
    public static IReadOnlyList<string> GetValidRibbons(PKM pk)
    {
        var names = GetAllRibbonNames(pk);
        return GetValidRibbons(pk, names);
    }

    /// <summary>
    /// Gets a list of valid ribbons for the <see cref="pk"/> that can be removed.
    /// </summary>
    /// <param name="pk">Entity to fetch the list for.</param>
    /// <param name="allRibbons">All ribbon names.</param>
    /// <returns>List of all removable ribbon names.</returns>
    public static IReadOnlyList<string> GetRemovableRibbons(PKM pk, IList<string> allRibbons)
    {
        var clone = pk.Clone();
        return RemoveAllValidRibbons(allRibbons, clone);
    }

    /// <summary>
    /// Gets a list of valid ribbons for the <see cref="pk"/> that can be removed.
    /// </summary>
    /// <param name="pk">Entity to fetch the list for.</param>
    /// <returns>List of all removable ribbon names.</returns>
    public static IReadOnlyList<string> GetRemovableRibbons(PKM pk)
    {
        var names = GetAllRibbonNames(pk);
        return GetRemovableRibbons(pk, names);
    }

    /// <summary>
    /// Sets all valid ribbons to the <see cref="pk"/>.
    /// </summary>
    /// <param name="pk">Entity to set ribbons for.</param>
    /// <returns>True if any ribbons were applied.</returns>
    public static bool SetAllValidRibbons(PKM pk)
    {
        var ribNames = GetAllRibbonNames(pk);
        ribNames.RemoveAll(z => z.StartsWith("RibbonMark", StringComparison.Ordinal)); // until marking legality is handled
        return SetAllValidRibbons(pk, ribNames);
    }

    /// <summary>
    /// Sets all valid ribbons to the <see cref="pk"/>.
    /// </summary>
    /// <param name="pk">Entity to set ribbons for.</param>
    /// <param name="ribNames">Ribbon names to try setting.</param>
    /// <returns>True if any ribbons were applied.</returns>
    public static bool SetAllValidRibbons(PKM pk, List<string> ribNames)
    {
        var list = SetAllValidRibbons(ribNames, pk);
        return list.Count != 0;
    }

    private static IReadOnlyList<string> SetAllValidRibbons(IList<string> allRibbons, PKM pk)
    {
        var la = new LegalityAnalysis(pk);
        var valid = new List<string>();

        while (TryApplyAllRibbons(pk, la, allRibbons, valid) != 0)
        {
            // Repeat the operation until no more ribbons are set.
        }

        // Ribbon Deadlock
        if (pk is IRibbonSetCommon6 c6)
            InvertDeadlockContest(c6, la, true);

        return valid;
    }

    /// <summary>
    /// Sets all valid ribbons to the <see cref="pk"/>.
    /// </summary>
    /// <param name="pk">Entity to set ribbons for.</param>
    /// <returns>True if any ribbons were removed.</returns>
    public static bool RemoveAllValidRibbons(PKM pk)
    {
        var ribNames = GetAllRibbonNames(pk);
        return RemoveAllValidRibbons(pk, ribNames);
    }

    /// <summary>
    /// Sets all valid ribbons to the <see cref="pk"/>.
    /// </summary>
    /// <param name="pk">Entity to set ribbons for.</param>
    /// <param name="ribNames">Ribbon names to try setting.</param>
    /// <returns>True if any ribbons were removed.</returns>
    public static bool RemoveAllValidRibbons(PKM pk, List<string> ribNames)
    {
        var list = RemoveAllValidRibbons(ribNames, pk);
        return list.Count != 0;
    }

    private static IReadOnlyList<string> RemoveAllValidRibbons(IList<string> allRibbons, PKM pk)
    {
        var la = new LegalityAnalysis(pk);
        var valid = new List<string>();

        // Ribbon Deadlock
        if (pk is IRibbonSetCommon6 c6)
            InvertDeadlockContest(c6, la, false);

        while (TryRemoveAllRibbons(pk, la, allRibbons, valid) != 0)
        {
            // Repeat the operation until no more ribbons are set.
        }

        return valid;
    }

    private static int TryApplyAllRibbons(PKM pk, LegalityAnalysis la, IList<string> allRibbons, ICollection<string> valid)
    {
        int applied = 0;
        for (int i = 0; i < allRibbons.Count;)
        {
            la.ResetParse();
            var rib = allRibbons[i];
            var success = TryApplyRibbon(pk, la, rib);
            if (success)
            {
                ++applied;
                allRibbons.RemoveAt(i);
                valid.Add(rib);
            }
            else
            {
                RemoveRibbon(pk, rib);
                ++i;
            }
        }

        return applied;
    }

    private static int TryRemoveAllRibbons(PKM pk, LegalityAnalysis la, IList<string> allRibbons, ICollection<string> valid)
    {
        int removed = 0;
        for (int i = 0; i < allRibbons.Count;)
        {
            la.ResetParse();
            var rib = allRibbons[i];
            var success = TryRemoveRibbon(pk, la, rib);
            if (success)
            {
                ++removed;
                allRibbons.RemoveAt(i);
                valid.Add(rib);
            }
            else
            {
                SetRibbonValue(pk, rib, 1);
                ++i;
            }
        }

        return removed;
    }

    private static void RemoveRibbon(PKM pk, string rib) => SetRibbonValue(pk, rib, 0);

    private static bool TryRemoveRibbon(PKM pk, LegalityAnalysis la, string rib)
    {
        RemoveRibbon(pk, rib);
        return UpdateIsValid(la);
    }

    private static bool TryApplyRibbon(PKM pk, LegalityAnalysis la, string rib)
    {
        SetRibbonValue(pk, rib, 1);
        return UpdateIsValid(la);
    }

    private static bool UpdateIsValid(LegalityAnalysis la)
    {
        LegalityAnalyzers.Ribbon.Verify(la);
        foreach (var p in la.Results)
        {
            if (!p.Valid)
                return false;
        }
        return true;
    }

    private static void SetRibbonValue(PKM pk, string rib, int value)
    {
        switch (rib)
        {
            case nameof(PK7.RibbonCountMemoryBattle):
                ReflectUtil.SetValue(pk, rib, value * (pk.Gen4 ? 6 : 8));
                break;
            case nameof(PK7.RibbonCountMemoryContest):
                ReflectUtil.SetValue(pk, rib, value * (pk.Gen4 ? 20 : 40));
                break;
            default:
                if (rib.StartsWith("RibbonCountG3", StringComparison.Ordinal))
                    ReflectUtil.SetValue(pk, rib, value * 4);
                else
                    ReflectUtil.SetValue(pk, rib, value != 0);
                break;
        }
    }

    private static void InvertDeadlockContest(IRibbonSetCommon6 c6, LegalityAnalysis la, bool desiredState)
    {
        // RibbonContestStar depends on having all contest ribbons, and having RibbonContestStar requires all.
        // Since the above logic sets individual ribbons, we must try setting this deadlock pair manually.
        if (c6.RibbonMasterToughness == desiredState || c6.RibbonContestStar == desiredState)
            return;

        la.ResetParse();
        c6.RibbonMasterToughness = c6.RibbonContestStar = desiredState;
        bool result = UpdateIsValid(la);
        if (!result)
            c6.RibbonMasterToughness = c6.RibbonContestStar = !desiredState;
    }
}
