using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public List<Transform> spawnPoints;

    void Start()
    {

    }

    void Update()
    {

    }

    public Vector3 GetSpawnPoint()
    {
        if (spawnPoints.Count > 0)
        {
            int ramdonIndex = Random.Range(0, spawnPoints.Count);
            return spawnPoints[ramdonIndex].position;
        }
        else
        {
            Debug.Log("No hay spawns Points");
            return Vector3.zero;
        }
    }
}