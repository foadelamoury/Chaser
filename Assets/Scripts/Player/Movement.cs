using UnityEngine;

// Assuming these interfaces exist based on your request. 
// If not, simply define them as: public interface IMovable { void GoForward(); void Reverse(); } etc.

[RequireComponent(typeof(Rigidbody2D)), RequireComponent(typeof(PolygonCollider2D))]
public class Movement : MonoBehaviour, IMovable, ITurnable
{
    [Header("Car Handling")]
    [SerializeField]
    float maxSpeed = 7.0f;
    [SerializeField]
    float acceleration = 5.0f;
    [SerializeField]
    float turnSpeed = 150.0f;

    float currentSpeed = 0f;
    Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Moves the car based on player input
    /// </summary>
    /// <param name="movementInput">To move forward and backwards and to steer the car</param> 
    public void Move(Vector2 movementInput)
    {
        if (movementInput.y > 0)
        {
            GoForward();
        }
        else if (movementInput.y < 0)
        {
            Reverse();
        }
        
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, acceleration * Time.deltaTime);
        }


        if (Mathf.Abs(currentSpeed) > 0.1f)
        {
            if (movementInput.x < 0) TurnLeft();

            else if (movementInput.x > 0) TurnRight();

        }

        // using transform.up to move the car in the direction it is facing 
        rb.linearVelocity = (Vector2)transform.up * currentSpeed;
    }

    public void StopVehicle()
    {
        currentSpeed = 0f;
        rb.linearVelocity = Vector2.zero;
        //rb.rotation = 0f;
        //transforms.rotation = Quaternion.identity;
    }



    #region Iturnable
    public void TurnLeft()
    {
        float direction;
        if (currentSpeed > 0)
            direction = 1;
        else
            direction = -1;

        float rotationAmount = turnSpeed * Time.deltaTime * direction;
        rb.MoveRotation(rb.rotation + rotationAmount);
    }

    public  void TurnRight()
    {
        float direction;


        if (currentSpeed > 0)
            direction = 1;
        else
            direction = -1;

        float rotationAmount = -turnSpeed * Time.deltaTime * direction;
        rb.MoveRotation(rb.rotation + rotationAmount);
    }

    #endregion
    #region Imovable
    public  void GoForward()
    {
        currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.deltaTime);
    }

    public  void Reverse()
    {
        currentSpeed = Mathf.MoveTowards(currentSpeed, -maxSpeed, acceleration * Time.deltaTime);
    }
    #endregion

}