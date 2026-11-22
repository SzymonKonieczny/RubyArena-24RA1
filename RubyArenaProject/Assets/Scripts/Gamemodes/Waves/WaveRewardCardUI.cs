using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sits on each reward card UI prefab. Populates the visible elements from a
/// <see cref="WaveRewardEffectCardSO"/> and reports clicks back to the reward panel.
/// </summary>
public class WaveRewardCardUI : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] Image iconImage;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text descriptionText;

    [Header("Interaction")]
    [SerializeField] Button selectButton;

    /// <summary>
    /// The card data currently shown on this UI element.
    /// </summary>
    public WaveRewardEffectCardSO Card { get; private set; }

    /// <summary>
    /// Index of this card within the current offer (0, 1, 2…).
    /// Passed to the callback so the applier knows which one was chosen.
    /// </summary>
    public int CardIndex { get; private set; }

    System.Action<int> onSelected;

    void Awake()
    {
        if (selectButton != null)
            selectButton.onClick.AddListener(OnClick);
    }

    void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveListener(OnClick);
    }

    /// <summary>
    /// Sets the card data and refreshes all visible UI elements.
    /// Call this from your reward panel when an offer arrives.
    /// </summary>
    /// <param name="card">The card definition to display.</param>
    /// <param name="index">Position in the offer, used when reporting the choice.</param>
    /// <param name="selectedCallback">Called with <paramref name="index"/> when the player clicks this card.</param>
    public void SetCard(WaveRewardEffectCardSO card, int index, System.Action<int> selectedCallback)
    {
        Card = card;
        CardIndex = index;
        onSelected = selectedCallback;

        if (card == null)
        {
            ClearVisuals();
            return;
        }

        if (nameText != null)
            nameText.text = card.cardName;

        if (descriptionText != null)
            descriptionText.text = card.description;

        if (iconImage != null)
        {
            iconImage.sprite = card.icon;
            iconImage.enabled = card.icon != null;
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// Hides the card and clears its state.
    /// </summary>
    public void Clear()
    {
        Card = null;
        CardIndex = -1;
        onSelected = null;
        gameObject.SetActive(false);
    }

    void OnClick()
    {
        onSelected?.Invoke(CardIndex);
    }

    void ClearVisuals()
    {
        if (nameText != null) nameText.text = "";
        if (descriptionText != null) descriptionText.text = "";
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }
}
