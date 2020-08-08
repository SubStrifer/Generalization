using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TagPerception
{
    private Transform transform;
    private float range;
    private int count;
    private string tag;
    private float[] valuesBuffer;
    protected List<Transform> objects = new List<Transform>();

    public TagPerception(Transform transform, float range, int count, string tag)
    {
        this.transform = transform;
        this.range = range;
        this.count = count;
        this.tag = tag;
        valuesBuffer = new float[count * 2];
        Reset();
    }

    public void Reset()
    {
        // Search for all game objects with tag
        foreach(GameObject go in GameObject.FindGameObjectsWithTag(tag))
        {
            // Skip itself
            if(go != transform.gameObject)
                objects.Add(go.transform);        
        }
    }

    // Returns count * 2 values
    public float[] Perceive()
    {
        // Add info to the buffer
        for(int i = 0; i < count; i++)
        {
            Transform t;
            if(i < objects.Count)
            {
                t = objects.ElementAt(i);
                // Debug line
                if (Application.isEditor)
                    Debug.DrawLine(transform.position, t.position, Color.red, 0.01f);
                // Position
                Vector3 position = t.position - transform.position;
                valuesBuffer[i * 2] = position.x / range;
                valuesBuffer[i * 2 + 1] = position.y / range;
            }
            else// Add 0s if no object detected
            {
                valuesBuffer[i * 2] = 0f;
                valuesBuffer[i * 2 + 1] = 0f;
            }
        }

        return valuesBuffer;
    }
}
