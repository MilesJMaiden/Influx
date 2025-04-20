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

    public bool IsCarryingRock { get; private set; }
    public bool IsCarryingRefined { get; private set; }

    private NavMeshAgent navAgent;
    private AgentState currentState;
    private float stateTimer;

    private GameObject selectedIndicator, highlightIndicator;
    private Coroutine refineRoutine;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();

        selectedIndicator = transform.Find("SelectedCylinder")?.gameObject;
        highlightIndicator = transform.Find("HighlightCylinder")?.gameObject;
        selectedIndicator?.SetActive(false);
        highlightIndicator?.SetActive(false);

        // Initialize own fuel slider
        if (fuelSlider)
        {
            fuelSlider.minValue = 0f;
            fuelSlider.maxValue = 1f;
            fuelSlider.value = 1f;
        }

        // Hide rock visuals
        transform.Find("Rock")?.gameObject.SetActive(false);
        transform.Find("RefinedRock")?.gameObject.SetActive(false);
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
        // cache references
        var rockBin = FindObjectOfType<RockBin>();
        var table = FindObjectOfType<Table>();
        var bin = FindObjectOfType<Bin>();

        switch (currentState)
        {
            case AgentState.MoveToRockBin:
                if (rockBin != null &&
                    Vector3.Distance(transform.position, rockBin.transform.position) <= rockPickupRadius)
                {
                    // pick up rock
                    IsCarryingRock = true;
                    transform.Find("Rock")?.gameObject.SetActive(true);
                    TransitionTo(AgentState.MoveToTable);
                }
                break;

            case AgentState.MoveToTable:
                if (table != null &&
                    Vector3.Distance(transform.position, table.transform.position) <= tableProcessRadius)
                {
                    // arrived at table
                    transform.Find("Rock")?.gameObject.SetActive(false);
                    IsCarryingRock = false;
                    table.transform.Find("Rock")?.gameObject.SetActive(true);

                    var tableSlider = table.GetComponentInChildren<Slider>();
                    if (tableSlider)
                    {
                        tableSlider.value = 0f;
                        tableSlider.gameObject.SetActive(true);
                        currentState = AgentState.RefineRock;
                        refineRoutine = StartCoroutine(RefineCoroutine(tableSlider, table));
                    }
                }
                break;

            case AgentState.MoveToBin:
                if (bin != null &&
                    Vector3.Distance(transform.position, bin.transform.position) <= navAgent.stoppingDistance)
                {
                    // deposit refined rock
                    transform.Find("RefinedRock")?.gameObject.SetActive(false);
                    IsCarryingRefined = false;
                    if (fuelSlider)
                        fuelSlider.value = Mathf.Min(1f, fuelSlider.value + fuelRefuelAmount);
                    TransitionTo(AgentState.Idle);
                }
                break;
        }
    }



    private bool ArrivedAtTag(string tag)
    {
        var obj = GameObject.FindWithTag(tag);
        if (obj == null) return false;
        return Vector3.Distance(transform.position, obj.transform.position) <= navAgent.stoppingDistance;
    }

    public void CommandPickupRock(RockBin bin)
    {
        currentState = AgentState.MoveToRockBin;
        navAgent.SetDestination(bin.transform.position);
    }

    private void BeginMoveToTable()
    {
        currentState = AgentState.MoveToTable;
        var tbl = FindObjectOfType<Table>();
        if (tbl != null)
            navAgent.SetDestination(tbl.transform.position);
    }

    // replace your old RefineCoroutine(...) with this signature & body:
    private IEnumerator RefineCoroutine(Slider tableSlider, Table table)
    {
        float elapsed = 0f, duration = 4f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            tableSlider.value = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        // finish refining
        tableSlider.gameObject.SetActive(false);
        table.transform.Find("Rock")?.gameObject.SetActive(false);

        transform.Find("RefinedRock")?.gameObject.SetActive(true);
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
                if (hit.collider.CompareTag("Floor"))
                {
                    navAgent.SetDestination(hit.point);
                    repairTarget = null;
                }
                else if (hit.collider.GetComponentInParent<Computer>() is Computer c)
                {
                    CommandRepair(c);
                }
                else if (hit.collider.GetComponentInParent<RockBin>() is RockBin rb)
                {
                    CommandPickupRock(rb);
                }
                else if (hit.collider.GetComponentInParent<Table>() is Table tbl)
                {
                    CommandDeliverToTable(tbl);
                }
                else if (hit.collider.GetComponentInParent<Bin>() is Bin bin)
                {
                    CommandDeposit(bin);
                }
            }
        }
    }

    public void CommandDeliverToTable(Table t)
    {
        // route via MoveToTable state so Table logic will run
        CommandPickupRock(FindObjectOfType<RockBin>()); // dummy, will be overridden when pipeline runs
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
