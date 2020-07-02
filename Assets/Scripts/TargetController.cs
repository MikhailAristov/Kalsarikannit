using System.Collections;
using UnityEngine;

public class TargetController : MonoBehaviour {

    public SpriteRenderer mySprite;
    public AudioSource myBeeper;
    public TileController CurrentTile;
    private Transform CameraTransform;

    // How often, in seconds, does the target add to stress of its tile
    public const float STRESS_INCREASE_INTERVAL = 2.0f;
    private const float PULSE_EXPANSION_FACTOR = 1.5f;
    public const float STRENGTH_ELIMINATION_THRESHOLD = 0.3f;
    public const float INITIAL_SCALING_ON_SPAWN = 0.25f;

    public float NextStressIncreaseIn = STRESS_INCREASE_INTERVAL;
    [Range(0f, 1f)]
    public float Strength = 1f;

    private bool isPulsing = false;
    private Vector2 initSpriteScale, pulsingSpriteScale;

    private float DistToCamera {
        get {
            return Vector2.Distance(transform.position, CameraTransform.position);
        }
    }

    // Use this for initialization
    void Start() {
        initSpriteScale = mySprite.transform.localScale;
        pulsingSpriteScale = initSpriteScale * PULSE_EXPANSION_FACTOR;
        mySprite.transform.localScale = INITIAL_SCALING_ON_SPAWN * Vector3.one;
        CameraTransform = Camera.main.transform;
    }

    // Update is called once per frame
    void Update() {
        if(Strength < STRENGTH_ELIMINATION_THRESHOLD) {
            GameObject.FindGameObjectWithTag("GameController").SendMessage("OnStressFactorEliminated", this);
            Strength = 1f;
        }
    }

    public void PlaceOnTile(TileController target) {
        Debug.Assert(target != null);
        transform.parent = target.myCenter;
        transform.localPosition = Vector2.zero;
        CurrentTile = target;
        NextStressIncreaseIn = STRESS_INCREASE_INTERVAL;
        Strength = 1f;
        // Apply random rotation
        transform.RotateAround(transform.position, Vector3.forward, UnityEngine.Random.value * 360f);
    }

    public void MoveToPool(Transform pool) {
        transform.parent = pool;
        transform.localPosition = Vector2.zero;
        transform.localScale = Vector3.one;
        mySprite.transform.localScale = INITIAL_SCALING_ON_SPAWN * Vector3.one;
        CurrentTile = null;
    }

    void FixedUpdate() {
        if(CurrentTile != null) {
            NextStressIncreaseIn -= Time.fixedDeltaTime;
            if(NextStressIncreaseIn < 0) {
                NextStressIncreaseIn += STRESS_INCREASE_INTERVAL;
                if(!isPulsing) {
                    isPulsing = true;
                    StartCoroutine(Pulse());
                }
            }
        }
        // Update beeper sound based on proximity to the camera
        myBeeper.volume = 1f / (1f + 0.5f * DistToCamera * DistToCamera);
    }

    private void IncreaseStress() {
        if(CurrentTile != null && !Util.Approx(CurrentTile.StressLevel, 1f)) {
            CurrentTile.StressLevel = Mathf.Lerp(CurrentTile.StressLevel, 1f, 0.1f * STRESS_INCREASE_INTERVAL);
        }
    }

    private IEnumerator Pulse() {
        float ExpansionStopAt = Time.timeSinceLevelLoad + STRESS_INCREASE_INTERVAL / 3f;
        float ContractionStopAt = Time.timeSinceLevelLoad + STRESS_INCREASE_INTERVAL * 2f / 3f;
        // Play a beep
        myBeeper.Play();
        // Expand
        while(Time.timeSinceLevelLoad < ExpansionStopAt && Vector2.Distance(mySprite.transform.localScale, pulsingSpriteScale) > Util.NEGLIGIBLE) {
            mySprite.transform.localScale = Vector2.Lerp(mySprite.transform.localScale, pulsingSpriteScale, 5f * Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }
        // Raise stress level
        IncreaseStress();
        // Contract
        while(Time.timeSinceLevelLoad < ContractionStopAt && Vector2.Distance(mySprite.transform.localScale, initSpriteScale) > Util.NEGLIGIBLE) {
            mySprite.transform.localScale = Vector2.Lerp(mySprite.transform.localScale, initSpriteScale, 3f * Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }
        isPulsing = false;
    }

    public void Reduce(float deltaTime) {
        Strength = Mathf.Lerp(Strength, 0f, 0.5f * deltaTime);
        transform.localScale = new Vector3(Strength, Strength, 1f);
    }
}
