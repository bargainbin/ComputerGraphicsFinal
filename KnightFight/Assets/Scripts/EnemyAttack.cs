using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    // Start is called before the first frame update
    GameObject player;
    public GameObject enemy;
    PlayerStats playerStats;
    void Start()
    {
        player = GameObject.Find("Player");
        playerStats = player.GetComponent<PlayerStats>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void eAttack() //Called during a certain frame of the enemy attack
    {
        //If the enemy is within a certain distance of the player, deal damage
        if(Vector3.Distance (enemy.transform.position, player.transform.position) <= 1.5)
        {
            playerStats.health = playerStats.health - 20f;
        }
    }
}
