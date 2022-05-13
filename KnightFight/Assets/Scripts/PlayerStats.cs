using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerStats : MonoBehaviour
{

    public float health;
    public int enemiesKilled;

    void Start()
    {
        //Basic stats for the player to be used in other scripts
        enemiesKilled = 0;
        health = 100f;

    }

    // Update is called once per frame
    void Update()
    {

    }
    
}
