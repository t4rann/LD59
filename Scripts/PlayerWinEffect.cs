using UnityEngine;

public class PlayerWinEffect : MonoBehaviour
{
    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem winParticles;
    
    private static PlayerWinEffect instance;
    
    void Awake()
    {
        instance = this;
    }

void Start()
{
    if (winParticles != null)
    {
        winParticles.Stop();
        winParticles.Clear();
    }
}
void Update()
{
    if (Input.GetKeyDown(KeyCode.P))
        PlayerWinEffect.PlayWinEffect();
}    
    public static void PlayWinEffect()
    {
        if (instance == null)
        {
            Debug.LogWarning("PlayerWinEffect instance not found!");
            return;
        }
        
        if (instance.winParticles != null)
            instance.winParticles.Play();
    }
}