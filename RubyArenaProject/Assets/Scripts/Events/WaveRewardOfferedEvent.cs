using System.Collections.Generic;

/// <summary>
/// Raised on every peer when the server decides a wave reward should be offered.
/// The UI listens to this and presents the cards; the player then calls
/// <see cref="WaveRewardApplier.ChooseCard"/> with their selection.
/// </summary>
public class WaveRewardOfferedEvent
{
    /// <summary>
    /// The cards the player may choose from.
    /// </summary>
    public IReadOnlyList<WaveRewardEffectCardSO> Cards;

    /// <summary>
    /// The wave number that triggered this reward.
    /// </summary>
    public int WaveNumber;
}
