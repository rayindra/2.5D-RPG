using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController3D : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private Animator anim;
    [SerializeField] private Transform model; // model 3D yang diputar
    [SerializeField] private LayerMask grassLayer;

    [SerializeField] private int minStepsToEncounter = 3;
    [SerializeField] private int maxStepsToEncounter = 8;

    private PlayerControls playerControls;
    private Rigidbody rb;

    private Vector3 movement;

    private bool movingInGrass;
    private float stepTimer;
    private int stepsInGrass;
    private int stepsToEncounter;

    private const string IS_WALK_PARAM = "IsWalk";
    private const string BATTLE_SCENE = "BattleScene";
    private const float TIME_PER_STEP = 0.5f;

    private void Awake()
    {
        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        CalculateStepsToNextEncounter();
    }

    void Update()
{
    Vector2 input = playerControls.Player.Move.ReadValue<Vector2>();

    float x = input.x;
    float z = input.y;

    movement = new Vector3(x, 0, z).normalized;

    anim.SetBool("IsWalk", movement != Vector3.zero);

    // ROTASI KE ARAH JALAN
    if (movement != Vector3.zero)
    {
        Quaternion targetRotation =
            Quaternion.LookRotation(movement);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            10f * Time.deltaTime
        );
    }
}

    private void FixedUpdate()
    {
        rb.MovePosition(
            transform.position +
            movement * speed * Time.fixedDeltaTime
        );

        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            1f,
            grassLayer
        );

        movingInGrass =
            colliders.Length > 0 &&
            movement != Vector3.zero;

        if (movingInGrass)
        {
            stepTimer += Time.fixedDeltaTime;

            if (stepTimer >= TIME_PER_STEP)
            {
                stepsInGrass++;
                stepTimer = 0;

                if (stepsInGrass >= stepsToEncounter)
                {
                    SceneManager.LoadScene(BATTLE_SCENE);
                }
            }
        }
    }

    private void CalculateStepsToNextEncounter()
    {
        stepsInGrass = 0;

        stepsToEncounter = Random.Range(
            minStepsToEncounter,
            maxStepsToEncounter
        );
    }
}