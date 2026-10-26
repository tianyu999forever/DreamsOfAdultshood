using System;
using System.Collections.Generic;

// Pure scoring rules, independent of the shared GameManager and other missions.
public static class mission3Rules_Manager_ShengChiehLee
{
    public static readonly string[] Items = { "Brass Compass", "Coastal Postcard", "Pocket Metronome", "Vinyl Record", "Seed Envelope", "Pressed Fern" };
    public static readonly string[] Guests = { "301 / The Traveller", "302 / The Musician", "303 / The Botanist" };
    public static readonly string[] Notes = {
        "301 / THE TRAVELLER\nI left something that points north and a memory of the coast. Please return both before my next journey.",
        "302 / THE MUSICIAN\nMy practice needs a steady beat. My evenings need the sound of a record to lull me to sleep. Two small things make a room feel like home.",
        "303 / THE BOTANIST\nOne belongs to a future garden. The other is a leaf I preserved from the past. Please keep them together for me please."
    };
    public static int Owner(int item) { if (item < 0 || item >= 6) throw new ArgumentOutOfRangeException(nameof(item)); return item / 2; }
    public struct Result { public int score, correct, sets, collected, itemPoints, setPoints, explorationPoints; }
    public static Result Settle(IReadOnlyList<bool> collected, IReadOnlyList<int> assignment, bool shortcut)
    {
        if (collected.Count != 6 || assignment.Count != 6) throw new ArgumentException("Six unique lost-property slots are required.");
        Result r = new Result();
        for (int i = 0; i < 6; i++) { if (!collected[i]) continue; r.collected++; if (assignment[i] == Owner(i)) r.correct++; }
        for (int g = 0; g < 3; g++) if (collected[g*2] && collected[g*2+1] && assignment[g*2] == g && assignment[g*2+1] == g) r.sets++;
        r.itemPoints = r.correct * 10; r.setPoints = r.sets * 10; r.explorationPoints = shortcut ? 10 : 0;
        r.score = r.itemPoints + r.setPoints + r.explorationPoints;
        return r;
    }
}
