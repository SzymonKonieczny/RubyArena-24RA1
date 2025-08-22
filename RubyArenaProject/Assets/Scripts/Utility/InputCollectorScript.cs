using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputCollectorScript : MonoBehaviour
{
    public float VerticalAxis {private set; get; }
    public float HorizontalAxis { private set; get; }

    public bool Jump { private set; get; } = false;
    public bool LeftClick { private set; get; } = false;
    public bool RightClick { private set; get; } = false;

    public bool QClick {  
        private set { QClickRef.value = value;}
        get { return QClickRef.value; }
    }

    public bool EClick
    {
        private set { EClickRef.value = value; }
        get { return EClickRef.value; }
    }

    public BoolRefType QClickRef { private set; get; } = new BoolRefType();
    public BoolRefType EClickRef { private set; get; } = new BoolRefType();

    public float StunTime = 0;
    /// <summary>
    /// CHEAT PRONE PROBALBLY TEMP SOLUTION xd ("temp")
    /// </summary>
    

    // Update is called once per frame
    void Update()
    {
        StunTime -= Time.deltaTime;

        if (StunTime > 0)
        {
            VerticalAxis = 0;
            HorizontalAxis = 0;
            QClick = false;
            EClick = false;
            return;
        }

        VerticalAxis = Input.GetAxis("Vertical");
        HorizontalAxis = Input.GetAxis("Horizontal");
        QClick = Input.GetKeyDown(KeyCode.Q);
        EClick = Input.GetKeyDown(KeyCode.E);
    }
}
