using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UIElements;



    [RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(InputCollectorScript))]

public class Movement : NetworkBehaviour
{
    public enum PlayerOrientationModes
    {
        Walking,
        Aiming
    }
    [SerializeField] Transform AimingCameraPos;
    [SerializeField] Transform NormalCameraPos;

     Transform cameraPos;
    [SerializeField] Transform Model;
    [SerializeField] Transform Orientation;
    [SerializeField] Transform UpwardTorsoBone;
    [SerializeField] PlayerOrientationModes OrientationMode = PlayerOrientationModes.Walking;
    public NetworkVariable<Vector3> RbVelocityNetworkVar = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Owner);
    PlayerResources playerResources;
    private Vector3 RequestedVelocityToAdd = new Vector3(); //Vector of velocity requested by the server to add to this Rb
    private Vector3 RequestedForceToAdd = new Vector3(); //Vector of velocity requested by the server to add to this Rb

    private InputCollectorScript InputCollector;
    private Rigidbody Rb;
    private PlayerScript playerScript;
    public float speed = 30;

    public float jumpForce;

    private bool isGrounded;
    void Start()
    {
        InputCollector = GetComponent<InputCollectorScript>();
        playerScript = GetComponent<PlayerScript>();
        playerResources = GetComponent<PlayerResources>();

        if(IsOwner)
        {
            if (OrientationMode == PlayerOrientationModes.Walking)
            {

                AimingCameraPos.gameObject.SetActive(false);
                cameraPos = NormalCameraPos;
            }
            else
            {
                NormalCameraPos.gameObject.SetActive(false);
                cameraPos = AimingCameraPos;
            }
        }

       
       if(IsOwner) 
            playerResources.Hp.OnValueChanged += (float prev, float newV) =>
             {
                 if (newV <= 0)
                 {
                     playerResources.Hp.Value = 100;
                     
                        NetworkObject.transform.position = new Vector3(0, 2, 0);
                 }
             };
        

    }
    [ClientRpc]
    public void AddNetworkRbVelocityClientRPC(Vector3 vel)
    {
        RequestedVelocityToAdd += vel;
    }
    [ClientRpc]
    public void AddNetworkRbForceClientRPC(Vector3 force)
    {
        RequestedForceToAdd += force;
    }
    void FixedUpdate()
    {
        if (playerScript.isStunnedNetworkVar.Value) return;

        if (IsOwner)
        {
            HandleMove();
        }   
    }
    private void Update()
    {
        if(IsOwner)
        {
            DoPlayerOrientation();
        }
    }
    [SerializeField]
    public void SwitchCameraStyle()
    {
        if (OrientationMode == PlayerOrientationModes.Walking)
        {
            OrientationMode = PlayerOrientationModes.Aiming;

            AimingCameraPos.gameObject.SetActive(true);

            cameraPos = AimingCameraPos;
            NormalCameraPos.gameObject.SetActive(false);
        }
        else
        {
            OrientationMode = PlayerOrientationModes.Walking;

            NormalCameraPos.gameObject.SetActive(true);

            cameraPos = NormalCameraPos;
            AimingCameraPos.gameObject.SetActive(false);
        }

    }
    public override void OnNetworkSpawn()
    {
        Rb = GetComponent<Rigidbody>();
        Rb.isKinematic = true;
    }
    void HandleMove()
    {
        if (Input.GetAxis("Jump") > 0)
        {
            if (isGrounded)
            {
                Rb.AddForce(transform.up * jumpForce);
            }
        }
        Vector3 velocity = new();
        switch (OrientationMode)
        {
            case PlayerOrientationModes.Walking:
                velocity = ((Orientation.forward * InputCollector.VerticalAxis) + (Orientation.right * InputCollector.HorizontalAxis))
              * speed * Time.fixedDeltaTime;

                break;
            case PlayerOrientationModes.Aiming:
                velocity = ((Model.forward * InputCollector.VerticalAxis) + (Model.right * InputCollector.HorizontalAxis))
             * speed * Time.fixedDeltaTime;
                break;
        }


        velocity += RequestedVelocityToAdd;
        RequestedVelocityToAdd = Vector3.zero;

        velocity.y = Rb.velocity.y;
        Rb.velocity = velocity;
        RbVelocityNetworkVar.Value = velocity;

        if(RequestedForceToAdd != Vector3.zero)
        {
            Rb.AddForce(RequestedForceToAdd);

            RequestedForceToAdd = Vector3.zero;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == ("Ground"))
        {
            isGrounded = true;
        }
    }
    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag == ("Ground"))
        {
            isGrounded = false;
        }
    }
    void DoPlayerOrientation()
    {
        switch (OrientationMode)
        {
            case PlayerOrientationModes.Walking:
                DoOrientationWalking();
                break;
            case PlayerOrientationModes.Aiming:
                doAiming();
                break;
        }
    }
    void doAiming()
    {
        /*
        Vector3 viewDirection = transform.position - new Vector3(cameraPos.position.x, transform.position.y,
                                                                       cameraPos.position.z);
        Orientation.forward = viewDirection.normalized;

        Vector3 DirToCombatLook = CombatLookAt.position - cameraPos.position;
        CombatLookAt.forward = DirToCombatLook.normalized;*/

        var ray = Camera.main.ScreenPointToRay(new Vector3((float)Screen.width / 2f, (float)Screen.height / 2f));
        Vector3 lookatDir;
        if (Physics.Raycast(ray, out RaycastHit raycastHit))
        {
            lookatDir = (raycastHit.point - ray.origin);
            lookatDir.y = 0;
        }
        else
        {
            lookatDir = ray.direction;
            lookatDir.y = 0;
        }

        Model.forward = lookatDir.normalized;
    }
    void DoOrientationWalking()
    {
        Vector3 viewDirection = transform.position - new Vector3(cameraPos.position.x, transform.position.y,
                                                                          cameraPos.position.z);
        Orientation.forward = viewDirection.normalized;
        Vector3 InputVec = Orientation.forward * InputCollector.VerticalAxis + Orientation.right * InputCollector.HorizontalAxis;
        if (InputVec != Vector3.zero)
        {
            Model.forward = Vector3.Slerp(Model.forward, InputVec, 0.2f);
        }

    }
    public void OrientTowardsCameraDir()
    {
        Vector3 viewDirection = transform.position - new Vector3(cameraPos.position.x, transform.position.y,
                                                                                 cameraPos.position.z);

        if (viewDirection != Vector3.zero)
        {
            Model.forward = viewDirection;
        }
    }
}