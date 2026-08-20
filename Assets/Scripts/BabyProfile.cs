using UnityEngine;

/// <summary>
/// Data container for a single baby the player might be searching for.
/// Create one asset per possible baby: right-click in Project window ->
/// Create -> Diggs -> Baby Profile. With "maximum of 3 kids" you'll make
/// 3 of these (e.g. Baby_Alex, Baby_Sam, Baby_Jordan) and drag all 3 into
/// GameManager's "Baby Pool" array in the Inspector.
///
/// IMPORTANT: displayName/portrait are what the player sees when picking
/// WHICH baby is theirs (the guess screen). The other fields are what
/// get revealed piece-by-piece as clue text during play — the player
/// should NOT see displayName/gender/etc. directly, only the sentence
/// built from them by ClueManager.BuildClueText().
/// </summary>
[CreateAssetMenu(fileName = "NewBabyProfile", menuName = "Diggs/Baby Profile")]
public class BabyProfile : ScriptableObject
{
    public enum Gender { Boy, Girl }

    [Header("Identity (shown on the Guess screen)")]
    public string displayName = "Baby A";
    public Sprite portrait;

    [Header("Clue Attributes (revealed one at a time during play)")]
    public Gender gender = Gender.Boy;

    [Tooltip("e.g. \"light\", \"medium\", \"dark\" - whatever wording you want the clue text to use")]
    public string skinTone = "light";

    [Tooltip("An item they carry, e.g. \"a stuffed rabbit\"")]
    public string accessory = "a stuffed rabbit";

    [Tooltip("Name of the primary color they're wearing, e.g. \"yellow\"")]
    public string primaryClothingColor = "yellow";

    [Tooltip("Where they were last seen, e.g. \"the playground near the swings\"")]
    public string lastSeenLocation = "the playground near the swings";
}
