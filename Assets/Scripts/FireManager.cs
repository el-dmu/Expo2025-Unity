using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class FireManager : MonoBehaviour
{
    public static FireManager Instance { get; private set; }

    private List<FireInteraction> allFires;
    private List<FireInteraction> activeFires;

    public static event Action<int, int> OnFireCountUpdate;
    public static event Action OnAllFiresExtinguished;

    public int InitialFireCount { get; private set; }
    public int ActiveFireCount => activeFires != null ? activeFires.Count : 0;

    public float GetOverallProgress()
    {
        if (InitialFireCount == 0) return 1f;
        float totalProgress = 0f;
        foreach (var fire in allFires)
        {
            if (fire != null) totalProgress += fire.ExtinguishProgress;
        }
        return totalProgress / InitialFireCount;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
    }

    void Start()
    {
        allFires = FindObjectsOfType<FireInteraction>().ToList();
        activeFires = new List<FireInteraction>(allFires);
        InitialFireCount = allFires.Count;

        foreach (var fire in activeFires)
        {
            fire.OnExtinguished += OnFireSourceExtinguished;
        }
    }

    private void OnFireSourceExtinguished(FireInteraction extinguishedFire)
    {
        extinguishedFire.OnExtinguished -= OnFireSourceExtinguished;
        activeFires.Remove(extinguishedFire);

        OnFireCountUpdate?.Invoke(activeFires.Count, InitialFireCount);

        if (activeFires.Count == 0)
        {
            OnAllFiresExtinguished?.Invoke();
        }
    }
}

