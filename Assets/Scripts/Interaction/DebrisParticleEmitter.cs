using System.Collections;
using UnityEngine;

public class DebrisParticleEmitter : MonoBehaviour
{
    private ParticleSystem ps;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        StartCoroutine(DebrisParticles());
    }

    private IEnumerator DebrisParticles()
    {
        ps.Play();
        yield return new WaitForSeconds(ps.main.duration);
        Destroy(gameObject);
    }
}

