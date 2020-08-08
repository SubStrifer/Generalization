using System;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    // Ship properties
    private float hp;
    public float maxHp;
    public float acceleration;
    public float maxVelocity;
    public float torque;
    public float maxAngularVelocity;
    public float range;
    public float reloadTime;
    public Rigidbody2D rb;
    public float reload { get; private set; }
    public Vector2 velocity => rb.velocity;

    private bool thrust;
    private Turn rotate;

    public event EventHandler<EventArgs> Damaged;
    public event EventHandler<EventArgs> Destroyed;
    public event EventHandler<EventArgs> Crashed;
    public event EventHandler<EventArgs> EnemyHit;
    public event EventHandler<EventArgs> EnemyNotHit;

    // Start is called before the first frame update
    void Start()
    {
        hp = maxHp;
        reload = reloadTime;
        thrust = false;
        rotate = Turn.No;
    }

    // Update is called once per frame
    void Update()
    {       
        // Limit velocity
        if(rb.velocity.magnitude > maxVelocity)
            rb.velocity = rb.velocity.normalized * maxVelocity;

        // Limit angular velocity
        if (Mathf.Abs(rb.angularVelocity) > maxAngularVelocity)
            rb.angularVelocity = rb.angularVelocity > 0 ? maxAngularVelocity : -maxAngularVelocity;

        // Reload bullet
        if(reload > 0f)
            reload -= Time.deltaTime;
    }

    // Handle physics
    void FixedUpdate()
    {
        // Moving forward
        if(thrust == true)
        {
            rb.AddRelativeForce(Vector2.right * acceleration, ForceMode2D.Force);
            thrust = false;
        }

        // Rotation
        if(rotate != Turn.No)
        {
            rb.AddTorque(rotate == Turn.Left ? -torque : torque, ForceMode2D.Force);
            rotate = Turn.No;
        }
    }

    // When touched an enemy or a wall
    public void OnCollisionEnter2D(Collision2D other)
    {
        hp = 0f;
        Crashed?.Invoke(this, new EventArgs());
    }

    // Reset Unit
    public void Reset()
    {
        hp = maxHp;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    // Thrust forward
    public void Thrust(bool thrust = true)
    {
        this.thrust = thrust;
    }

    // Rotate left or right
    public void Rotate(bool left)
    {
        rotate = left ? Turn.Left : Turn.Right;
    }

    // Shoot
    public void Shoot()
    {
        if(reload > 0f)
        {
            return;
        }

        reload = reloadTime;
        Vector3 startPosition = transform.position;
        Vector3 endPosition = transform.TransformDirection(PolarToCartesian(range, 0f));

        // Debug draw ray
        if (Application.isEditor)
            Debug.DrawRay(startPosition, endPosition, Color.green, 0.02f, true);

        // For each collider detected
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(startPosition, endPosition, range))
        {
            // Continue if detected itself
            if(hit.collider.gameObject == transform.gameObject)
                continue;

            // Return, if hit wall (prevents shooting through walls)
            if (hit.collider.gameObject.CompareTag("Wall"))
            {
                EnemyNotHit?.Invoke(this, new EventArgs());
                return;
            }

            // Daamage the closest unit
            if (hit.collider.gameObject.CompareTag("Unit"))
            {
                hit.collider.gameObject.GetComponent<UnitController>().Damage(1f);
                EnemyHit?.Invoke(this, new EventArgs());
                return;
            }
        }

        // Nothing hit
        EnemyNotHit?.Invoke(this, new EventArgs());
    }

    // Damage this unit
    public void Damage(float hp)
    {
        if(hp <= 0f)
            return;
        
        this.hp -= hp;
        Damaged?.Invoke(this, new EventArgs());

        if(this.hp <= 0f)
        {
            Destroyed?.Invoke(this, new EventArgs());
        }
    }

    //todo move to Maths
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

public enum Turn
{
    No,
    Left,
    Right
}
