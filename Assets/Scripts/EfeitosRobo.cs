using UnityEngine;

public class EfeitosRobo : MonoBehaviour
{
    [Header("Animação das Rodas")]
    [Tooltip("Arraste os objetos 'Visual' das rodas da ESQUERDA aqui")]
    public Transform[] rodasEsquerdas;
    
    [Tooltip("Arraste os objetos 'Visual' das rodas da DIREITA aqui")]
    public Transform[] rodasDireitas;
    
    [Tooltip("Ajuste para sincronizar a velocidade visual com o chão")]
    public float multiplicadorGiro = 350f;
    
    [Tooltip("Qual eixo roda a roda? Normalmente é (1, 0, 0) ou (0, 0, 1)")]
    public Vector3 eixoDeRotacao = new Vector3(1, 0, 0);

    [Header("Efeitos de Fumaça/Poeira")]
    [Tooltip("Arraste os Particle Systems de fumaça (ex: um perto de cada roda traseira)")]
    public ParticleSystem[] particulasFumaca;
    
    [Tooltip("Quantidade máxima de partículas soltas por segundo nas derrapagens")]
    public float taxaMaximaFumaca = 50f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        foreach (var fumaca in particulasFumaca)
        {
            if (fumaca != null)
            {
                var em = fumaca.emission;
                em.enabled = true;
                em.rateOverTime = 0f;
            }
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        Vector3 velLocal = transform.InverseTransformDirection(rb.linearVelocity);
        float velFrente  = velLocal.z;
        float velGiro = rb.angularVelocity.y * 2f; 
        float giroEsq = (velFrente + velGiro) * multiplicadorGiro * Time.fixedDeltaTime;
        float giroDir = (velFrente - velGiro) * multiplicadorGiro * Time.fixedDeltaTime;
        foreach (var roda in rodasEsquerdas)
            if (roda != null) roda.Rotate(eixoDeRotacao, giroEsq, Space.Self);
        foreach (var roda in rodasDireitas)
            if (roda != null) roda.Rotate(eixoDeRotacao, giroDir, Space.Self);
        float derrapagemLateral = Mathf.Abs(velLocal.x);
        float intensidadeGiro   = Mathf.Abs(rb.angularVelocity.y);
        float fatorFumaca = 0f;
        if (derrapagemLateral > 0.5f)
            fatorFumaca += derrapagemLateral * 15f;
        if (intensidadeGiro > 1.5f)
            fatorFumaca += intensidadeGiro * 10f;
        fatorFumaca = Mathf.Clamp(fatorFumaca, 0f, taxaMaximaFumaca);
        foreach (var fumaca in particulasFumaca)
        {
            if (fumaca != null)
            {
                var em = fumaca.emission;
                em.rateOverTime = fatorFumaca;
            }
        }
    }
}