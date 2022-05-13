using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    // Start is called before the first frame update
    GameObject enemy1;
    GameObject enemy2;
    GameObject enemy3;
    Enemy enemy1Stats;
    Enemy enemy2Stats;
    Enemy enemy3Stats;
    void Start()
    {
        //Get all enemies in the hierarchy
        enemy1 = GameObject.Find("Enemy1");
        enemy2 = GameObject.Find("Enemy2");
        enemy3 = GameObject.Find("Enemy3");
        enemy1Stats = enemy1.GetComponent<Enemy>();
        enemy2Stats = enemy2.GetComponent<Enemy>();
        enemy3Stats = enemy3.GetComponent<Enemy>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Attack() //Attack is used at a certain frame in all attack animations
    {
        //Check distance between the player and all enemies. If the distance is close enough, deal damage.
        GameObject player = GameObject.Find("Player");
        if(enemy1.activeSelf && (Vector3.Distance (enemy1.transform.position, player.transform.position) <= 1.5))
        {
            enemy1Stats.health = enemy1Stats.health - 20f;
        }
        if(enemy2.activeSelf && (Vector3.Distance (enemy2.transform.position, player.transform.position) <= 1.5))
        {
            enemy2Stats.health = enemy2Stats.health - 20f;
        }
        if(enemy3.activeSelf && (Vector3.Distance (enemy3.transform.position, player.transform.position) <= 1.5))
        {
            enemy3Stats.health = enemy3Stats.health - 20f;
        }
    }
}
