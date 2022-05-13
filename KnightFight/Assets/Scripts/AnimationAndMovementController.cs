using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MiddleVR.Unity;
using MiddleVR;

public class AnimationAndMovementController : MonoBehaviour
{
    //declare reference variable
    PlayerInput playerInput;
    CharacterController characterController;
    Animator animator;
    public Transform cam;
    public Enemy enemy;
    public Transform enemyPos;
    PlayerStats player;

    // variables to store player input value
    Vector2 currentMovementInput;
    Vector3 currentMovement;
    Vector3 currentRunMovement;
    bool isMovementPressed;
    bool isRunPressed;
    bool isAttackPressed;
    float rotationFactorPerFrame = 15.0f;
    float runMultiplier = 5.0f;

    float turnSmoothTime = 0.1f;
    float turnSmoothVelocity;

    // awake is called earlier than start
    void Awake(){
        // initially set reference variables
        playerInput = new PlayerInput();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        playerInput.CharacterControls.Move.started += onMovementInput;
        playerInput.CharacterControls.Move.canceled += onMovementInput;
        playerInput.CharacterControls.Move.performed += onMovementInput;
        playerInput.CharacterControls.Run.started += onRun;
        playerInput.CharacterControls.Run.canceled += onRun;
        playerInput.CharacterControls.Attack.started += onAttack;
        playerInput.CharacterControls.Attack.canceled += onAttack;
        enemy = GameObject.Find("Enemy1").GetComponent<Enemy>();
        player = GetComponent<PlayerStats>();
    }

    void onAttack(InputAction.CallbackContext context)
    {
        isAttackPressed = context.ReadValueAsButton();
    }

    void onRun(InputAction.CallbackContext context)
    {
        isRunPressed = context.ReadValueAsButton();
    }

    void onMovementInput(InputAction.CallbackContext context)
    {
        currentMovementInput = context.ReadValue<Vector2>();
        currentMovement.x = currentMovementInput.x;
        currentMovement.z = currentMovementInput.y;
        currentRunMovement.x = currentMovementInput.x * runMultiplier;
        currentRunMovement.z = currentMovementInput.y * runMultiplier;
        isMovementPressed = currentMovementInput.x != 0 || currentMovementInput.y != 0;
    }

    void handleRotation()
    {
        Vector3 positionToLookAt;
        // the change in position our character shoud point to
        positionToLookAt.x = currentMovement.x;
        positionToLookAt.y = 0.0f;
        positionToLookAt.z = currentMovement.z;
        // the current rotation of our character
        Quaternion currentRotation = transform.rotation;

        if (isMovementPressed) {
            // creates a new rotation based on where the player is currently pressing
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, rotationFactorPerFrame * Time.deltaTime);
        }
    }

    void handleAnimation()
    {
        //Get animation states from the animator
        bool isWalking = animator.GetBool("isWalking");
        bool isRunning = animator.GetBool("isRunning");
        bool isAttacking = animator.GetBool("isAttacking");

        //Check the currently set values and send to the animator to get current animation
        if (isAttackPressed)
        {
            animator.SetBool("isAttacking", true);
        }
        if (isMovementPressed && !isWalking)
        {
            animator.SetBool("isAttacking", false);
            animator.SetBool("isWalking", true);
        }else if (!isMovementPressed && isWalking){
            animator.SetBool("isWalking", false);
        }

        if ((isAttackPressed && isWalking) && !isAttacking)
        {
            animator.SetBool("isAttacking", true);
            //if(Vector3.Distance (this.transform.position, enemyPos.transform.position) < 2){
                //enemy.health = enemy.health - 10;
            //}
            
        }
        else if ((!isAttackPressed && isWalking) && isAttacking)
        {
            animator.SetBool("isAttacking", false);
        }

        if ((isMovementPressed && isRunPressed) && !isRunning)
        {
            animator.SetBool("isRunning", true);
        }
        else if ((!isMovementPressed || !isRunPressed) && isRunning)
        {
            animator.SetBool("isRunning", false);
        }

        if (player.health <= 0) //Check player's health -- if they're dead, use the death animation
        {
            
            
            animator.SetBool("isDead", true);
            
            

        }
    }

    void handleGravity() //Keeps player on the ground
    {
        if (characterController.isGrounded)
        {
            float groundGravity = -0.05f;
            currentMovement.y = groundGravity;
            currentRunMovement.y = groundGravity;
        } else
        {
            float gravity = -9.8f;
            currentMovement.y += gravity;
            currentRunMovement.y += gravity;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //These if-else statements call the MiddleVR device manager to check whether certain key and key combinations on the keyboard are pressed
        //When they are, update the boolean values that the animator checks and movement vector
        if (MVR.DeviceMgr.IsKeyPressed(MVR.VRK_A) && MVR.DeviceMgr.IsKeyPressed(MVR.VRK_W))
        {
            currentMovement.x = -1;
            currentMovement.z = 1;
            isMovementPressed = true;
        }
        else if (MVR.DeviceMgr.IsKeyPressed(MVR.VRK_A) && MVR.DeviceMgr.IsKeyPressed(MVR.VRK_S))
        {
            currentMovement.x = -1;
            currentMovement.z = -1;
            isMovementPressed = true;
        }
        else if (MVR.DeviceMgr.IsKeyPressed(MVR.VRK_D) && MVR.DeviceMgr.IsKeyPressed(MVR.VRK_W))
        {
            currentMovement.x = 1;
            currentMovement.z = 1;
            isMovementPressed = true;
        }
        else if (MVR.DeviceMgr.IsKeyPressed(MVR.VRK_S) && MVR.DeviceMgr.IsKeyPressed(MVR.VRK_D))
        {
            currentMovement.x = 1;
            currentMovement.z = -1;
            isMovementPressed = true;
        }
        else if (MVR.DeviceMgr.IsKeyPressed(MVR.VRK_A))
        {
            currentMovement.x = -1;
            currentMovement.z = 0;
            isMovementPressed = true;
        }
        else if (MVR.DeviceMgr.IsKeyPressed(MVR.VRK_W))
        {
            currentMovement.z = 1;
            currentMovement.x = 0;
            isMovementPressed = true;
        }
        else if (MVR.DeviceMgr.IsKeyPressed(MVR.VRK_S))
        {
            currentMovement.z = -1;
            currentMovement.x = 0;
            isMovementPressed = true;
        }
        else if (MVR.DeviceMgr.IsKeyPressed(MVR.VRK_D))
        {
            currentMovement.x = 1;
            currentMovement.z = 0;
            isMovementPressed = true;
        }
        else
        {
            currentMovement.x = 0;
            currentMovement.z = 0;
            isMovementPressed = false;
        }

        if (MVR.DeviceMgr.IsKeyPressed(MVR.VRK_SHIFT))
        {
            isRunPressed = true;
        }
        else
        {
            isRunPressed = false;
        }

        if (MVR.DeviceMgr.IsKeyPressed(MVR.VRK_E))
        {
            isAttackPressed = true;
        }
        else
        {
            isAttackPressed = false;
        }
        //handleRotation();
        handleAnimation(); 
        Vector3 direction = currentMovement;

        //Handle rotation
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            print(moveDir);

            if (isRunPressed)
            {
                characterController.Move(moveDir.normalized * 6.0f * Time.deltaTime);
            }
            else
            {
                characterController.Move(moveDir.normalized * 2.0f * Time.deltaTime);
            }
        }
    }

    private void FixedUpdate()
    {
        
    }

    void OnEnable(){
        // enable the character controls action map
        playerInput.CharacterControls.Enable();
    }

    void OnDisable(){
        // disable the character controls action map
        playerInput.CharacterControls.Disable();
    }

    
}
