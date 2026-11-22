using UnityEngine;

/// <summary>
/// A reward card offered between waves. Assigns a persistent gameplay effect to the player.
/// </summary>
[CreateAssetMenu(fileName = "WaveRewardCard", menuName = "ScriptableObjects/Waves/RewardCard", order = 20)] 
public class WaveRewardEffectCardSO : ScriptableObject
{
    [Tooltip("Display name shown in the reward UI.")]
    public string cardName;

    [Tooltip("Short description of what the effect does.")]
    public string description;

    [Tooltip("The gameplay effect applied to the player when this card is chosen.")]
    public GameplayEffectSO effect;

    [Tooltip("Optional icon for the UI.")]
    public Sprite icon;
}
