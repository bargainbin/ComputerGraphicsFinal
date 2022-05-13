// LocomotionAgent.cs
using UnityEngine;
using UnityEngine.AI;

[RequireComponent (typeof (NavMeshAgent))]
[RequireComponent (typeof (Animator))]
public class LocomotionAgent : MonoBehaviour {
    Animator anim;
    NavMeshAgent agent;
    Vector2 smoothDeltaPosition = Vector2.zero;
    Vector2 velocity = Vector2.zero;
    public PlayerStats player;
    Enemy enemyStats;
    public bool isDead;



    void Start ()
    {
        isDead = false; //Initialize enemy to be alive
        anim = GetComponent<Animator> ();
        agent = GetComponent<NavMeshAgent> ();
        player = GameObject.Find("Player").GetComponent<PlayerStats>();
        // Don’t update position automatically
        agent.updatePosition = false;
        enemyStats = GetComponent<Enemy>(); //Used to track enemy health
    }
    
    void Update ()
    {
        if(!isDead) //Check to see if the enemy isn't dead; if not, run the navigation agent and animate
        {
            bool isAttacking = anim.GetBool("isAttacking");
            Vector3 worldDeltaPosition = agent.nextPosition - transform.position;

            // Map 'worldDeltaPosition' to local space
            float dx = Vector3.Dot(transform.right, worldDeltaPosition);
            float dy = Vector3.Dot(transform.forward, worldDeltaPosition);
            Vector2 deltaPosition = new Vector2(dx, dy);

            // Low-pass filter the deltaMove
            float smooth = Mathf.Min(1.0f, Time.deltaTime / 0.15f);
            smoothDeltaPosition = Vector2.Lerp(smoothDeltaPosition, deltaPosition, smooth);

            // Update velocity if time advances
            if (Time.deltaTime > 1e-5f)
                velocity = smoothDeltaPosition / Time.deltaTime;

            bool shouldMove = velocity.magnitude > 0.5f && agent.remainingDistance > agent.radius;

            // Update animation parameters
            anim.SetBool("isWalking", shouldMove);

            if (agent.isStopped && !isAttacking)
            {
                anim.SetBool("isAttacking", true);

            }
            if (agent.isStopped && isAttacking)
            {
                anim.SetBool("isAttacking", false);
            }
            if(enemyStats.health <= 0)
            {
                isDead = true; //If health reaches 0 or less, signify that enemy is dead
            }
        
        }
        else //If the enemy is dead, stop the navigation agent and run the death animation
        {
            agent.isStopped = true;
            anim.SetBool("isWalking", false);
            anim.SetBool("isAttacking", false);
            anim.SetBool("isDead", true);
        }


    }

    void OnAnimatorMove () 
    {
        // Update position to agent position
        transform.position = agent.nextPosition;
    }
}