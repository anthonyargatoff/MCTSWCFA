using UnityEngine;

public class Fireworks : MonoBehaviour
{
    private bool fireworkPrefabLoaded = false;
    private static GameObject fireworkPrefab;
    private Camera mainCamera;
    
    private const float timeBetweenFireworks = 0.5f;
    private float timeSinceLastFirework = 0f; 
    
    private void Awake()
    {
        mainCamera = Camera.main;
        if (!fireworkPrefabLoaded)
        {
            fireworkPrefab = Resources.Load<GameObject>("Prefabs/Firework");
            fireworkPrefabLoaded = true;
        }
    }
    
    private void Update()
    {
        if (timeSinceLastFirework > timeBetweenFireworks)
        {
            var screenPosition = new Vector2(Random.Range(0.1f,0.9f),Random.Range(0.1f,0.5f));
            var position = mainCamera.ViewportToWorldPoint(screenPosition);
            var color = Color.HSVToRGB(Random.Range(0f, 1f), 1f, 1f);
            var fw = Instantiate(fireworkPrefab, new Vector3(position.x,position.y,0), Quaternion.identity);
            var particleSys = fw.GetComponent<ParticleSystem>();
            var m = particleSys.main;
            var colorOverLifeTime = particleSys.colorOverLifetime;
            m.startColor = color;
            colorOverLifeTime.color = new ParticleSystem.MinMaxGradient(Color.white, color);
            fw.transform.SetParent(transform);
            timeSinceLastFirework = 0;
        } 
        timeSinceLastFirework += Time.unscaledDeltaTime;
    }
}
