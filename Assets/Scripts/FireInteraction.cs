using UnityEngine;
using System;

[RequireComponent(typeof(ParticleSystem))]
public class FireInteraction : MonoBehaviour
{
    [Header("진화 설정")]
    public float timeToExtinguish = 5.0f;
    public float sprayGracePeriod = 0.2f;

    [Header("오브젝트 연결")]
    public AudioSource fireAudioSource;

    public event Action<FireInteraction> OnExtinguished;

    private ParticleSystem fireParticleSystem;
    private bool isExtinguished = false;
    private float sprayTimer = 0.0f;
    private float lastParticleHitTime;

    public float ExtinguishProgress => (timeToExtinguish > 0) ? Mathf.Clamp01(sprayTimer / timeToExtinguish) : 0f;

    void Start()
    {
        fireParticleSystem = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        if (isExtinguished) return;

        if (Time.time - lastParticleHitTime < sprayGracePeriod)
        {
            sprayTimer += Time.deltaTime;
            if (sprayTimer >= timeToExtinguish)
            {
                ExtinguishFire();
            }
        }
        else
        {
            sprayTimer = 0f;
        }
    }

    void OnParticleCollision(GameObject other)
    {
        if (isExtinguished) return;

        if (other.CompareTag("ExtinguisherParticle"))
        {
            lastParticleHitTime = Time.time;
        }
    }

    private void ExtinguishFire()
    {
        if (isExtinguished) return;
        isExtinguished = true;
        sprayTimer = timeToExtinguish;

        fireParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (fireAudioSource != null && fireAudioSource.isPlaying)
        {
            fireAudioSource.Stop();
        }

        OnExtinguished?.Invoke(this);
        this.enabled = false;
    }
}

