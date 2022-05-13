using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TwoDimensionalAnimationStateController : MonoBehaviour
{
    Animator animator;
    float velocityZ = 0.0f;
    float velocityX = 0.0f;
    public float acceleration = 2.0f;
    public float deceleration = 2.0f;
    public float maximumWalkVelocity = 0.5f;
    public float maximumRunVelocity = 2.0f;

    //increase performance
    int VelocityZHash;
    int VelocityXHash;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();

        // increase performance
        VelocityZHash = Animator.StringToHash("Velocity Z");
        VelocityXHash = Animator.StringToHash("Velocity X");
    }

    //handles acceleration and deceleration
    void changeVelocity(bool forwardPressed, bool leftPressed, bool rightPressed, bool runPressed, float currentMaxVelocity){
        if(forwardPressed && velocityZ < currentMaxVelocity){
            velocityZ += Time.deltaTime * acceleration;
        }
        if(leftPressed && velocityX > -currentMaxVelocity){
            velocityX -= Time.deltaTime * acceleration;
        }
        if(rightPressed && velocityX < currentMaxVelocity){
            velocityX += Time.deltaTime * acceleration;
        }

        if(!forwardPressed && velocityZ > 0.0f){
            velocityZ -= Time.deltaTime * deceleration;
        }

        if(!leftPressed && velocityX < 0.0f){
            velocityX += Time.deltaTime * deceleration;
        }

        if(!rightPressed && velocityX > 0.0f){
            velocityX -= Time.deltaTime * deceleration;
        }
    }

    //handles reset and locking of velocity
    void lockOrResetVelocity(bool forwardPressed, bool leftPressed, bool rightPressed, bool runPressed, float currentMaxVelocity){
        if(!forwardPressed && velocityZ < 0.0f){
            velocityZ = 0.0f;
        }

        if(!leftPressed && !rightPressed && velocityX != 0.0f && (velocityX > -0.5f && velocityX < 0.05f)){
            velocityX = 0.0f;
        }

        //lock forward
        if(forwardPressed && runPressed && velocityZ > currentMaxVelocity){
            velocityZ = currentMaxVelocity;
        } else if(forwardPressed && velocityZ > currentMaxVelocity) {
            velocityZ -= Time.deltaTime * deceleration;
            if(velocityZ > currentMaxVelocity && velocityZ < (currentMaxVelocity + 0.05f)){
                velocityZ = currentMaxVelocity;
            }
        } else if(forwardPressed && velocityZ < currentMaxVelocity && velocityZ > (currentMaxVelocity - 0.05f)){
            velocityZ = currentMaxVelocity;
        }

        //lock left
        if(leftPressed && runPressed && velocityX < -currentMaxVelocity){
            velocityX = -currentMaxVelocity;
        } else if(leftPressed && velocityX < -currentMaxVelocity) {
            velocityX += Time.deltaTime * deceleration;
            if(velocityX < -currentMaxVelocity && velocityX > (-currentMaxVelocity - 0.05f)){
                velocityX = -currentMaxVelocity;
            }
        } else if(leftPressed && velocityX > -currentMaxVelocity && velocityX < (-currentMaxVelocity + 0.05f)){
            velocityX = -currentMaxVelocity;
        }

        //lock right
        if(rightPressed && runPressed && velocityX > currentMaxVelocity){
            velocityX = currentMaxVelocity;
        } else if(rightPressed && velocityX > currentMaxVelocity) {
            velocityX -= Time.deltaTime * deceleration;
            if(velocityX > currentMaxVelocity && velocityX < (currentMaxVelocity + 0.05f)){
                velocityX = currentMaxVelocity;
            }
        } else if(rightPressed && velocityX < currentMaxVelocity && velocityX > (currentMaxVelocity - 0.05f)){
            velocityX = currentMaxVelocity;
        }
    }

    // Update is called once per frame
    void Update()
    {
        bool forwardPressed = Input.GetKey(KeyCode.W);
        bool leftPressed = Input.GetKey(KeyCode.A);
        bool rightPressed = Input.GetKey(KeyCode.D);
        bool runPressed = Input.GetKey(KeyCode.LeftShift);

        float currentMaxVelocity = runPressed ? maximumRunVelocity : maximumWalkVelocity;

        // handle changes in velocity
        changeVelocity(forwardPressed, leftPressed, rightPressed, runPressed, currentMaxVelocity);
        lockOrResetVelocity(forwardPressed, leftPressed, rightPressed, runPressed, currentMaxVelocity);

        animator.SetFloat(VelocityZHash, velocityZ);
        animator.SetFloat(VelocityXHash, velocityX);
    }
}
