using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillsUIManager : MonoBehaviour
{
    [SerializeField] GridLayoutGroup layoutGroup;
    [SerializeField] RectTransform rectTransform;


    // Start is called before the first frame update
    void Start()
    {
        int cellSize = (int)(Screen.height * 0.15f); //30% of the screen
        //int groupHeight = (int)(Screen.height * 0.1f);
        layoutGroup.cellSize = new Vector2(cellSize, cellSize);
        rectTransform.sizeDelta = new Vector2(cellSize * 3 , cellSize);

    }
    // Update is called once per frame
    void Update()
    {

    }
}
