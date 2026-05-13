using Unity.Netcode;
using UnityEngine;
[RequireComponent(typeof(InputHandler), typeof(Movement), typeof(TheCollider))]
public class Vehicle : NetworkBehaviour
{


    [SerializeField]
    [Tooltip("LayerMask to identify obstacles in the game environment.")]
    LayerMask m_ObstacleLayer;


    [SerializeField] Movement movement;
    [SerializeField] InputHandler playerInput;
    [SerializeField] TheCollider theCollider;





    void Awake()
    {
        Initialize();

    }

    private void Initialize()
    {
        if (!movement) movement = GetComponent<Movement>();
        if (!playerInput) playerInput = GetComponent<InputHandler>();
        if (!theCollider) theCollider = GetComponent<TheCollider>();


    }
    private void LateUpdate()
    {
        if (IsOwner)
        {
            Vector3 inputVector = playerInput.MovementInput;
            MovementServerRPC(inputVector);
        }
    }
    [ServerRpc]
    void MovementServerRPC(Vector2 inputVector)
    {
        movement.Move(inputVector);

    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (NetworkManager.Singleton.IsServer == false)
            return;

        theCollider.Die(collision);
        movement.StopVehicle();

    }




}
