using System.Collections.Generic;

/// <summary>
/// Central registry for PlayerPrefs progress keys.
/// Keep all "chapter done" / gallery unlock keys here so adding new routes
/// doesn't require hunting through unrelated code.
/// </summary>
public static class ProgressKeys
{
    public static readonly string PlayerName = "PlayerName";

    // Continue/checkpoint tracking
    public static readonly string LastCompletedCheckpointKey = "LastCompletedCheckpointKey";
    public static readonly string LastCompletedCheckpointAtUtcTicks = "LastCompletedCheckpointAtUtcTicks";
    public static readonly string CheckpointCompletedAtPrefix = "CheckpointCompletedAt_";

    public static string GetCheckpointCompletedAtKey(string chapterKey)
    {
        return CheckpointCompletedAtPrefix + chapterKey;
    }

    public static readonly string[] GalleryUnlockKeys =
    {
        "A1", "A2", "A3", "A4", "A5", "A6",
    };

    public static readonly string[] GlobalChapterKeys =
    {
        "Chapter1", "Chapter2", "Chapter3", "Chapter4", "Chapter5", "Chapter6",
        "TheEnd",
    };

    public static readonly string[] AlephEpilogueKeys =
    {
        "AEpilogue1", "AEpilogue2", "AEpilogue3",
    };

    public static readonly string[] ScarletEpilogueKeys =
    {
        "SEpilogue1", "SEpilogue2", "SEpilogue3",
    };

    /// <summary>
    /// All keys that represent progress and should be deleted for a full reset.
    /// Safe to include keys that don't exist yet; PlayerPrefs.DeleteKey is a no-op.
    /// </summary>
    public static readonly string[] AllProgressKeys = BuildAllProgressKeys();

    private static string[] BuildAllProgressKeys()
    {
        var keys = new List<string>(128);

        keys.Add(LastCompletedCheckpointKey);
        keys.Add(LastCompletedCheckpointAtUtcTicks);

        keys.AddRange(GlobalChapterKeys);
        keys.AddRange(AlephEpilogueKeys);
        keys.AddRange(ScarletEpilogueKeys);
        keys.AddRange(GalleryUnlockKeys);

        // Route chapter completion keys.
        // Existing route: Aleph (A)
        AddRouteChapterKeys(keys, "A", 7, 15);

        // Planned/added routes: Scarlet (S) and MrHorse (H)
        // Add your ChapterScene assets with names like SChapter7, HChapter7, etc.
        AddRouteChapterKeys(keys, "S", 7, 15);
        AddRouteChapterKeys(keys, "H", 7, 15);

        // Timestamp keys for all known checkpoint keys (global chapters, epilogues, and route chapters).
        // This allows a full reset to fully clear checkpoint recency across routes.
        var checkpointKeys = new List<string>(64);
        checkpointKeys.AddRange(GlobalChapterKeys);
        checkpointKeys.AddRange(AlephEpilogueKeys);
        checkpointKeys.AddRange(ScarletEpilogueKeys);
        AddRouteChapterKeys(checkpointKeys, "A", 7, 15);
        AddRouteChapterKeys(checkpointKeys, "S", 7, 15);
        AddRouteChapterKeys(checkpointKeys, "H", 7, 15);

        foreach (var ck in checkpointKeys)
        {
            keys.Add(GetCheckpointCompletedAtKey(ck));
        }

        return keys.ToArray();
    }

    private static void AddRouteChapterKeys(List<string> keys, string routePrefix, int firstChapter, int lastChapter)
    {
        for (int chapter = firstChapter; chapter <= lastChapter; chapter++)
        {
            keys.Add(routePrefix + "Chapter" + chapter);
        }
    }
}
