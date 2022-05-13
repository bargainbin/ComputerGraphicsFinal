using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float health;
    public PlayerStats player;
    public GameObject enemy;

    void Start()
    {
        health = 50f;
        player = GameObject.Find("Player").GetComponent<PlayerStats>();
    }

    // Update is called once per frame
    void Update()
    {
       
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Sword")
        {
            this.health -= 10;
        }
    }
}
