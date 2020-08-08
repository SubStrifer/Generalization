using System.Collections.Generic;
using UnityEngine;

public class RayProximity
{
    private Transform transform;
    private Collider2D collider;
    private float rayDistance;
    private float[] rayAngles;
    private Vector3[] rayPositions;
    private string[] detectableTags;
    private float[] valuesBuffer;

    List<RaycastHit2D> hits = new List<RaycastHit2D>();

    // Suggested number of angles: 12/15/18/20
    public RayProximity(Transform transform, float rayDistance, int rayAngles, string[] detectableTags)
    {
        this.transform = transform;
        this.collider = transform.GetComponent<Collider2D>();
        this.rayDistance = rayDistance;
        this.detectableTags = detectableTags;
        this.valuesBuffer = new float[rayAngles * 2];

        // Handle angles
        this.rayAngles = new float[rayAngles];
        rayPositions = new Vector3[rayAngles];

        // Collect collider points
        Vector2[] points = (collider as PolygonCollider2D).points;

        for(int i = 0; i < rayAngles; i++)
        {
            // Calculate angle
            float angle = i * (360f / rayAngles);
            this.rayAngles[i] = angle;

            // Calculate start position
            Vector2 endPosition = transform.position + transform.TransformDirection(PolarToCartesian(1f, angle));

            // Calculate intersection points
            Vector2[] intersections = new Vector2[points.Length];
            bool intersected = false;
            bool linesIntersected = false;
            Vector2 closeA = new Vector2();
            Vector2 closeB = new Vector2();

            for(int j = 0; j < points.Length; j++)
            {
                FindIntersection(Vector2.zero, PolarToCartesian(1f, angle), points[j], points[j + 1 < points.Length ? j + 1 : 0],
                    out linesIntersected, out intersected, out intersections[j], out closeA, out closeB);
                if(!intersected)
                    intersections[j] = new Vector2();
            }

            // Find the furthest point
            Vector2 point = new Vector2();

            foreach (Vector2 p in intersections)
            {
                if(p.magnitude > point.magnitude)
                    point = p;
            }

            rayPositions[i] = point;
        }

    }

    // Find the point of intersection between
    // the lines p1 --> p2 and p3 --> p4.
    private void FindIntersection(
        Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4,
        out bool lines_intersect, out bool segments_intersect,
        out Vector2 intersection,
        out Vector2 close_p1, out Vector2 close_p2)
    {
        // Get the segments' parameters.
        float dx12 = p2.x - p1.x;
        float dy12 = p2.y - p1.y;
        float dx34 = p4.x - p3.x;
        float dy34 = p4.y - p3.y;

        // Solve for t1 and t2
        float denominator = (dy12 * dx34 - dx12 * dy34);

        float t1 =
            ((p1.x - p3.x) * dy34 + (p3.y - p1.y) * dx34)
                / denominator;
        if (float.IsInfinity(t1))
        {
            // The lines are parallel (or close enough to it).
            lines_intersect = false;
            segments_intersect = false;
            intersection = new Vector2(float.NaN, float.NaN);
            close_p1 = new Vector2(float.NaN, float.NaN);
            close_p2 = new Vector2(float.NaN, float.NaN);
            return;
        }
        lines_intersect = true;

        float t2 =
            ((p3.x - p1.x) * dy12 + (p1.y - p3.y) * dx12)
                / -denominator;

        // Find the point of intersection.
        intersection = new Vector2(p1.x + dx12 * t1, p1.y + dy12 * t1);

        // The segments intersect if t1 and t2 are between 0 and 1.
        segments_intersect =
            ((t1 >= 0) && (t1 <= 1) &&
            (t2 >= 0) && (t2 <= 1));

        // Find the closest points on the segments.
        if (t1 < 0)
        {
            t1 = 0;
        }
        else if (t1 > 1)
        {
            t1 = 1;
        }

        if (t2 < 0)
        {
            t2 = 0;
        }
        else if (t2 > 1)
        {
            t2 = 1;
        }

        close_p1 = new Vector2(p1.x + dx12 * t1, p1.y + dy12 * t1);
        close_p2 = new Vector2(p3.x + dx34 * t2, p3.y + dy34 * t2);
    }

    // Returns rayAngles * 2 values
    public float[] Raycast()
    {
        // Set all values to 0
        for(int i = 0; i < rayAngles.Length * 2; i++)
        {
            valuesBuffer[i] = 0f;
        }

        // For each angle
        for(int i = 0; i < rayAngles.Length; i++)
        {
            float angle = rayAngles[i];

            //Vector2 point = PolarToCartesian(rayStart[i], angle);
            Vector3 startPosition = transform.TransformPoint(rayPositions[i]);
            //transform.position + new Vector3(point.x, point.y);

            Vector3 endPosition = transform.TransformDirection(PolarToCartesian(rayDistance, angle));

            // Debug draw ray
            if (Application.isEditor)
                Debug.DrawRay(startPosition, endPosition, Color.blue, 0.01f, true);

            bool any = false;

            // For each collider detected
            foreach (RaycastHit2D hit in Physics2D.RaycastAll(startPosition, endPosition, rayDistance))
            {
                // Continue if detected itself
                if(hit.collider.gameObject == transform.gameObject)
                    continue;

                // For each tag
                for (int j = 0; j < detectableTags.Length; j++)
                {
                    // Store the closest viable object
                    if (hit.collider.gameObject.CompareTag(detectableTags[j]))
                    {
                        valuesBuffer[i * 2] = 1f;
                        valuesBuffer[i * 2 + 1] = hit.distance / rayDistance;
                        any = true;
                        break;
                    }
                }
                // Break if the closest object detected
                if(any)
                    break;
            }
        }

        return valuesBuffer;
    }

    /// <summary>
    /// Converts polar coordinate to cartesian coordinate.
    /// </summary>
    public Vector2 PolarToCartesian(float radius, float angle)
    {
        var x = radius * Mathf.Cos(angle * Mathf.Deg2Rad);
        var y = radius * Mathf.Sin(angle * Mathf.Deg2Rad);
        return new Vector2(x, y);
    }
}
