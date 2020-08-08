using UnityEngine;
using Unity.MLAgents;

public class UnitAgent : Agent
{
    public UnitController unitController;

    private RayProximity rayProximity;
    private TagPerception enemyPerception;
    private static int collisions;
    private static float time;

    // Initialize agent
    public override void Initialize()
    {
        base.Initialize();
        rayProximity = new RayProximity(transform, 3f, 12, new string[] { "Wall", "Unit" });
        enemyPerception = new TagPerception(transform, 30f, 3, "Unit");
        
        unitController.Crashed += (o, s) => { Stats.AddCollision(); AddReward(-1f); EndEpisode(); };
        //unitController.Damaged += (o, s) => { AddReward(-.5f); };
        unitController.Destroyed += (o, s) => { EndEpisode(); };
        unitController.EnemyHit += (o, s) => { Stats.AddHit(); AddReward(1f); };
        unitController.EnemyNotHit += (o, s) => { AddReward(-.1f); };
    }

    // When new episode begines, i.e. when the agent was just respawned
    public override void OnEpisodeBegin()
    {
        // Reset unitController
        unitController.Reset();

        // Randomize position and rotation
        transform.position = EnvironmentGenerator.spawnPositions[Random.Range(0, EnvironmentGenerator.spawnPositions.Count)];
        transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
    }

    // Collect observations from the environment
    public override void CollectObservations(Unity.MLAgents.Sensors.VectorSensor sensor)
    {
        // Agent
        // Calculate rotation vector
        float x = Mathf.Cos((transform.localEulerAngles.z) * Mathf.Deg2Rad);
        float y = Mathf.Sin((transform.localEulerAngles.z) * Mathf.Deg2Rad);
        
        sensor.AddObservation(new Vector2(x, y));// 2
        sensor.AddObservation(unitController.acceleration / 10f);// 3
        sensor.AddObservation(unitController.maxVelocity / 10f);// 4
        sensor.AddObservation(unitController.velocity / 10f);// 6
        sensor.AddObservation(unitController.reloadTime);// 7

        if(unitController.reload > 0f)// 8
            sensor.AddObservation(0f);
        else
            sensor.AddObservation(1f);

        // Obstacles
        sensor.AddObservation(rayProximity.Raycast());// +24 =32
        // Enemies
        sensor.AddObservation(enemyPerception.Perceive());// +6 =38
    }

    // Mask actions that cannot be performed
    public override void CollectDiscreteActionMasks(DiscreteActionMasker actionMasker){
        // Disallow shooting if the laser isn't loaded
        if(unitController.reload > 0f)
        {
            actionMasker.SetMask(2, new int[]{1}); 
        }
    }

    // Handle actions
    public override void OnActionReceived(float[] vectorAction)
    {
        // Thrust forward (branch 0)
        if(Mathf.Abs(vectorAction[0]) == 1f)
            unitController.Thrust();

        // Rotate (branch 1)
        if(Mathf.Abs(vectorAction[1]) > 0)
            unitController.Rotate(vectorAction[1] == 2f ? true : false);

        // Shoot (branch 2)
        if(vectorAction[2] == 1)
        {
            unitController.Shoot();
        }

        // Penalty over time
        AddReward(-.001f);
    }
}
