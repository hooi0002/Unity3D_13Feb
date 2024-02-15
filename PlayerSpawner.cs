using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviour
{
    // create a variable representing this script/class
    public static PlayerSpawner instance;

    // called before Start()
    private void Awake()
    {
        instance = this;
    }

    // Player prefab in the "Resources" folder
    public GameObject playerPrefab;
    private GameObject player;

    public GameObject deathEffect;

    // Start is called before the first frame update
    void Start()
    {
        if(PhotonNetwork.IsConnected)
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer()
    {
        Transform spawnPoint = SpawnManager.instance.GetSpawnPoint();

        player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
    }

    public void Die(string damager)
    {
        UIController.instance.deathText.text = "You were killed by " + damager;
        
        if(player != null)
        {
            StartCoroutine(DieCo());
        }
    }

    // A function that is meant to run as a coroutine
    public IEnumerator DieCo()
    {
        PhotonNetwork.Instantiate(deathEffect.name, player.transform.position, Quaternion.identity);
        PhotonNetwork.Destroy(player);
        UIController.instance.deathScreen.SetActive(true);

        yield return new WaitForSeconds(5f);

        UIController.instance.deathScreen.SetActive(false);

        SpawnPlayer();
    }
}
