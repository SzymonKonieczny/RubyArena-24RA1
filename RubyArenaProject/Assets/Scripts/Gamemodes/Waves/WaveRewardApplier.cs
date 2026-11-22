using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owned by <see cref="WavesGameModeManager"/>. After every <see cref="wavesPerReward"/>
/// waves, offers the player a choice of random <see cref="WaveRewardEffectCardSO"/>s.
/// The chosen effect is applied to the local player immediately.
///
/// <para>
/// Cards are populated automatically into the <see cref="cardSlots"/>. No separate
/// UI controller is needed — just wire the slots in the inspector.
/// </para>
/// </summary>
public class WaveRewardApplier : MonoBehaviour
{
    [Header("Reward Frequency")]
    [Tooltip("A reward is offered every N waves cleared. Set to 1 for a reward after every wave.")]
    [SerializeField] int wavesPerReward = 3;

    [Tooltip("How many cards the player chooses from.")]
    [SerializeField] int cardsToOffer = 3;

    [Header("UI")]
    [Tooltip("The reward panel GameObject to show/hide while the player is choosing.")]
    [SerializeField] GameObject rewardPanel;

    [Tooltip("Individual card UI slots. At least cardsToOffer slots should be assigned.")]
    [SerializeField] List<WaveRewardCardUI> cardSlots = new();

    [Header("Card Pool")]
    [Tooltip("All possible reward cards. At least cardsToOffer non-null entries are required.")]
    [SerializeField] List<WaveRewardEffectCardSO> cardPool = new();

    /// <summary>
    /// The cards currently on offer, or null if no offer is active.
    /// </summary>
    public IReadOnlyList<WaveRewardEffectCardSO> CurrentOffer { get; private set; }

    /// <summary>
    /// True while a reward is waiting to be claimed.
    /// </summary>
    public bool IsAwaitingChoice => CurrentOffer != null;

    bool cursorWasVisible;
    CursorLockMode previousLockState;

    EventBus<WaveRewardOfferedEvent> rewardOfferedEvent;

    // ------------------------------------------------------------------
    // Init
    // ------------------------------------------------------------------

    public void Initialize(EventBus<WaveRewardOfferedEvent> eventBus)
    {
        rewardOfferedEvent = eventBus;
        if (rewardPanel != null)
            rewardPanel.SetActive(false);
    }

    // ------------------------------------------------------------------
    // Public API for UI
    // ------------------------------------------------------------------

    /// <summary>
    /// Call this from the UI when the player selects a card.
    /// Applies the effect immediately and clears the offer.
    /// </summary>
    /// <param name="chosenIndex">Index into <see cref="CurrentOffer"/>. 0-based.</param>
    public void ChooseCard(int chosenIndex)
    {
        if (!IsAwaitingChoice)
        {
            Debug.LogWarning($"[{nameof(WaveRewardApplier)}] ChooseCard called but no reward is active.");
            return;
        }

        if (chosenIndex < 0 || chosenIndex >= CurrentOffer.Count)
        {
            Debug.LogWarning($"[{nameof(WaveRewardApplier)}] ChooseCard index {chosenIndex} out of range (0..{CurrentOffer.Count - 1}).");
            return;
        }

        WaveRewardEffectCardSO chosenCard = CurrentOffer[chosenIndex];
        CurrentOffer = null;

        HidePanel();
        ApplyEffect(chosenCard);
    }

    // ------------------------------------------------------------------
    // Called by WavesGameModeManager when a wave is cleared
    // ------------------------------------------------------------------

    /// <summary>
    /// Called by WavesGameModeManager when a wave is fully cleared.
    /// Draws random cards and raises an event for the UI.
    /// </summary>
    public void OnWaveCleared(int waveNumber)
    {
        if (cardPool.Count == 0) return;
        if (waveNumber <= 0) return;
        if (waveNumber % wavesPerReward != 0) return;

        List<WaveRewardEffectCardSO> offer = DrawRandomCards();
        if (offer.Count == 0) return;

        CurrentOffer = offer;

        ShowPanel();

        rewardOfferedEvent?.Raise(new WaveRewardOfferedEvent
        {
            Cards = offer,
            WaveNumber = waveNumber,
        });
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    List<WaveRewardEffectCardSO> DrawRandomCards()
    {
        List<WaveRewardEffectCardSO> pool = new(cardPool);
        pool.RemoveAll(c => c == null || c.effect == null);

        int count = Mathf.Min(cardsToOffer, pool.Count);
        List<WaveRewardEffectCardSO> result = new(count);

        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, pool.Count);
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        return result;
    }

    void ShowPanel()
    {
        cursorWasVisible = Cursor.visible;
        previousLockState = Cursor.lockState;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (rewardPanel != null)
            rewardPanel.SetActive(true);

        PopulateSlots();
    }

    void HidePanel()
    {
        Cursor.visible = cursorWasVisible;
        Cursor.lockState = previousLockState;

        if (rewardPanel != null)
            rewardPanel.SetActive(false);

        ClearSlots();
    }

    void PopulateSlots()
    {
        if (CurrentOffer == null) return;

        for (int i = 0; i < cardSlots.Count; i++)
        {
            if (cardSlots[i] == null) continue;

            if (i < CurrentOffer.Count)
                cardSlots[i].SetCard(CurrentOffer[i], i, ChooseCard);
            else
                cardSlots[i].Clear();
        }
    }

    void ClearSlots()
    {
        for (int i = 0; i < cardSlots.Count; i++)
        {
            cardSlots[i]?.Clear();
        }
    }

    void ApplyEffect(WaveRewardEffectCardSO card)
    {
        if (card?.effect == null)
        {
            Debug.LogWarning($"[{nameof(WaveRewardApplier)}] Tried to apply a null card or effect.");
            return;
        }

        PlayerSkillHolder skillHolder = LocalPlayerStateManager.LocalInstance?.localPlayerRef;
        if (skillHolder == null || skillHolder.statModifierHolder == null)
        {
            Debug.LogWarning($"[{nameof(WaveRewardApplier)}] Local player has no StatModifierHolder — cannot apply effect.");
            return;
        }

        GameplayEffect effect = card.effect.CreateEffect();
        effect.Definition = card.effect;
        effect.SourceNetworkObjectId = ulong.MaxValue; // Singleplayer — no real network id needed
        effect.CarrierNetworkObjectId = ulong.MaxValue;

        skillHolder.statModifierHolder.Apply(effect);

        Debug.Log($"[{nameof(WaveRewardApplier)}] Applied reward '{card.cardName}' ({card.effect.effectId}).");
    }
}
