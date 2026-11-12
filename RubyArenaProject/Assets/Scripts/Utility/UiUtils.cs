using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;

namespace Assets.Scripts.Utility
{
    class UiUtils
    {
        public static void  ScaleGridLayout(float _extraPadding, float _cellSizeScreenSizeRatio, GridLayoutGroup layoutGroup, RectTransform rectTransform)
        {
            int cellSize = (int)(Screen.height * _cellSizeScreenSizeRatio);
            int padding = (int)(Screen.height * _extraPadding);
            layoutGroup.cellSize = new Vector2(cellSize, cellSize);
            layoutGroup.padding.bottom = (padding / 2);
            layoutGroup.padding.top = (padding / 2);
            layoutGroup.padding.left = (padding / 2);
            layoutGroup.padding.right = (padding / 2);
            rectTransform.sizeDelta = new Vector2((cellSize) * 3 + padding, cellSize + padding);
        }
    }
}
