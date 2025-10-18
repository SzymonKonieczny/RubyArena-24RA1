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

    [SerializeField]
    public void Recalculate()
    {
        int cellSize = (int)(Screen.height * cellSizeScreenSizeRatio);
        int padding = (int)(Screen.height * extraPadding);
        layoutGroup.cellSize = new Vector2(cellSize, cellSize);
        layoutGroup.padding.bottom = (padding / 2);
        layoutGroup.padding.top = (padding / 2);
        layoutGroup.padding.left = (padding / 2);
        layoutGroup.padding.right = (padding / 2);
        rectTransform.sizeDelta = new Vector2((cellSize) * 3 + padding, cellSize + padding);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
