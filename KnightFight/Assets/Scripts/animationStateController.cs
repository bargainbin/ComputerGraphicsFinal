using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class animationStateController : MonoBehaviour
{
    Animator animator;
    float velocity = 0.0f;
    public float acceleration = 0.1f;
    public float decceleration = 0.1f;
    int VelocityHash;

    void Start(){
        animator = GetComponent<Animator>();

        VelocityHash = Animator.StringToHash("Velocity");
    }

    void Update(){
        bool forwardPressed = Input.GetKey("w");
        bool runPressed = Input.GetKey("left shift");

        if(forwardPressed && velocity < 1.0f){
            velocity += Time.deltaTime * acceleration;
        }
        if(!forwardPressed && velocity > 0.0f){
            velocity -= Time.deltaTime * decceleration;
        }
        if(!forwardPressed && velocity < 0.0f){
            velocity = 0.0f;
        }

        animator.SetFloat(VelocityHash, velocity);
    }
    /*
    Animator animator;
    int isWalkingHash;
    int isRunningHash;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        isWalkingHash = Animator.StringToHash("isWalking");
        isRunningHash = Animator.StringToHash("isRunning");
    }

    // Update is called once per frame
    void Update()
    {
        bool isRunning = animator.GetBool(isRunningHash);
        bool isWalking = animator.GetBool(isWalkingHash);
        bool forwardPressed = Input.GetKey("w");
        bool runPressed = Input.GetKey("left shift");
        // if player presses the w key
        if(!isWalking && forwardPressed){
            //sets the isWalking boolean to true
            animator.SetBool(isWalkingHash, true);
        }
        // if player is not presses the w key
        if(isWalking && !forwardPressed){
            //sets the isWalking boolean to false
            animator.SetBool(isWalkingHash, false);
        }

        if(!isRunning && (forwardPressed && runPressed)){
            animator.SetBool(isRunningHash, true);
        }

        if(isRunning && (!forwardPressed || !runPressed)){
            animator.SetBool(isRunningHash, false);
        }
    }*/
}
