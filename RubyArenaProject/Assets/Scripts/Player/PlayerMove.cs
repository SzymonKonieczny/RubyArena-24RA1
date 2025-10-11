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
    public Transform Orientation;
    [SerializeField] Transform UpwardTorsoBone;
    [SerializeField] PlayerOrientationModes OrientationMode = PlayerOrientationModes.Walking;
    public NetworkVariable<Vector3> RbVelocityNetworkVar = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Owner);
    public PlayerResources playerResources {  get;  private set; }
    private Vector3 RequestedVelocityToAdd = new Vector3(); //Vector of velocity requested by the server to add to this Rb
    private Vector3 RequestedForceToAdd = new Vector3(); //Vector of velocity requested by the server to add to this Rb

    private InputCollectorScript InputCollector;
    public Rigidbody Rb;
    private PlayerScript playerScript;
    public float speed = 30;
    public bool canFly = false;
    public float dashModifier = 25;

    public float jumpForce;

    private bool isGrounded;
    void Start()
    {
        InputCollector = GetComponent<InputCollectorScript>();
        playerScript = GetComponent<PlayerScript>();
        playerResources = GetComponent<PlayerResources>();

        if (IsOwner)
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
    }

    public void SnapModelToCameraDir()
    {
        Model.forward = Camera.main.transform.forward.ScaleVec(new Vector3(1, 0, 1)).normalized;
    }


    [ClientRpc]
    public void RequestTeleportClientRPC(Vector3 newPosition)
    {
        transform.position = newPosition;
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
    IEnumerator Dash(Vector3 dashDirection, float dashSpeed, float dashDuration)
    {
        float time = 0f;
        int it = 0;
        while (time < dashDuration)
        {
            Vector3 offset = dashDirection.normalized * dashModifier * Time.fixedDeltaTime;
            transform.position += offset;
            time += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
            it += 1;
        }

    }
    public void startDash()
    {
        Vector3 direction = new();
        switch (OrientationMode)
        {
            case PlayerOrientationModes.Aiming:
                direction = ((Model.forward * Input.GetAxis("Vertical")) + (Model.right * Input.GetAxis("Horizontal"))); 
                break;
            case PlayerOrientationModes.Walking:
                direction = Model.forward * Mathf.Abs( (Input.GetAxis("Vertical") + Input.GetAxis("Horizontal"))/2);
                break;
        }

        StartCoroutine(Dash(direction.normalized, speed , 0.2f));
        //StartCoroutine(Dash(Rb.velocity.normalized, speed , 0.2f));
    }

    void FixedUpdate()
    {

        if (IsOwner)
        {
            HandleMove();
        }   
    }
    private void Update()
    {
        if(IsOwner)
        {
            if (InputCollector.rightClick)
                SwitchCameraStyle();
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

            //Set model rotation to current camera look
            SnapModelToCameraDir();
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
      
        Vector3 velocity = new();
        switch (OrientationMode)
        {
            case PlayerOrientationModes.Walking:
                velocity = ((Orientation.forward * Input.GetAxis("Vertical")) + (Orientation.right * Input.GetAxis("Horizontal")))
              * speed * Time.fixedDeltaTime;

                break;
            case PlayerOrientationModes.Aiming:
                velocity = ((Model.forward * Input.GetAxis("Vertical")) + (Model.right * Input.GetAxis("Horizontal")))
             * speed * Time.fixedDeltaTime;
                break;
        }

        if (canFly)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                velocity.y = jumpForce;
            }
            if (Input.GetKey(KeyCode.LeftShift))
            {
                velocity.y = -jumpForce;
            }
        }
        else
        {
            if(isGrounded && Input.GetKeyDown(KeyCode.Space))
            {
                velocity.y = jumpForce;
            }
        }
        velocity += RequestedVelocityToAdd;
        RequestedVelocityToAdd = Vector3.zero;

        velocity.y += Rb.velocity.y;

        if(InputCollector.StunTime >0)
        {
            velocity = Vector3.zero;
        }
        Rb.velocity = velocity;
        RbVelocityNetworkVar.Value = velocity;
        if(RequestedForceToAdd != Vector3.zero)
        {
            Rb.AddForce(RequestedForceToAdd, ForceMode.Impulse);

            RequestedForceToAdd = Vector3.zero;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == ("Terrain"))
        {
            isGrounded = true;
        }
    }
    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag == ("Terrain"))
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