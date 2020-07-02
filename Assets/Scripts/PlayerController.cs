using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour {

    public const float LOW_STRESS_THRESHOLD = -0.5f;
    public const float HIGH_STRESS_THRESHOLD = 0.8f;
    private const float PULSE_EXPANSION_FACTOR = 1.5f;
    private const float PULSE_DURATION = 0.5f;
    private const float SINGLE_ROTATION_DURATION = 1f;
    private const float STEP_DURATION = 0.5f;
    private const float HOMEWARD_STEP_DURATION = 0.35f;
    private const float RUNNING_STEP_DURATION = 0.2f;

    [Range(-1f, 1f)]
    public float StressLevel = LOW_STRESS_THRESHOLD;

    public BoardController myBoard;
    public Transform mySprite;
    public SpriteRenderer[] myEyes = new SpriteRenderer[2];
    public AudioSource[] myHeartBeat = new AudioSource[2];
    public AudioSource Noise;
    private float NoiseLevel;
    public TileController CurrentTile;
    public TileController TargetTile;
    private List<TileController> PathToTarget;
    public LineRenderer PathDisplay;

    private Vector2 initSpriteScale, pulsingSpriteScale;

    private bool isSleeping;
    private bool isFleeing;
    private bool isMoving;
    private bool emergencyStop;

    public TargetController AdjacentStressFactor;
    public bool ProcessingStressFactor {
        get {
            return (!isMoving && AdjacentStressFactor != null && AdjacentStressFactor.CurrentTile == CurrentTile);
        }
    }

    public bool HasMoved = false;
    private float LastActivityTimestamp;

    // Use this for initialization
    void Start() {
        myBoard = GameObject.FindGameObjectWithTag("GameController").GetComponent<BoardController>();
        initSpriteScale = mySprite.transform.localScale;
        pulsingSpriteScale = initSpriteScale * PULSE_EXPANSION_FACTOR;
        StartCoroutine(PulseWithStress());
        PathToTarget = new List<TileController>();
        UpdateActivityTimestamp();
    }

    // Update is called once per frame
    void Update() {
        if(myBoard.ExpoMode && HasMoved) {
            if(isMoving || isFleeing) {
                UpdateActivityTimestamp();
            }
            if(Time.timeSinceLevelLoad - LastActivityTimestamp > BoardController.TIME_BEFORE_GAME_RESET_IN_EXPO_MODE) {
                SceneManager.LoadScene(0); // Re-load the default scene to reset the level
            }
        }

        if(!isFleeing && !isMoving && !isSleeping && myBoard.EffectiveMaxStressFactors < 1 && CurrentTile.CompareTag("Home") && Noise.volume < Util.NEGLIGIBLE) {
            isSleeping = true;
            StartCoroutine(GoToSleep());
        }

        if(!isFleeing && isMoving && !isSleeping && Input.GetMouseButtonUp(1)) {
            emergencyStop = true;
            PathDisplay.enabled = false;
        }

        if(!isFleeing && !isMoving && Input.GetMouseButtonUp(0)) {
            isMoving = true;
            StartCoroutine(FollowPathToTarget());
        }

        if(isFleeing) {
            Vector3 rotationAxis = (transform.position.x < 0) ? Vector3.back : Vector3.forward;
            mySprite.transform.RotateAround(mySprite.transform.position, rotationAxis, 360f / SINGLE_ROTATION_DURATION * Time.deltaTime);
        } else if(!Util.Approx(mySprite.localRotation.eulerAngles.magnitude, 0)) {
            mySprite.transform.localRotation = Quaternion.Lerp(mySprite.transform.localRotation, Quaternion.identity, Time.deltaTime);
        }

        // Adjust noise levels
        if(!Util.Approx(Noise.volume, NoiseLevel)) {
            Noise.volume = Mathf.Lerp(Noise.volume, NoiseLevel, 0.5f * Time.deltaTime);
        }
    }

    void FixedUpdate() {
        if(!Util.Approx(StressLevel, CurrentTile.EffectiveStressLevel)) {
            // More stress is easier than less...
            if(CurrentTile.EffectiveStressLevel < Mathf.Min(StressLevel, 0) && CurrentTile.CompareTag("Home")) {
                StressLevel = Mathf.Lerp(StressLevel, CurrentTile.EffectiveStressLevel, 0.5f * Time.fixedDeltaTime);
            } else if(CurrentTile.EffectiveStressLevel > Mathf.Max(StressLevel, 0)) {
                StressLevel = Mathf.Lerp(StressLevel, CurrentTile.EffectiveStressLevel, 3f * Time.fixedDeltaTime);
            }
        }

        if(!isFleeing && StressLevel > HIGH_STRESS_THRESHOLD) {
            if(isMoving) {
                emergencyStop = true;
            }
            isFleeing = true;
            StartCoroutine(RunHome());
        }

        if(ProcessingStressFactor) {
            AdjacentStressFactor.Reduce(Time.fixedDeltaTime);
        }

        NoiseLevel = (CurrentTile.EffectiveStressLevel < LOW_STRESS_THRESHOLD) ? 0 : (CurrentTile.EffectiveStressLevel - LOW_STRESS_THRESHOLD) / (1f - LOW_STRESS_THRESHOLD);
    }

    public void PlaceOnTile(TileController target) {
        Debug.Assert(target != null);
        transform.parent = target.myCenter;
        transform.localPosition = Vector2.zero;
        CurrentTile = target;
    }

    private IEnumerator PulseWithStress() {
        float lastPulseAt = Time.timeSinceLevelLoad;
        while(true) {
            // Don't pulsate at all on low stress
            yield return new WaitUntil(() => StressLevel > LOW_STRESS_THRESHOLD);
            // Calculate next pulse
            lastPulseAt = Time.timeSinceLevelLoad;
            // Prepare pulse
            float ExpansionStopAt = Time.timeSinceLevelLoad + PULSE_DURATION / 2f;
            float ContractionStopAt = Time.timeSinceLevelLoad + PULSE_DURATION;
            // Play heartbeat
            if(!myHeartBeat[0].isPlaying) {
                myHeartBeat[0].Play();
            } else {
                myHeartBeat[1].Play();
            }
            // Expand
            while(Time.timeSinceLevelLoad < ExpansionStopAt && Vector2.Distance(mySprite.localScale, pulsingSpriteScale) > Util.NEGLIGIBLE) {
                mySprite.localScale = Vector2.Lerp(mySprite.localScale, pulsingSpriteScale, 10f * Time.deltaTime);
                yield return new WaitForEndOfFrame();
            }
            // Contract
            while(Time.timeSinceLevelLoad < ContractionStopAt && Vector2.Distance(mySprite.localScale, initSpriteScale) > Util.NEGLIGIBLE) {
                mySprite.localScale = Vector2.Lerp(mySprite.localScale, initSpriteScale, 5f * Time.deltaTime);
                yield return new WaitForEndOfFrame();
            }
            // Wait for next pulse
            yield return new WaitUntil(() => Time.timeSinceLevelLoad > (lastPulseAt + 4f - 3.5f * StressLevel));
        }
    }

    void OnMouseMovedOverTile(TileController tile) {
        if(!isSleeping && !isFleeing && !isMoving && tile != CurrentTile) {
            TargetTile = tile;
            RecalculatePathToTarget(tile);
            DisplayPathToTarget();
        }
    }

    void OnMouseMovedFromTile(TileController tile) {
        if(!isMoving && TargetTile == tile) {
            TargetTile = null;
            HidePathToTarget();
        }
    }

    // Classic A*: https://en.wikipedia.org/wiki/A*_search_algorithm
    private void RecalculatePathToTarget(TileController goal) {
        List<TileController> closedSet = new List<TileController>();
        List<TileController> openSet = new List<TileController>() { CurrentTile };
        Dictionary<TileController, TileController> cameFrom = new Dictionary<TileController, TileController>();
        Dictionary<TileController, float> gScore = new Dictionary<TileController, float>();
        Dictionary<TileController, float> fScore = new Dictionary<TileController, float>();
        foreach(TileController t in myBoard.AllTiles) {
            gScore[t] = (t == CurrentTile) ? 0f : float.MaxValue;
            fScore[t] = (t == CurrentTile) ? Vector2.Distance(t.transform.localPosition, goal.transform.localPosition) : float.MaxValue;
        }
        while(openSet.Count > 0) {
            // Select lowest fScore for current
            TileController current = openSet[0];
            foreach(TileController t in openSet) {
                if(fScore[t] < fScore[current]) {
                    current = t;
                }
            }
            if(current == goal) {
                PathToTarget.Clear();
                PathToTarget.Add(current);
                while(cameFrom.ContainsKey(current)) {
                    current = cameFrom[current];
                    PathToTarget.Insert(0, current);
                }
                return;
            }

            // Move current node
            openSet.Remove(current);
            closedSet.Add(current);
            // Expand neighbours
            foreach(TileController n in current.myNeighbours) {
                if(closedSet.Contains(n)) {
                    continue;
                }
                float tentativeGScore = gScore[current] + TileController.VERTICAL_SPACING;
                if(!openSet.Contains(n)) {
                    openSet.Add(n);
                } else if(tentativeGScore >= gScore[n]) {
                    continue;
                }
                if(cameFrom.ContainsKey(n)) {
                    cameFrom[n] = current;
                } else {
                    cameFrom.Add(n, current);
                }
                gScore[n] = tentativeGScore;
                fScore[n] = tentativeGScore + Mathf.Max(TileController.VERTICAL_SPACING, Vector2.Distance(n.transform.localPosition, goal.transform.localPosition));
            }
        }
        Debug.LogError("No path to target found!");
    }

    private void DisplayPathToTarget() {
        Debug.Assert(PathToTarget.Count > 1);
        Vector3[] pathPositions = new Vector3[PathToTarget.Count];
        for(int i = 0; i < PathToTarget.Count; i++) {
            pathPositions[i] = PathToTarget[i].transform.position - transform.position;
        }
        // Adapt the line
        PathDisplay.enabled = true;
        PathDisplay.positionCount = PathToTarget.Count;
        PathDisplay.SetPositions(pathPositions);
        UpdateActivityTimestamp();
    }

    private void HidePathToTarget() {
        PathDisplay.enabled = false;
    }

    private IEnumerator FollowPathToTarget() {
        if(PathToTarget.Count > 1 && PathDisplay.enabled) {
            HasMoved = true;
            // Stabilize path while moving
            PathDisplay.transform.SetParent(transform.parent);
            // Jump across tiles
            for(int i = 1; i < PathToTarget.Count; i++) {
                // Clear adjacent stress factor
                AdjacentStressFactor = null;
                // Here we abuse that the homeward tile is always on the coordinates origin point
                bool homeward = PathToTarget[i].transform.position.magnitude < transform.parent.position.magnitude;
                // Update my parent
                transform.SetParent(PathToTarget[i].myCenter);
                CurrentTile = PathToTarget[i];
                // Lerp to parent's local position zero
                Vector2 startingPos = transform.localPosition;
                float lastDistToTarget = float.MaxValue;
                do {
                    lastDistToTarget = transform.localPosition.magnitude;
                    float stepDuration = homeward ? HOMEWARD_STEP_DURATION : STEP_DURATION;
                    transform.Translate(-startingPos / stepDuration * Time.deltaTime);
                    yield return new WaitForEndOfFrame();
                } while(transform.localPosition.magnitude > Util.NEGLIGIBLE && lastDistToTarget > transform.localPosition.magnitude);
                // If emergency stop requested, stop after the current jump
                if(emergencyStop) {
                    break;
                }
            }
        }
        // Grab back the path display and hide it
        PathDisplay.transform.SetParent(transform);
        PathDisplay.transform.localPosition = Vector2.zero;
        PathDisplay.enabled = false;
        TargetTile = null;
        PathToTarget.Clear();
        // Check for stress factors
        AdjacentStressFactor = CurrentTile.myCenter.GetComponentInChildren<TargetController>();
        // Stop moving
        emergencyStop = false;
        isMoving = false;
    }

    private IEnumerator RunHome() {
        StressLevel = 1f;
        // Wait until the character stops moving, if necessary
        yield return new WaitUntil(() => !isMoving);
        // Wait until the character removes the stress factor fully
        if(ProcessingStressFactor) {
            yield return new WaitWhile(() => ProcessingStressFactor);
            // Second breath (only if there are targets still visible)
            if(Random.value < myBoard.StressFactorProgress && CountVisibleTargets() > 1) {//
                StressLevel /= 2;
                isFleeing = false;
                yield break;
            }
        }
        // Calculate path to home but don't display it
        PathDisplay.enabled = false;
        TargetTile = GameObject.FindGameObjectWithTag("Home").GetComponent<TileController>();
        RecalculatePathToTarget(TargetTile);
        // Start co-routine to run home
        if(PathToTarget.Count > 1) {
            // Jump across tiles
            for(int i = 1; i < PathToTarget.Count; i++) {
                // Clear adjacent stress factor
                AdjacentStressFactor = null;
                // Update my parent
                transform.SetParent(PathToTarget[i].myCenter);
                CurrentTile = PathToTarget[i];
                // Lerp to parent's local position zero
                Vector2 startingPos = transform.localPosition;
                float lastDistToTarget = float.MaxValue;
                do {
                    lastDistToTarget = transform.localPosition.magnitude;
                    transform.Translate(-startingPos / RUNNING_STEP_DURATION * Time.deltaTime);
                    yield return new WaitForEndOfFrame();
                } while(transform.localPosition.magnitude > Util.NEGLIGIBLE && lastDistToTarget > transform.localPosition.magnitude);
            }
        }
        StressLevel /= 2;
        isFleeing = false;
    }

    private int CountVisibleTargets() {
        int result = 0;
        GameObject[] tgts = GameObject.FindGameObjectsWithTag("Target");
        if(tgts.Length > 0) {
            for(int i = 0; i < tgts.Length; i++) {
                if(tgts[i].GetComponentInChildren<SpriteRenderer>().isVisible) {
                    result += 1;
                }
            }
        }
        return result;
    }

    private IEnumerator GoToSleep() {
        // Focus camera on player
        Camera.main.GetComponent<CameraController>().FocusOnPlayer();
        // Mute noise
        Noise.volume = 0;
        // Shut the Hex's eyes
        SpriteRenderer lEye = myEyes[0], rEye = myEyes[1];
        Vector3 closedEye = new Vector3(lEye.transform.localScale.x, 0, lEye.transform.localScale.y);
        while(lEye.transform.localScale.y > Util.NEGLIGIBLE * 3 && rEye.transform.localScale.y > Util.NEGLIGIBLE * 3) {
            lEye.transform.localScale = Vector3.Lerp(lEye.transform.localScale, closedEye, 0.1f * Time.deltaTime);
            rEye.transform.localScale = Vector3.Lerp(rEye.transform.localScale, closedEye, 0.1f * Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }
        UpdateActivityTimestamp(-BoardController.TIME_BEFORE_GAME_RESET_IN_EXPO_MODE / 2f);
        myBoard.SendMessage("OnPlayerAsleep");
    }

    private void UpdateActivityTimestamp(float Offset = 0f) {
        LastActivityTimestamp = Time.timeSinceLevelLoad + Offset;
    }
}
