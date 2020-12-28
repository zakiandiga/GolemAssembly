using System;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    //Add character visual game object as a children to the prefab
    //Required cinemachine brain on main camera

    #region InputActionReference
    [SerializeField] private InputActionReference movementControl;
    [SerializeField] private InputActionReference jumpControl;
    [SerializeField] private InputActionReference crouchControl;
    [SerializeField] private InputActionReference interactControl;
    [SerializeField] private InputActionReference openMenu;
    #endregion

    #region MovementVariables
    public Transform groundChecker;
    public float groundCheckerRadius = 0.4f;
    public LayerMask groundMask;
    private bool groundedPlayer;

    [SerializeField] private float playerSpeed = 2.0f;
    [SerializeField] private float jumpHeight = 1.0f;
    [SerializeField] private float gravityValue = -100f;
    [SerializeField] private float rotationSpeed = 4f;
    private float jumpConst = -3.0f;
    private Vector3 playerVelocity;

    private Vector2 movement;

    #endregion

    #region CameraComponent
    private Transform cam;
    //private GameObject cinemachineLock;
    [SerializeField] private CinemachineVirtualCamera directObjCam;
    [SerializeField] private CinemachineVirtualCamera playerLockCam;
    [SerializeField] private CinemachineFreeLook playerFreeCam;
    private CinemachineBrain cameraBrain;
    #endregion

    #region OtherRequiredComponent
    private CharacterController controller;
    private Animator anim;
    #endregion

    #region ActionAnnouncer
    public static event Action<PlayerMovement> OnOpenMenu; 
    public static event Action<PlayerMovement> OnInteract;
    #endregion

    #region PlayerState
    private MovementState movementState = MovementState.Idle;
    public enum MovementState 
    {
        Move,
        Crouch,
        Idle,
        Jump,
        OnMenu,
        OnCutscene
    }    
    #endregion

    #region CameraMode
    private CameraMode cameraMode = CameraMode.Free;
    public enum CameraMode
    {
        Free,
        LockOn,
        Cutscene,
        OnObject
    }
    #endregion

    private void OnEnable()
    {
        movementControl.action.Enable(); //Enable (and disable) these reference action
        jumpControl.action.Enable();     //Utilize this to activate/deactivate player control
        crouchControl.action.Enable();   //Instead of SetActive the component
        openMenu.action.Enable();
        interactControl.action.Enable();
    }

    private void OnDisable()
    {
        movementControl.action.Disable();
        jumpControl.action.Disable();
        crouchControl.action.Disable();
        openMenu.action.Disable();
        interactControl.action.Disable();
    }

    private void Start()
    {
        controller = GetComponent<CharacterController>();        
        anim = GetComponentInChildren<Animator>();
        cam = Camera.main.transform;
        cameraBrain = cam.GetComponent<CinemachineBrain>();
        //InventoryUI.OnAssembling += AssemblingControl;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void DisablingMovement()  //when opening UI
    {
        movementControl.action.Disable();
        jumpControl.action.Disable();
        crouchControl.action.Disable();
        interactControl.action.Disable();        
    }

    private void EnablingMovement()
    {
        movementControl.action.Enable();
        jumpControl.action.Enable();
        crouchControl.action.Enable();
        interactControl.action.Enable();        
    }

    private void CameraStateSwitch()
    {
        switch(cameraMode)
        {
            case CameraMode.Free:
                playerFreeCam.m_Priority = 1;
                playerLockCam.m_Priority = 0;
                directObjCam.m_Priority = 0;
                break;
            case CameraMode.LockOn:
                playerLockCam.m_Priority = 1;
                directObjCam.m_Priority = 0;
                playerFreeCam.m_Priority = 0;
                break;
            case CameraMode.OnObject:
                directObjCam.m_Priority = 1;
                playerLockCam.m_Priority = 0;
                playerFreeCam.m_Priority = 0;
                break;
        }        
    }

    private void MenuControlSwitch()
    {
        if(movementState == MovementState.OnMenu)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            DisablingMovement();
        }
        else if(movementState != MovementState.OnMenu)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            EnablingMovement();
        }
    }

    /*
    private void AssemblingControl(InventoryUI ui)
    {
        if (cameraMode == CameraMode.OnObject)
        {
            cameraMode = CameraMode.Free;
        }
        else
        {
            cameraMode = CameraMode.OnObject;
        }
        CameraStateSwitch();       
    }
    */

    void Update()
    {
        #region Movement

        //groundedPlayer = controller.isGrounded;
        groundedPlayer = Physics.CheckSphere(groundChecker.position, groundCheckerRadius, groundMask);

        if (groundedPlayer && playerVelocity.y < 0)
        {            
            playerVelocity.y = -2;
            //anim.SetFloat("vSpeed", 0);
            //anim.SetBool("jump", false);
        }

        movement = movementControl.action.ReadValue<Vector2>();
                
        #region MovementState
        //MovementStateHandler
        if (movement != Vector2.zero)
        {
            if (movementState != MovementState.Move)
            {
                movementState = MovementState.Move;
                //anim.SetBool("run", true);
                //anim.SetFloat("forward", movement.y);
                //anim.SetFloat("right", movement.x);
            }
        }
        if(movement == Vector2.zero && movementState != MovementState.OnMenu)
        {
            if(movementState != MovementState.Idle)
            {
                movementState = MovementState.Idle;
                //anim.SetBool("run", false);                
            }            
        }
        #endregion

        Vector3 moveDirection = new Vector3(movement.x, 0, movement.y);
        moveDirection = cam.forward * moveDirection.z + cam.right * moveDirection.x;                    
        moveDirection.y = 0f;
        controller.Move(moveDirection * Time.deltaTime * playerSpeed);
        
        // Changes the height position of the player.
        if (jumpControl.action.triggered && groundedPlayer)
        {            
            playerVelocity.y += Mathf.Sqrt(jumpHeight * jumpConst * gravityValue);
            //groundedPlayer = controller.isGrounded;
            //anim.SetBool("jump", true);
            movementState = MovementState.Jump;
        }

        //Default gravity
        playerVelocity.y += gravityValue * Time.deltaTime;
        //anim.SetFloat("vSpeed", playerVelocity.y);
        controller.Move(playerVelocity * Time.deltaTime);
        

        #region FaceDirection        
        if (movementState == MovementState.Move) 
        {
            float targetAngle = 0f;
            if (cameraMode == CameraMode.Free)
            {
                targetAngle = Mathf.Atan2(movement.x, movement.y) * Mathf.Rad2Deg + cam.eulerAngles.y;
            }
            else if (cameraMode == CameraMode.LockOn)
            {
                targetAngle = cam.eulerAngles.y;
            }

            Quaternion rotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime * rotationSpeed);
        } 
        #endregion

        #region LockOn/Crouch
        if (crouchControl.action.ReadValue<float>() != 0)
        {
            if(cameraMode != CameraMode.LockOn && cameraMode != CameraMode.OnObject)
            {
                cameraMode = CameraMode.LockOn;
                //anim.SetBool("walk", true);
                CameraStateSwitch();
            }               
        }
        else if(crouchControl.action.ReadValue<float>() == 0)
        {
            if(cameraMode != CameraMode.Free && cameraMode != CameraMode.OnObject)
            {
                cameraMode = CameraMode.Free;
                //anim.SetBool("walk", false);
                CameraStateSwitch();
            }            
        }
        #endregion

        #endregion

        if (openMenu.action.triggered)
        {
            OnOpenMenu?.Invoke(this);
            if(movementState != MovementState.OnMenu)
            {
                movementState = MovementState.OnMenu;
            }
            else
            {
                movementState = MovementState.Idle;
            }
            MenuControlSwitch();
        }

        if(interactControl.action.triggered)
        {
            OnInteract?.Invoke(this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundChecker.position, groundCheckerRadius);
    }
}
