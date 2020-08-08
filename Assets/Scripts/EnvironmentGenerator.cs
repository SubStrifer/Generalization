using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEngine;

/// <summary>Procedurally generates walls and agents.</summary>
public class EnvironmentGenerator : MonoBehaviour
{
    [Header("Environment")]
    public Vector2Int minSize;
    public Vector2Int maxSize;
    public int minAgents;
    public int maxAgents;
    public float wallsDensity;
    public int regeneratePeriod;
    public new GameObject camera;

    [Header("Prefabs")]
    public GameObject agentPrefab;
    public GameObject wallPrefab;

    // Static spawn positions, so agents can read it
    public static List<Vector3> spawnPositions;
    public static int agentsNumber;

    private int lastSteps;
    private const float wallSize = 4f;
    private GameObject environment;
    private Vector2Int size;
    private bool[,] walls;

    // Start is called before the first frame update
    void Start()
    {
        lastSteps = regeneratePeriod;
        spawnPositions = new List<Vector3>();
        GenerateEnvironment();
    }

    // Update is called once per frame
    void Update()
    {
        // Regenerate the environment
        if(Input.GetKeyUp(KeyCode.R))
            GenerateEnvironment();
        
        int steps = Academy.Instance.StepCount;
        if(regeneratePeriod > 0 && steps > lastSteps)
        {
            GenerateEnvironment();
            lastSteps += regeneratePeriod;
        }
    }

    void GenerateEnvironment()
    {
        // Destroy previous environment if exists.
        if(environment)
        {
            DestroyImmediate(environment);
            environment = null;
            spawnPositions.Clear();
        }

        // Randomize map size
        size = new Vector2Int(Random.Range(minSize.x, maxSize.x + 1), Random.Range(minSize.y, maxSize.y + 1));
        // Generate environment GameObject
        environment = new GameObject("Environment");

        // Generate border walls
        for(int x = 1; x < size.x + 1; x++)
        for(int y = 0; y < size.y + 2; y += size.y + 1)
            Instantiate(wallPrefab, new Vector3(x * wallSize, y * wallSize), Quaternion.identity, environment.transform);
        for(int x = 0; x < size.x + 2; x += size.x + 1)
        for(int y = 0; y < size.y + 2; y++)
            Instantiate(wallPrefab, new Vector3(x * wallSize, y * wallSize), Quaternion.identity, environment.transform);

        // Generate walls at every position with probability wallsDensity
        walls = new bool[size.x, size.y];
        for(int x = 0; x < size.x; x++)
        for(int y = 0; y < size.y; y++)
            walls[x, y] = Random.value < wallsDensity;

        // Ensure connectivity between all empty spaces
        bool[,] visited = new bool[size.x, size.y];
        for(int x = 0; x < size.x; x++)
        for(int y = 0; y < size.y; y++)
        {
            visited[x, y] = false;
        }
        
        List<HashSet<Vector2Int>> areas = new List<HashSet<Vector2Int>>();

        for(int x = 0; x < size.x; x++)
        for(int y = 0; y < size.y; y++)
        {
            if(!visited[x, y] && !walls[x, y])
            {
                HashSet<Vector2Int> area = BasicFill(new Vector2Int(x, y), ref visited);
                areas.Add(area);
            }
        }
        EnsureConnectivity(areas);

        // Place walls and save spawn positions
        for(int x = 0; x < size.x; x++)
        for(int y = 0; y < size.y; y++)
        {
            if(walls[x, y] == false)
                spawnPositions.Add(new Vector3(wallSize + x * wallSize, wallSize + y * wallSize));
            else
                Instantiate(wallPrefab, new Vector3(wallSize + x * wallSize, wallSize + y * wallSize),
                    Quaternion.identity, environment.transform);
        }

        // Spawn agents
        agentsNumber = Random.Range(minAgents, maxAgents + 1);
        for(int i = 0; i < agentsNumber; i++)
        {
            Vector3 position = spawnPositions[Random.Range(0, spawnPositions.Count)];
            GameObject agent = Instantiate(agentPrefab, position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)), environment.transform);
            agent.GetComponent<BehaviorParameters>().TeamId = i;
            // Remove spawn position to prevent placing agents on each other
            spawnPositions.Remove(position);
        }

        // Center the environment
        camera.transform.position = new Vector3(((size.x + 1) * wallSize) / 2f, ((size.y + 1) * wallSize) / 2f, -10f);
    }

    // Generate a set of position using flood fill on empty space
    private HashSet<Vector2Int> BasicFill(Vector2Int position, ref bool[,] visited)
    {
        HashSet<Vector2Int> area = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(position);

        while(queue.Count > 0)
        {
            position = queue.Dequeue();

            if(!walls[position.x, position.y] && !area.Contains(position) && !visited[position.x, position.y])
            {
                area.Add(position);
                visited[position.x, position.y] = true;
                // Enqueue neighbors
                if(position.x > 0)
                    if(!walls[position.x - 1, position.y] &&
                        !area.Contains(new Vector2Int(position.x - 1, position.y)))
                            queue.Enqueue(new Vector2Int(position.x - 1, position.y));
                if(position.y > 0)
                    if(!walls[position.x, position.y - 1] &&
                        !area.Contains(new Vector2Int(position.x, position.y - 1)))
                            queue.Enqueue(new Vector2Int(position.x, position.y - 1));
                if(position.x < size.x - 1)
                    if(!walls[position.x + 1, position.y] &&
                        !area.Contains(new Vector2Int(position.x + 1, position.y)))
                            queue.Enqueue(new Vector2Int(position.x + 1, position.y));
                if(position.y < size.y - 1)
                    if(!walls[position.x, position.y + 1] &&
                        !area.Contains(new Vector2Int(position.x, position.y + 1)))
                            queue.Enqueue(new Vector2Int(position.x, position.y + 1));
            }
        }     

        return area;
    }

    private void EnsureConnectivity(List<HashSet<Vector2Int>> areas)
    {
        // Determine the largest
        HashSet<Vector2Int> largest = areas.First();
        foreach(HashSet<Vector2Int> area in areas)
        {
            if(area.Count > largest.Count)
                largest = area;
        }

        // Create dijkstra nodes
        DijkstraNode[,] dijkstra = new DijkstraNode[size.x, size.y];
        for(int x = 0; x < size.x; x++)
        for(int y = 0; y < size.y; y++)
        {
            dijkstra[x, y] = new DijkstraNode(walls[x, y] ? 2 : 1);
        }

        // Build a Dijkstra graph from the largest node
        List<Vector2Int> queue = new List<Vector2Int>();
        Vector2Int start = largest.RandomElement();
        queue.Add(start);
        int count = 0;
        while(queue.Count > 0)
        {
            Vector2Int position = queue.First();     
            DijkstraNode node = dijkstra[position.x, position.y];

            if(position.x > 0)
            {
                DijkstraNode child = dijkstra[position.x - 1, position.y];
                if(!child.visited)
                {
                    if(child.minCostToStart == 0 ||
                        node.minCostToStart + child.cost < child.minCostToStart)
                        {
                            child.minCostToStart = node.minCostToStart + child.cost;
                            child.nearestToStart = position;
                            if(!queue.Contains(new Vector2Int(position.x - 1, position.y)))
                                queue.Add(new Vector2Int(position.x - 1, position.y));
                        }
                }
            }

            if(position.y > 0)
            {
                DijkstraNode child = dijkstra[position.x, position.y - 1];
                if(!child.visited)
                {
                    if(child.minCostToStart == 0 ||
                        node.minCostToStart + child.cost < child.minCostToStart)
                        {
                            child.minCostToStart = node.minCostToStart + child.cost;
                            child.nearestToStart = position;
                            if(!queue.Contains(new Vector2Int(position.x, position.y - 1)))
                                queue.Add(new Vector2Int(position.x, position.y - 1));
                        }
                }
            }

            if(position.x + 1 < size.x)
            {
                DijkstraNode child = dijkstra[position.x + 1, position.y];
                if(!child.visited)
                {
                    if(child.minCostToStart == 0 ||
                        node.minCostToStart + child.cost < child.minCostToStart)
                        {
                            child.minCostToStart = node.minCostToStart + child.cost;
                            child.nearestToStart = position;
                            if(!queue.Contains(new Vector2Int(position.x + 1, position.y)))
                                queue.Add(new Vector2Int(position.x + 1, position.y));
                        }
                }
            }

            if(position.y + 1 < size.y)
            {
                DijkstraNode child = dijkstra[position.x, position.y + 1];
                if(!child.visited)
                {
                    if(child.minCostToStart == 0 ||
                        node.minCostToStart + child.cost < child.minCostToStart)
                        {
                            child.minCostToStart = node.minCostToStart + child.cost;
                            child.nearestToStart = position;
                            if(!queue.Contains(new Vector2Int(position.x, position.y + 1)))
                                queue.Add(new Vector2Int(position.x, position.y + 1));
                        }
                }
            }
            queue.Remove(position);
            node.visited = true;
            count++;
        }

        // Connect all
        areas.Remove(largest);
        while(areas.Count > 0)
        {
            HashSet<Vector2Int> current = areas.First();
            Vector2Int position = current.RandomElement();
            DijkstraNode node = dijkstra[position.x, position.y];
            // Check if dijkstra algorithm reached this cell
            // If not - it's separated by walls
            if(node.nearestToStart == Vector2Int.zero)
            {
                Debug.LogWarning("Area cannot be connected! " + position);
                areas.Remove(current);
                continue;
            }
            // Walk position by position using dijsktra graph
            // Remove any walls along the way
            while(true)
            {
                position = node.nearestToStart;
                node = dijkstra[position.x, position.y];
                // Check if reached the largest
                if(largest.Contains(position))
                {
                    areas.Remove(current);
                    break;
                }
                // Remove wall
                walls[position.x, position.y] = false;
            }

        }
    }

    private class DijkstraNode
    {
        public bool visited;
        public Vector2Int nearestToStart;
        public int cost;
        public float minCostToStart;

        public DijkstraNode(int cost)
        {
            this.cost = cost;
            nearestToStart = Vector2Int.zero;
            minCostToStart = 0;
            visited = false;
        }
    }
    
}


