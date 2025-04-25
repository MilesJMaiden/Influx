// Agent.cs
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public enum AgentState
{
    Idle,
    Wander,
    MoveToComputer,
    Repair,
    MoveToRockBin,
    PickRock,
    MoveToTable,
    RefineRock,
    MoveToBin,
    DepositFuel
}

[RequireComponent(typeof(NavMeshAgent))]
public class Agent : MonoBehaviour
{
    public static Agent SelectedAgent { get; private set; }

    [Header("Movement & FSM")]
    public float idleDuration = 2f;
    public float wanderRadius = 10f;

    [Header("Repair Settings")]
    const float detectRadius = 12f, repairDistance = 1.5f, repairRate = 0.1f;
    private Computer repairTarget;

    [Header("Fuel Settings")]
    [Tooltip("UI Slider under this agent for its own fuel level.")]
    public Slider fuelSlider;
    const float fuelDepleteRate = 0.01f;
    const float fuelRefuelAmount = 0.25f;
    // add at the top, with your other consts:
    const float rockPickupRadius = 3f;
    const float tableProcessRadius = 3f;
    const float binDepositRadius = 3f;

    public bool IsCarryingRock { get; private set; }
    public bool IsCarryingRefined { get; private set; }

    private NavMeshAgent navAgent;
    private AgentState currentState;
    private float stateTimer;

    private GameObject selectedIndicator, highlightIndicator;
    private Coroutine refineRoutine;

    private GameObject _refinedRockObject;


    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();

        selectedIndicator = transform.Find("SelectedCylinder")?.gameObject;
        highlightIndicator = transform.Find("HighlightCylinder")?.gameObject;
        selectedIndicator?.SetActive(false);
        highlightIndicator?.SetActive(false);

        // cache refined‐rock child and hide it
        _refinedRockObject = transform.Find("RefinedRock")?.gameObject;
        if (_refinedRockObject != null)
            _refinedRockObject.SetActive(false);

        // Initialize own fuel slider...
        // Hide rock visuals
        transform.Find("Rock")?.gameObject.SetActive(false);
    }


    private void Start()
    {
        StartCoroutine(EnableWhenNavReady());
    }

    private IEnumerator EnableWhenNavReady()
    {
        while (!navAgent.isOnNavMesh) yield return null;
        TransitionTo(AgentState.Idle);
    }

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.Q) && SelectedAgent != null)
        {
            SelectedAgent.Deselect();
            return;  // bail out of any other per‑frame logic
        }
        // deplete own fuel
        if (fuelSlider)
            fuelSlider.value = Mathf.Max(0f, fuelSlider.value - fuelDepleteRate * Time.deltaTime);

        // 1) Repair pipeline
        if (currentState == AgentState.MoveToComputer || currentState == AgentState.Repair)
        {
            RepairBehavior();
            return;
        }

        // 2) Rock→Table→Bin pipeline
        if (currentState == AgentState.MoveToRockBin ||
            currentState == AgentState.MoveToTable ||
            currentState == AgentState.RefineRock ||
            currentState == AgentState.MoveToBin)
        {
            RockPipelineBehavior();
            return;
        }

        // 3) Player override
        if (SelectedAgent == this)
        {
            HandleUserInput();
            return;
        }

        // 4) Auto‑repair
        if (repairTarget == null)
            TryAutoRepair();
        if (repairTarget != null)
            return;

        // 5) Auto‑refuel if below 75% and not carrying anything
        bool needFuel = fuelSlider && fuelSlider.value < 0.75f;
        if (needFuel && !IsCarryingRock && !IsCarryingRefined && currentState == AgentState.Idle)
        {
            var rockBin = FindObjectOfType<RockBin>();
            if (rockBin != null)
                CommandPickupRock(rockBin);
            return;
        }

        // 6) Normal FSM
        stateTimer -= Time.deltaTime;
        if (currentState == AgentState.Idle && stateTimer <= 0f)
            TransitionTo(AgentState.Wander);
        else if (currentState == AgentState.Wander &&
                 !navAgent.pathPending &&
                 navAgent.remainingDistance <= navAgent.stoppingDistance)
            TransitionTo(AgentState.Idle);
    }

    // —— Repair Logic ——

    private void TryAutoRepair()
    {
        Computer closest = null;
        float bestDist = detectRadius;
        foreach (var c in FindObjectsOfType<Computer>())
        {
            if (c.IsUnderRepair) continue;
            var s = c.GetComponentInChildren<Slider>();
            if (s == null || s.value >= 0.66f) continue;
            float d = Vector3.Distance(transform.position, c.transform.position);
            if (d < bestDist) { bestDist = d; closest = c; }
        }
        if (closest != null)
            CommandRepair(closest);
    }

    private void RepairBehavior()
    {
        float dist = Vector3.Distance(transform.position, repairTarget.transform.position);
        if (currentState == AgentState.MoveToComputer)
        {
            if (dist <= repairDistance)
            {
                navAgent.ResetPath();
                currentState = AgentState.Repair;
                repairTarget.NotifyRepairStart();
            }
        }
        else // Repair
        {
            var slider = repairTarget.GetComponentInChildren<Slider>();
            if (slider == null) { EndRepair(); return; }
            slider.value = Mathf.Min(1f, slider.value + repairRate * Time.deltaTime);
            if (slider.value >= 1f)
                EndRepair();
        }
    }

    public void CommandRepair(Computer c)
    {
        repairTarget = c;
        currentState = AgentState.MoveToComputer;
        navAgent.SetDestination(c.transform.position);
    }

    private void EndRepair()
    {
        repairTarget.NotifyRepairEnd();
        repairTarget = null;
        TransitionTo(AgentState.Idle);
    }

    // —— Rock→Table→Bin Pipeline ——

    private void RockPipelineBehavior()
    {
        switch (currentState)
        {
            case AgentState.MoveToRockBin:
                {
                    var rockBin = FindObjectOfType<RockBin>();
                    if (rockBin == null)
                    {
                        Debug.LogWarning("Agent: No RockBin found in scene.");
                        return;
                    }

                    float d = Vector3.Distance(transform.position, rockBin.transform.position);
                    if (d <= rockPickupRadius)
                    {
                        // pick up rock
                        IsCarryingRock = true;
                        transform.Find("Rock")?.gameObject.SetActive(true);

                        // if this agent is selected, stay idle
                        if (SelectedAgent == this)
                        {
                            TransitionTo(AgentState.Idle);
                        }
                        else
                        {
                            // move to table
                            currentState = AgentState.MoveToTable;
                            var table = FindObjectOfType<Table>();
                            if (table != null)
                                navAgent.SetDestination(table.transform.position);
                            else
                                Debug.LogWarning("Agent: No Table found to deliver rock to.");
                        }
                    }
                    break;
                }

            case AgentState.MoveToTable:
                {
                    var table = FindObjectOfType<Table>();
                    if (table == null)
                    {
                        Debug.LogWarning("Agent: No Table found in scene.");
                        return;
                    }

                    float d = Vector3.Distance(transform.position, table.transform.position);
                    if (d <= tableProcessRadius)
                    {
                        // drop agent's rock
                        transform.Find("Rock")?.gameObject.SetActive(false);
                        IsCarryingRock = false;

                        // put rock on table
                        table.transform.Find("Rock")?.gameObject.SetActive(true);

                        // enable & reset table slider
                        var tableSlider = table.GetComponentInChildren<Slider>();
                        if (tableSlider != null)
                        {
                            tableSlider.value = 0f;
                            tableSlider.gameObject.SetActive(true);

                            // **always** begin refining
                            currentState = AgentState.RefineRock;
                            if (refineRoutine != null) StopCoroutine(refineRoutine);
                            refineRoutine = StartCoroutine(RefineCoroutine(tableSlider, table));
                        }
                        else
                        {
                            Debug.LogWarning("Agent: Table has no Slider to refine rock.");
                        }
                    }
                    break;
                }


            case AgentState.MoveToBin:
                {
                    var bin = FindObjectOfType<Bin>();
                    if (bin == null) break;

                    float d = Vector3.Distance(transform.position, bin.transform.position);
                    if (d <= binDepositRadius)
                    {
                        // Hide the agent's refined rock
                        transform.Find("RefinedRock")?.gameObject.SetActive(false);
                        IsCarryingRefined = false;

                        // Add fuel to the bin
                        bin.AddFuel(fuelRefuelAmount);

                        // selected agents go Idle, others resume Wandering
                        if (SelectedAgent == this)
                            TransitionTo(AgentState.Idle);
                        else
                            TransitionTo(AgentState.Wander);
                    }
                    break;
                }
        }
    }

    public void CommandPickupRock(RockBin bin)
    {
        currentState = AgentState.MoveToRockBin;
        navAgent.SetDestination(bin.transform.position);
    }

    private IEnumerator RefineCoroutine(Slider tableSlider, Table table)
    {
        float elapsed = 0f;
        const float duration = 4f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            tableSlider.value = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        // hide the table’s UI & its rock model
        tableSlider.gameObject.SetActive(false);
        table.transform.Find("Rock")?.gameObject.SetActive(false);

        // enable the agent’s RefinedRock child
        if (_refinedRockObject != null)
            _refinedRockObject.SetActive(true);
        IsCarryingRefined = true;

        // now head back to bin
        TransitionTo(AgentState.MoveToBin);
        var bin = FindObjectOfType<Bin>();
        if (bin != null)
            navAgent.SetDestination(bin.transform.position);
    }


    // —— Mouse Hover & Cursor ——

    private void OnMouseEnter()
    {
        if (SelectedAgent != this)
        {
            highlightIndicator?.SetActive(true);
            UICursorManager.Instance?.SetHoverCursor();
        }
    }

    private void OnMouseExit()
    {
        if (SelectedAgent != this)
        {
            highlightIndicator?.SetActive(false);
            UICursorManager.Instance?.ResetCursorImage();
        }
    }

    // —— Selection ——

    private void OnMouseDown()
    {
        if (SelectedAgent == this) return;
        SelectedAgent?.Deselect();
        Select();
    }

    private void Select()
    {
        SelectedAgent = this;
        selectedIndicator?.SetActive(true);
        highlightIndicator?.SetActive(false);
        navAgent.ResetPath();
        UICursorManager.Instance?.ResetCursorImage();
    }

    public void Deselect()
    {
        selectedIndicator?.SetActive(false);
        if (SelectedAgent == this)
        {
            SelectedAgent = null;
            TransitionTo(AgentState.Idle);
        }
    }

    // —— Player Command for Bin Deposit ——

    public void CommandDeposit(Bin b)
    {
        currentState = AgentState.MoveToBin;
        navAgent.SetDestination(b.transform.position);
    }

    // —— Player Input when Selected ——

    // —— Player Input when Selected ——
    private void HandleUserInput()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Deselect();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 100f))
            {
                // floor click
                if (hit.collider.CompareTag("Floor"))
                {
                    navAgent.SetDestination(hit.point);
                    repairTarget = null;
                }
                // computer click
                else if (hit.collider.GetComponentInParent<Computer>() is Computer c)
                {
                    CommandRepair(c);
                }
                // rock-bin click
                else if (hit.collider.GetComponentInParent<RockBin>() is RockBin rb)
                {
                    CommandPickupRock(rb);
                }
                // table click
                else if (hit.collider.GetComponentInParent<Table>() is Table tbl)
                {
                    // only navigate to table if we're carrying an unrefined rock
                    if (IsCarryingRock && !IsCarryingRefined)
                        CommandDeliverToTable(tbl);
                }
                // bin click
                else if (hit.collider.GetComponentInParent<Bin>() is Bin bin)
                {
                    CommandDeposit(bin);
                }
            }
        }
    }

    /// <summary>
    /// Route via MoveToTable state so Table logic will run on arrival.
    /// </summary>
    public void CommandDeliverToTable(Table t)
    {
        currentState = AgentState.MoveToTable;
        navAgent.SetDestination(t.transform.position);
    }

    // —— FSM Helpers ——

    private void TransitionTo(AgentState state)
    {
        currentState = state;
        switch (state)
        {
            case AgentState.Idle:
                stateTimer = idleDuration;
                navAgent.ResetPath();
                break;
            case AgentState.Wander:
                var rnd = Random.insideUnitSphere * wanderRadius + transform.position;
                if (NavMesh.SamplePosition(rnd, out var hit, wanderRadius, NavMesh.AllAreas))
                    navAgent.SetDestination(hit.position);
                break;
            case AgentState.MoveToRockBin:
                var rockBin = FindObjectOfType<RockBin>();
                if (rockBin != null)
                    navAgent.SetDestination(rockBin.transform.position);
                break;
            case AgentState.MoveToTable:
                var table = GameObject.FindWithTag("Table");
                if (table != null)
                    navAgent.SetDestination(table.transform.position);
                break;
            case AgentState.MoveToBin:
                var bin = FindObjectOfType<Bin>();
                if (bin != null)
                    navAgent.SetDestination(bin.transform.position);
                break;
        }
    }
}
