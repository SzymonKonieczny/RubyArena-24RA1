using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YangGlovesGlow : MonoBehaviour, ISkillEffect
{
    public GameObject LeftFlame;
    public GameObject RightFlame;

    public void PlayEffect(int id)
    {
       switch(id)
        {
            case 0:
                RightFlame.SetActive(true);
                LeftFlame.SetActive(true);

                break;
            case 1:
                RightFlame.SetActive(false);
                LeftFlame.SetActive(false);
                break;

        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
