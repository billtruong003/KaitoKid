using System;
using Teabag.Authentication;
using Teabag.Core;
using UnityEngine;
using UnityEngine.Events;

public class AsteroidSpawner : MonoBehaviour
{
    public FakePhysics asteroid;
    public bool spawn;
    bool doneForSecond = false;
    public UnityEvent onSpawn;

    private void Update()
    {
        if (!spawn)
            return;

        DateTime time = AuthenticationUtils.serverTime;
        float dividedSecond = time.Second / 5f;
        int fullSecond = Mathf.RoundToInt(dividedSecond);
        if (dividedSecond == fullSecond)
        {
            if (!doneForSecond)
            {
                Debug.Log("Spawn asteroid");
                System.Random random = new System.Random(fullSecond + time.Date.DayOfYear);
                for (int i = 0; i < 3; i++)
                {
                    FakePhysics physics = Instantiate(asteroid, transform.position, Quaternion.identity);
                    Vector3 velocity = new Vector3(random.Next(-10, 10), random.Next(20, 60), random.Next(-10, 10));
                    physics.velocity = velocity;
                }

                onSpawn.Invoke();

                doneForSecond = true;
            }
        }
        else
            doneForSecond = false;
    }

    public void StartSpawning()
    {
        spawn = true;
    }

    public void StopSpawning()
    {
        spawn = false;
    }
}
