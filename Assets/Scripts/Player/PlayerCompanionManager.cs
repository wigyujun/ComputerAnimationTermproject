using System.Collections.Generic;
using UnityEngine;

public class PlayerCompanionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform companionRoot;
    [SerializeField] private GameObject companionPrefab;

    [Header("Settings")]
    [SerializeField] private int maxCompanions = 2;

    [Header("Slots")]
    [SerializeField] private Vector3 slot1LocalPosition = new Vector3(-0.6f, -0.55f, 0f);
    [SerializeField] private Vector3 slot2LocalPosition = new Vector3(0.6f, -0.55f, 0f);

    private readonly List<GameObject> companions = new List<GameObject>();

    public int CompanionCount => companions.Count;
    public int MaxCompanions => maxCompanions;

    public bool CanRecruitCompanion()
    {
        return companions.Count < maxCompanions;
    }

    public bool RecruitCompanion()
    {
        if (!CanRecruitCompanion())
            return false;

        if (companionPrefab == null)
        {
            Debug.LogWarning("PlayerCompanionManager: companionPrefab이 비어 있음");
            return false;
        }

        Transform parent = companionRoot != null ? companionRoot : transform;

        GameObject companion = Instantiate(companionPrefab, parent);
        companion.transform.localPosition = GetSlotLocalPosition(companions.Count);
        companion.transform.localRotation = Quaternion.identity;

        companions.Add(companion);
        return true;
    }

    private Vector3 GetSlotLocalPosition(int index)
    {
        if (index == 0) return slot1LocalPosition;
        if (index == 1) return slot2LocalPosition;
        return new Vector3(0f, -0.8f, 0f);
    }
}
