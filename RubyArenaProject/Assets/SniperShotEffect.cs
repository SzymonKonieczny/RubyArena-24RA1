using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SniperShotEffect : MonoBehaviour, ISkillEffect
{
    [SerializeField] ParticleSystem shotEffect;
    public void PlayEffect(int id)
    {
        shotEffect.Play();
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
