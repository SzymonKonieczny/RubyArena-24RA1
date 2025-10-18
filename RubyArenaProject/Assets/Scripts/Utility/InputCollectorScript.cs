using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class InputCollectorScript : NetworkBehaviour
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

    public bool leftClick
    {
        private set { leftClickRef.value = value; }
        get { return leftClickRef.value; }
    }

    public bool rightClick
    {
        private set { rightClickRef.value = value; }
        get { return rightClickRef.value; }
    }
    public BoolRefType QClickRef { private set; get; } = new BoolRefType();
    public BoolRefType EClickRef { private set; get; } = new BoolRefType();

    public BoolRefType leftClickRef { private set; get; } = new BoolRefType();

    public BoolRefType rightClickRef { private set; get; } = new BoolRefType();


    public float StunTime = 0;
    /// <summary>
    /// CHEAT PRONE PROBALBLY TEMP SOLUTION xd ("temp")
    /// </summary>
    private void Start()
    {
        GameObject debugTimerDisplayGO = GameObject.Find("DebugStunTimer");
    }

    // Update is called once per frame
    void Update()
    {
        if (!this.IsOwner) return;
        StunTime -= Time.deltaTime;


        if (StunTime > 0)
        {
            VerticalAxis = 0;
            HorizontalAxis = 0;
            QClick = false;
            EClick = false;
            leftClick = false;
            //rightClick not affected
            return;
        }

        VerticalAxis = Input.GetAxis("Vertical");
        HorizontalAxis = Input.GetAxis("Horizontal");
        QClick = Input.GetKeyDown(KeyCode.Q);
        EClick = Input.GetKeyDown(KeyCode.E);
        leftClick = Input.GetMouseButtonDown(0);
        rightClick = Input.GetMouseButtonDown(1);
    }
}
