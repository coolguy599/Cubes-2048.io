using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 15f;
    public GameObject cubePrefab;
    public int initialTailLength = 3;
    public CubeAppearanceManager appearanceManager;
    public float tailDistance = 1.5f;
    public float followSmoothness = 8f;
    public float rotationSmoothness = 10f;
    public float boostSpeed = 10f;
    public float boostDuration = 2f;
    public float boostCooldown = 5f;
    public float boostRefillSpeed = 0.5f;

    Vector3 targetPosition;
    bool isMoving;
    List<GameObject> tailCubes = new List<GameObject>();
    Vector3 currentDirection = Vector3.forward;
    float gridSize = 1f;
    CubeController playerCubeController;
    bool isMerging;
    bool isBoosting;
    float currentBoost;
    float boostTimer;
    float cooldownTimer;

    void Start()
    {
        targetPosition = transform.position;
        currentBoost = boostDuration;

        playerCubeController = GetComponent<CubeController>();
        if (playerCubeController == null)
        {
            playerCubeController = gameObject.AddComponent<CubeController>();
        }

        playerCubeController.currentValue = 1073741824;
        playerCubeController.appearanceManager = appearanceManager;
        playerCubeController.UpdateAppearance();

        InitializeTail();
    }

    void Update()
    {
        if (isMerging) return;

        HandleBoostInput();
        HandleInput();
        MovePlayer();
        UpdateTail();
        UpdatePlayerAppearance();
        UpdateBoost();
    }

    void HandleBoostInput()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        if (Input.GetMouseButton(0) && currentBoost > 0)
        {
            isBoosting = true;
            currentBoost -= Time.deltaTime;
            boostTimer = 0.5f;
        }
        else
        {
            isBoosting = false;
        }

        if (!Input.GetMouseButton(0) && currentBoost < boostDuration)
        {
            currentBoost += Time.deltaTime * boostRefillSpeed;
            currentBoost = Mathf.Min(currentBoost, boostDuration);
        }

        if (currentBoost <= 0)
        {
            cooldownTimer = boostCooldown;
            currentBoost = 0;
        }
    }

    void UpdateBoost()
    {
        if (boostTimer > 0)
        {
            boostTimer -= Time.deltaTime;
        }
    }

    void UpdatePlayerAppearance()
    {
        TextMesh textMesh = GetComponentInChildren<TextMesh>();
        if (textMesh != null)
        {
            textMesh.text = playerCubeController.currentValue.ToString();
        }
    }

    void HandleInput()
    {
        if (isMoving) return;

        if (Input.GetMouseButton(0))
        {
            HandleMouseInput();
        }

        if (Input.GetKey(KeyCode.UpArrow)) currentDirection = Vector3.forward;
        else if (Input.GetKey(KeyCode.DownArrow)) currentDirection = Vector3.back;
        else if (Input.GetKey(KeyCode.LeftArrow)) currentDirection = Vector3.left;
        else if (Input.GetKey(KeyCode.RightArrow)) currentDirection = Vector3.right;
    }

    void HandleMouseInput()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.y = transform.position.y;

        Vector3 newDirection = (worldPos - transform.position).normalized;

        if (newDirection.magnitude > 0.1f)
        {
            currentDirection = newDirection;
        }
    }

    void MovePlayer()
    {
        if (!isMoving)
        {
            targetPosition = transform.position + currentDirection * gridSize;
            isMoving = true;
        }

        float currentMoveSpeed = isBoosting ? boostSpeed : moveSpeed;

        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentMoveSpeed * Time.deltaTime);

            if (currentDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(currentDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;
            }
        }
    }

    void InitializeTail()
    {
        for (int i = 0; i < initialTailLength; i++)
        {
            AddTailCube(2);
        }
    }

    void AddTailCube(int value)
    {
        Vector3 spawnPosition = CalculateTailPosition(tailCubes.Count);
        Quaternion spawnRotation = CalculateTailRotation(tailCubes.Count);

        GameObject newCube = Instantiate(cubePrefab, spawnPosition, spawnRotation);
        CubeController cubeController = newCube.GetComponent<CubeController>();
        cubeController.appearanceManager = appearanceManager;
        cubeController.SetValue(value);

        Rigidbody cubeRb = newCube.AddComponent<Rigidbody>();
        cubeRb.useGravity = false;
        cubeRb.isKinematic = true;

        foreach (var existingCube in tailCubes)
        {
            Physics.IgnoreCollision(newCube.GetComponent<Collider>(), existingCube.GetComponent<Collider>(), true);
        }

        tailCubes.Add(newCube);
    }

    Vector3 CalculateTailPosition(int index)
    {
        if (index == 0)
        {
            return transform.position - transform.forward * tailDistance;
        }
        else
        {
            GameObject previousCube = tailCubes[index - 1];
            return previousCube.transform.position - previousCube.transform.forward * tailDistance;
        }
    }

    Quaternion CalculateTailRotation(int index)
    {
        if (index == 0)
        {
            return transform.rotation;
        }
        else
        {
            GameObject previousCube = tailCubes[index - 1];
            return previousCube.transform.rotation;
        }
    }

    void UpdateTail()
    {
        for (int i = 0; i < tailCubes.Count; i++)
        {
            if (tailCubes[i] == null) continue;

            Vector3 targetPosition;
            Quaternion targetRotation;

            if (i == 0)
            {
                targetPosition = transform.position - transform.forward * tailDistance;
                targetRotation = transform.rotation;
            }
            else
            {
                GameObject previousCube = tailCubes[i - 1];
                targetPosition = previousCube.transform.position - previousCube.transform.forward * tailDistance;
                targetRotation = previousCube.transform.rotation;
            }

            tailCubes[i].transform.position = Vector3.Lerp(
                tailCubes[i].transform.position,
                targetPosition,
                followSmoothness * Time.deltaTime
            );

            tailCubes[i].transform.rotation = Quaternion.Slerp(
                tailCubes[i].transform.rotation,
                targetRotation,
                rotationSmoothness * Time.deltaTime
            );
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Cube") && !isMerging)
        {
            CubeController otherCube = collision.gameObject.GetComponent<CubeController>();
            if (otherCube != null)
            {
                HandleCubeCollection(otherCube);
            }
        }
    }

    void HandleCubeCollection(CubeController collectedCube)
    {
        if (collectedCube.currentValue > playerCubeController.currentValue)
        {
            return;
        }

        if (collectedCube.currentValue == playerCubeController.currentValue)
        {
            StartCoroutine(MergeWithPlayer(collectedCube));
        }
        else
        {
            AddToTail(collectedCube);
        }
    }

    IEnumerator MergeWithPlayer(CubeController collectedCube)
    {
        isMerging = true;

        int newValue = playerCubeController.currentValue * 2;
        Vector3 mergePosition = transform.position;

        float mergeDuration = 0.3f;
        float elapsed = 0f;
        Vector3 startPos = collectedCube.transform.position;
        Quaternion startRot = collectedCube.transform.rotation;

        while (elapsed < mergeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / mergeDuration;

            collectedCube.transform.position = Vector3.Lerp(startPos, mergePosition, t);
            collectedCube.transform.rotation = Quaternion.Slerp(startRot, transform.rotation, t);
            collectedCube.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);

            yield return null;
        }

        playerCubeController.SetValue(newValue);
        Destroy(collectedCube.gameObject);

        CheckForAllMerges();
        isMerging = false;
    }

    void AddToTail(CubeController collectedCube)
    {
        Rigidbody collectedRb = collectedCube.GetComponent<Rigidbody>();
        if (collectedRb != null)
        {
            collectedRb.isKinematic = true;
        }

        Collider collectedCollider = collectedCube.GetComponent<Collider>();
        if (collectedCollider != null)
        {
            collectedCollider.isTrigger = true;
        }

        foreach (var cube in tailCubes)
        {
            Physics.IgnoreCollision(collectedCollider, cube.GetComponent<Collider>(), true);
        }

        collectedCube.transform.SetParent(transform);
        tailCubes.Add(collectedCube.gameObject);
        CheckForAllMerges();
    }

    void CheckForAllMerges()
    {
        Dictionary<int, List<GameObject>> valueGroups = new Dictionary<int, List<GameObject>>();

        valueGroups[playerCubeController.currentValue] = new List<GameObject> { gameObject };

        foreach (var cube in tailCubes)
        {
            if (cube != null)
            {
                CubeController cubeController = cube.GetComponent<CubeController>();
                if (cubeController != null)
                {
                    int value = cubeController.currentValue;
                    if (!valueGroups.ContainsKey(value))
                    {
                        valueGroups[value] = new List<GameObject>();
                    }
                    valueGroups[value].Add(cube);
                }
            }
        }

        foreach (var group in valueGroups)
        {
            if (group.Value.Count >= 2)
            {
                StartCoroutine(MergeAllCubes(group.Value, group.Key));
                return;
            }
        }
    }

    IEnumerator MergeAllCubes(List<GameObject> cubesToMerge, int value)
    {
        isMerging = true;

        int newValue = value * 2;
        int mergeCount = cubesToMerge.Count / 2;
        Vector3 mergePosition = transform.position;

        List<GameObject> cubesToKeep = new List<GameObject>();
        List<GameObject> cubesToDestroy = new List<GameObject>();

        bool playerIncluded = cubesToMerge.Contains(gameObject);

        for (int i = 0; i < mergeCount * 2; i++)
        {
            if (i < mergeCount)
            {
                cubesToKeep.Add(cubesToMerge[i]);
            }
            else
            {
                cubesToDestroy.Add(cubesToMerge[i]);
            }
        }

        float mergeDuration = 0.3f;
        float elapsed = 0f;

        List<Vector3> startPositions = new List<Vector3>();
        List<Quaternion> startRotations = new List<Quaternion>();

        foreach (var cube in cubesToDestroy)
        {
            startPositions.Add(cube.transform.position);
            startRotations.Add(cube.transform.rotation);
        }

        while (elapsed < mergeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / mergeDuration;

            for (int i = 0; i < cubesToDestroy.Count; i++)
            {
                if (cubesToDestroy[i] != null)
                {
                    cubesToDestroy[i].transform.position = Vector3.Lerp(startPositions[i], mergePosition, t);
                    cubesToDestroy[i].transform.rotation = Quaternion.Slerp(startRotations[i], transform.rotation, t);
                    cubesToDestroy[i].transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
                }
            }
            yield return null;
        }

        if (playerIncluded)
        {
            playerCubeController.SetValue(newValue);
        }
        else if (cubesToKeep.Count > 0 && cubesToKeep[0] != null)
        {
            CubeController keepController = cubesToKeep[0].GetComponent<CubeController>();
            keepController.SetValue(newValue);
        }

        foreach (var cube in cubesToDestroy)
        {
            if (cube != null)
            {
                if (cube != gameObject)
                {
                    tailCubes.Remove(cube);
                }
                Destroy(cube);
            }
        }

        yield return new WaitForSeconds(0.1f);
        CheckForAllMerges();
        isMerging = false;
    }

    public float GetBoostPercentage()
    {
        return currentBoost / boostDuration;
    }

    public bool IsBoostReady()
    {
        return currentBoost >= boostDuration * 0.1f && cooldownTimer <= 0;
    }

    public void ChangeTailDistance(float newDistance)
    {
        tailDistance = Mathf.Clamp(newDistance, 1.0f, 2.5f);
    }

    public void ChangeFollowSmoothness(float newSmoothness)
    {
        followSmoothness = Mathf.Clamp(newSmoothness, 5f, 15f);
    }

    public void ChangeRotationSmoothness(float newSmoothness)
    {
        rotationSmoothness = Mathf.Clamp(newSmoothness, 8f, 20f);
    }
}
