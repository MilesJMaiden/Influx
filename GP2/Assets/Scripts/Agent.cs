// Agent.cs
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public enum AgentState
{
    Idle,
    Wander,
    ChaseAlien,
    TrapAlien,
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
    private GameObject _alienVisual;
    public bool isCarryingAlien;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();

        selectedIndicator = transform.Find("SelectedCylinder")?.gameObject;
        highlightIndicator = transform.Find("HighlightCylinder")?.gameObject;
        selectedIndicator?.SetActive(false);
        highlightIndicator?.SetActive(false);

        // hide rock/refined rock
        transform.Find("Rock")?.gameObject.SetActive(false);
        _refinedRockObject = transform.Find("RefinedRock")?.gameObject;
        if (_refinedRockObject != null)
            _refinedRockObject.SetActive(false);

        // hide Alien visual
        _alienVisual = transform.Find("Alien")?.gameObject;
        if (_alienVisual != null)
            _alienVisual.SetActive(false);
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
        // 1) Deselect shortcut
        if (Input.GetKeyDown(KeyCode.Q) && SelectedAgent != null)
        {
            SelectedAgent.Deselect();
            return;
        }

        // 2) Fuel depletion
        if (fuelSlider)
            fuelSlider.value = Mathf.Max(0f, fuelSlider.value - fuelDepleteRate * Time.deltaTime);

        // 3) Player override: only in Idle or Wander
        if (SelectedAgent == this &&
            (currentState == AgentState.Idle || currentState == AgentState.Wander))
        {
            HandleUserInput();
            return;
        }

        // 4) Repair pipeline
        if (currentState == AgentState.MoveToComputer || currentState == AgentState.Repair)
        {
            RepairBehavior();
            return;
        }

        // 5) ALIEN HUNTING / TRAPPING (highest auto priority)
        //    Only if NOT carrying rock or refined rock, and not already in the rock pipeline
        if (!IsCarryingRock && !IsCarryingRefined)
        {
            var aliens = FindObjectsOfType<AlienController>()
                         .Select(a => a.gameObject)
                         .ToList();
            if (aliens.Any())
            {
                currentState = AgentState.ChaseAlien;
                var nearest = aliens
                    .OrderBy(a => Vector3.Distance(transform.position, a.transform.position))
                    .First();
                navAgent.SetDestination(nearest.transform.position);

                // if we’re within 2 units, capture it
                if (!navAgent.pathPending &&
                    Vector3.Distance(transform.position, nearest.transform.position) <= 2f)
                {
                    Destroy(nearest);
                    isCarryingAlien = true;
                    if (_alienVisual != null) _alienVisual.SetActive(true);

                    // immediately switch to trap behavior
                    var containers = FindObjectsOfType<Container>()
                        .Where(c => c.alienSpawnPoint != null)
                        .ToList();
                    if (containers.Any())
                    {
                        currentState = AgentState.TrapAlien;
                        var dropTarget = containers
                            .OrderBy(c => Vector3.Distance(
                                transform.position,
                                c.alienSpawnPoint.transform.position))
                            .First();
                        navAgent.SetDestination(
                            dropTarget.alienSpawnPoint.transform.position);
                    }
                }
                return;
            }
        }

        else if (isCarryingAlien)
        {
            currentState = AgentState.TrapAlien;

            // find all valid drop‐off containers
            var containers = FindObjectsOfType<Container>()
                .Where(c => c.alienSpawnPoint != null)
                .ToList();

            if (containers.Any())
            {
                // pick closest spawn point
                var nearest = containers
                    .OrderBy(c => Vector3.Distance(
                        transform.position,
                        c.alienSpawnPoint.transform.position))
                    .First();

                var dropPos = nearest.alienSpawnPoint.transform.position;
                navAgent.SetDestination(dropPos);

                // **use a hard-coded 2f radius** instead of stoppingDistance
                if (!navAgent.pathPending &&
                    Vector3.Distance(transform.position, dropPos) <= 2f)
                {
                    // drop the alien
                    isCarryingAlien = false;
                    if (_alienVisual != null)
                        _alienVisual.SetActive(false);

                    // if there are more aliens, resume chase, else go idle
                    if (FindObjectsOfType<AlienController>().Any())
                        currentState = AgentState.ChaseAlien;
                    else
                        TransitionTo(AgentState.Idle);
                }
            }
            return;
        }

        // 5) If world‐bin low (<50%), do refuel pipeline first
        var globalBin = FindObjectOfType<Bin>();
        if (globalBin != null)
        {
            var binSlider = globalBin.GetComponentInChildren<Slider>();
            if (binSlider != null
                && binSlider.value < 0.5f
                && !IsCarryingRock
                && !IsCarryingRefined
                && currentState == AgentState.Idle)
            {
                var rockBin = FindObjectOfType<RockBin>();
                if (rockBin != null)
                {
                    CommandPickupRock(rockBin);
                    return;
                }
            }
        }

        // 6) Repair pipeline
        if (currentState == AgentState.MoveToComputer
            || currentState == AgentState.Repair)
        {
            RepairBehavior();
            return;
        }

        // 7) Rock→Table→Bin pipeline
        if (currentState == AgentState.MoveToRockBin
            || currentState == AgentState.MoveToTable
            || currentState == AgentState.RefineRock
            || currentState == AgentState.MoveToBin)
        {
            RockPipelineBehavior();
            return;
        }

        // 8) Auto‐repair
        if (repairTarget == null)
            TryAutoRepair();
        if (repairTarget != null)
            return;

        // 9) Auto‐refuel
        bool needFuel = fuelSlider && fuelSlider.value < 0.75f;
        if (needFuel
            && !IsCarryingRock
            && !IsCarryingRefined
            && currentState == AgentState.Idle)
        {
            var rockBin = FindObjectOfType<RockBin>();
            if (rockBin != null)
            {
                CommandPickupRock(rockBin);
                return;
            }
        }

        // 10) Normal FSM
        stateTimer -= Time.deltaTime;
        if (currentState == AgentState.Idle && stateTimer <= 0f)
            TransitionTo(AgentState.Wander);
        else if (currentState == AgentState.Wander
                 && !navAgent.pathPending
                 && navAgent.remainingDistance <= navAgent.stoppingDistance)
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
        if (closest != null) CommandRepair(closest);
    }

    private void RepairBehavior()
    {
        float dist = Vector3.Distance(
            transform.position,
            repairTarget.transform.position);
        if (currentState == AgentState.MoveToComputer)
        {
            if (dist <= repairDistance)
            {
                navAgent.ResetPath();
                currentState = AgentState.Repair;
                repairTarget.NotifyRepairStart();
            }
        }
        else
        {
            var slider = repairTarget
                         .GetComponentInChildren<Slider>();
            if (slider == null) { EndRepair(); return; }
            slider.value = Mathf.Min(
                1f,
                slider.value + repairRate * Time.deltaTime);
            if (slider.value >= 1f) EndRepair();
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
                var rockBin = FindObjectOfType<RockBin>();
                if (rockBin == null) { Debug.LogWarning("No RockBin!"); return; }
                float d = Vector3.Distance(
                    transform.position, rockBin.transform.position);
                if (d <= rockPickupRadius)
                {
                    IsCarryingRock = true;
                    transform.Find("Rock")?.gameObject.SetActive(true);

                    if (SelectedAgent == this)
                        TransitionTo(AgentState.Idle);
                    else
                    {
                        currentState = AgentState.MoveToTable;
                        var table = FindObjectOfType<Table>();
                        if (table != null)
                            navAgent.SetDestination(table.transform.position);
                    }
                }
                break;

            case AgentState.MoveToTable:
                var tableObj = FindObjectOfType<Table>();
                if (tableObj == null) { Debug.LogWarning("No Table!"); return; }
                float dt = Vector3.Distance(
                    transform.position, tableObj.transform.position);
                if (dt <= tableProcessRadius)
                {
                    transform.Find("Rock")?.gameObject.SetActive(false);
                    IsCarryingRock = false;
                    tableObj.transform.Find("Rock")?.gameObject.SetActive(true);
                    var tableSlider = tableObj.GetComponentInChildren<Slider>();
                    if (tableSlider != null)
                    {
                        tableSlider.value = 0f;
                        tableSlider.gameObject.SetActive(true);
                        currentState = AgentState.RefineRock;
                        if (refineRoutine != null)
                            StopCoroutine(refineRoutine);
                        refineRoutine = StartCoroutine(
                            RefineCoroutine(tableSlider, tableObj));
                    }
                }
                break;

            case AgentState.MoveToBin:
                var bin = FindObjectOfType<Bin>();
                if (bin == null) break;
                float db = Vector3.Distance(
                    transform.position, bin.transform.position);
                if (db <= binDepositRadius)
                {
                    transform.Find("RefinedRock")
                             ?.gameObject.SetActive(false);
                    IsCarryingRefined = false;
                    bin.AddFuel(fuelRefuelAmount);
                    if (SelectedAgent == this)
                        TransitionTo(AgentState.Idle);
                    else
                        TransitionTo(AgentState.Wander);
                }
                break;
        }
    }

    public void CommandPickupRock(RockBin bin)
    {
        currentState = AgentState.MoveToRockBin;
        navAgent.SetDestination(bin.transform.position);
    }

    private IEnumerator RefineCoroutine(Slider tableSlider, Table table)
    {
        float elapsed = 0f, duration = 4f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            tableSlider.value =
                Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        tableSlider.gameObject.SetActive(false);
        table.transform.Find("Rock")?.gameObject.SetActive(false);
        if (_refinedRockObject != null)
            _refinedRockObject.SetActive(true);
        IsCarryingRefined = true;
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

    // inside Agent.cs…

    private void Select()
    {
        // if we were heading to repair something, abandon it without resetting it to full:
        if (repairTarget != null)
        {
            repairTarget.CancelRepair();
            repairTarget = null;
        }

        // drop into Idle so click‐override immediately works
        currentState = AgentState.Idle;
        navAgent.ResetPath();

        SelectedAgent = this;
        selectedIndicator?.SetActive(true);
        highlightIndicator?.SetActive(false);
        UICursorManager.Instance?.ResetCursorImage();
    }

    public void Deselect()
    {
        // if we were mid-repair, cancel it without resetting to 1
        if (currentState == AgentState.Repair && repairTarget != null)
        {
            repairTarget.CancelRepair();
            repairTarget = null;
        }

        selectedIndicator?.SetActive(false);
        if (SelectedAgent == this)
        {
            SelectedAgent = null;
            TransitionTo(AgentState.Wander);
        }
    }

    // —— Player Command for Bin Deposit ——

    public void CommandDeposit(Bin b)
    {
        currentState = AgentState.MoveToBin;
        navAgent.SetDestination(b.transform.position);
    }

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
                    // 1) cancel any queued repair
                    repairTarget = null;
                    // 2) manually force Wander so we can move
                    TransitionTo(AgentState.Wander);
                    // 3) go there
                    navAgent.SetDestination(hit.point);
                }
                // computer click
                else if (hit.collider.GetComponentInParent<Computer>() is Computer c)
                {
                    CommandRepair(c);
                }
                // rock‐bin click
                else if (hit.collider.GetComponentInParent<RockBin>() is RockBin rb)
                {
                    CommandPickupRock(rb);
                }
                // table click
                else if (hit.collider.GetComponentInParent<Table>() is Table tbl)
                {
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

    public void CommandDropAlien(Container container)
    {
        // switch into TrapAlien so Update() will navigate you there,
        // but force the destination to this exact spawn point
        currentState = AgentState.TrapAlien;
        navAgent.SetDestination(container.alienSpawnPoint.transform.position);
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
                var rnd = Random.insideUnitSphere
                          * wanderRadius + transform.position;
                if (NavMesh.SamplePosition(
                        rnd, out var hit,
                        wanderRadius, NavMesh.AllAreas))
                    navAgent.SetDestination(hit.position);
                break;
            case AgentState.MoveToRockBin:
                var rb = FindObjectOfType<RockBin>();
                if (rb != null)
                    navAgent.SetDestination(rb.transform.position);
                break;
            case AgentState.MoveToTable:
                var tbl = GameObject.FindWithTag("Table");
                if (tbl != null)
                    navAgent.SetDestination(tbl.transform.position);
                break;
            case AgentState.MoveToBin:
                var b = FindObjectOfType<Bin>();
                if (b != null)
                    navAgent.SetDestination(b.transform.position);
                break;
        }
    }
}

