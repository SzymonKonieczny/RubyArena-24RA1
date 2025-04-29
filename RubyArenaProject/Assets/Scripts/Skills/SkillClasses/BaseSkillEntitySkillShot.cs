using Unity.Netcode;

public abstract class BaseSkillEntityBehaviorSkillShot : BaseSkillEntityBehavior
{
    public virtual void Start()
    {
        Hit.OnValueChanged = CollisionRegisteredOnServer;
    }
    protected abstract NetworkObject GetAffectedObjectS();
    protected abstract void AffectObjectS(NetworkObject Object);
    protected override abstract void PlayEffectsC();

    protected override void CollisionRegisteredOnServer(bool prev, bool newV)
    {

        if (newV == false || prev == true) return;

        if (IsServer)
        {
            if (!wasCancelled)
            {
                var affectedObj = GetAffectedObjectS();
                AffectObjectS(affectedObj);
            }
          //  DisableColldiersSC();
        }

        PlayEffectsC();
            DisableColldiersSC();
     //   if (IsClient) // host is both client and server!
     //   {
     //
     //   }

    }
}
