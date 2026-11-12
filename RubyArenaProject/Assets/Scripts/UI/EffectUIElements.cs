using Assets.Scripts.Events;
using Assets.Scripts.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using VContainer;

public class EffectUIElements : MonoBehaviour
{

    

    [SerializeField] GridLayoutGroup layoutGroup;
    [SerializeField] RectTransform rectTransform;
    [SerializeField] float cellSizeScreenSizeRatio = 0.10f;
    [SerializeField] float extraPadding = 0.05f;
    Dictionary<GameplayEffectSO, UIEffectIcon> effects = new();

    [SerializeField] GameObject IconPrefab;

    [Inject] //Injected by direct reference. The scope is given a reference to this. 
    EventBus<EntityGameplayEffectListChange> effectsChanged;

    EventBinding<EntityGameplayEffectListChange> EventBinding;
    // Start is called before the first frame update
    void Start()
    {
        UiUtils.ScaleGridLayout(extraPadding, cellSizeScreenSizeRatio, layoutGroup, rectTransform);
        EventBinding = new(OnGlobalEffectListChanged);
        effectsChanged.Register(EventBinding);

    }
    void OnGlobalEffectListChanged(EntityGameplayEffectListChange change)
    {
        //Check if its us.
        if (LocalPlayerStateManager.LocalInstance.localPlayerRef.statModifierHolder.NetworkObjectId != change.recieverNetworkObjectId)
            return;

        switch (change.Type) 
        {
            case EntityGamePlayEffectChangeType.Added:
                AddEffectIcon(change);
                break;
            case EntityGamePlayEffectChangeType.Refreshed:
                effects[change.effectSO].endTime = NetworkManager.Singleton.ServerTime.Time + change.effectSO.duration;
                break;
            case EntityGamePlayEffectChangeType.Removed:
                Destroy(effects[change.effectSO].gameObject);
                effects.Remove(change.effectSO);
                break;
        }
    }


    void AddEffectIcon(EntityGameplayEffectListChange change)
    {
        var EffectUI = Instantiate(IconPrefab, this.transform).GetComponent<UIEffectIcon>();

        EffectUI.image.sprite = change.effectSO.Icon;
        EffectUI.endTime = NetworkManager.Singleton.ServerTime.Time + change.effectSO.duration;
        effects.TryAdd(change.effectSO, EffectUI);
    }
    // Update is called once per frame
    void Update()
    {
        foreach ((var EffectSO, var EffectUI ) in effects)
        {
            double serverTime = NetworkManager.Singleton.ServerTime.Time;
            float secondsRemaming = Mathf.Clamp((float)(EffectUI.endTime - serverTime), 0, 99);
            EffectUI.slider.value = Mathf.Clamp((secondsRemaming / EffectSO.duration), 0, 1);
        }

    }
    private void OnDestroy()
    {
        effectsChanged.Unregister(EventBinding);
    }
}
