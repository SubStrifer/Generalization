using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class Stats : MonoBehaviour
{
    public float speedMultipler;

    private static int lastSteps;
    private static int collisions;
    private static int hits;
    private static StatsRecorder statsRecorder;
    private List<float> collisionsList;
    private List<float> hitsList;
    private bool statsPrinted;

    // Start is called before the first frame update
    void Start()
    {
        lastSteps = 9000;
        statsRecorder = Academy.Instance.StatsRecorder;
        collisionsList = new List<float>();
        hitsList = new List<float>();
    }

    // Update is called once per frame
    void Update()
    {
        // Update stats
        if(Academy.Instance.StepCount > lastSteps)
        {
            lastSteps += 10000;
            statsRecorder.Add("Collisions", (float)collisions / (float)EnvironmentGenerator.agentsNumber);
            statsRecorder.Add("Hits", (float)hits / (float)EnvironmentGenerator.agentsNumber);
            collisionsList.Add((float)collisions / (float)EnvironmentGenerator.agentsNumber);
            hitsList.Add((float)hits / (float)EnvironmentGenerator.agentsNumber);
            collisions = 0;
            hits = 0;
        }

        // Print stats
        if(!statsPrinted && Academy.Instance.StepCount > 100000)
        {
            statsPrinted = true;
            Debug.Log("Collisions\n" + string.Join("\n", collisionsList.ToArray()));
            Debug.Log("Hits\n" + string.Join("\n", hitsList.ToArray()));
        }

        // Speed up time
        if(Input.GetKeyUp(KeyCode.X))
        {
            if(Time.timeScale == 1f)
                Time.timeScale = speedMultipler;
            else
                Time.timeScale = 1f;
        }
    }

    public static void AddCollision()
    {
        collisions++;;
    }

    public static void AddHit()
    {
        hits++;
    }
}
