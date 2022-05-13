// MoveTo.cs
using UnityEngine;
using UnityEngine.AI;
    
public class MoveTo : MonoBehaviour {
       
    public Transform goal;
    float distance;
    float minDistance = 1.50f;
    public LocomotionAgent enemyAgent;
       
    
    void Start () {
        //Set the navigation agent's destination to the player's position and check distance
        enemyAgent = GetComponent<LocomotionAgent>();
        distance = Vector3.Distance (goal.transform.position, this.transform.position);
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        agent.destination = goal.position; 
    }

    void Update () {
        //If the enemy isn't dead, check to make sure that the agent isn't within a certain distance of the player.
        //If it is, stop the agent. If not, keep going.
        if (!enemyAgent.isDead)
        {
            distance = Vector3.Distance(goal.transform.position, this.transform.position);

            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            if (distance < minDistance)
            {
                agent.isStopped = true;
            }
            else
            {
                agent.isStopped = false;
            }
            //Update the agent's destination to the player's current position
            agent.destination = goal.position;
        }
        
    }
}
