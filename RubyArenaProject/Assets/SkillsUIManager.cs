using Assets.Scripts.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillsUIManager : MonoBehaviour
{
    [SerializeField] GridLayoutGroup layoutGroup;
    [SerializeField] RectTransform rectTransform;
    [SerializeField] float cellSizeScreenSizeRatio = 0.15f;
    [SerializeField] float extraPadding = 0.05f;

    // Start is called before the first frame update
    void Start()
    {
        Recalculate();
    }

    public void Recalculate()
    {
        UiUtils.ScaleGridLayout(extraPadding, cellSizeScreenSizeRatio, layoutGroup, rectTransform);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
