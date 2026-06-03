using UnityEngine;

public class EfeitosRobo : MonoBehaviour
{
    [Header("Visuais das Rodas")]
    [Tooltip("Mesma ordem que wcEsquerdas")]
    public Transform[] rodasEsquerdas;
    [Tooltip("Mesma ordem que wcDireitas")]
    public Transform[] rodasDireitas;

    [Header("WheelColliders")]
    public WheelCollider[] wcEsquerdas;
    public WheelCollider[] wcDireitas;

    [Header("Correção Visual (Substitui os Empties)")]
    [Tooltip("Se a roda estiver enterrada, ajuste o Y (ex: 0, 0.075, 0)")]
    public Vector3 correcaoPosicao = Vector3.zero;
    
    [Tooltip("Se a roda estiver girando deitada, gire o X ou Z (ex: 0, 0, 90)")]
    public Vector3 correcaoRotacao = Vector3.zero;

    [Header("Configurações Físicas")]
    public float raioRoda = 0.075f;
    public float meiaPista = 0.15f;
    public Vector3 eixoDeRotacao = Vector3.right;

    [Header("Efeitos de Fumaça")]
    public ParticleSystem[] particulasFumaca;
    public float taxaMaximaFumaca = 50f;

    private Rigidbody rb;
    private bool temWheelColliders;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        temWheelColliders = wcEsquerdas != null && wcEsquerdas.Length > 0
                         && wcDireitas  != null && wcDireitas.Length  > 0;

        foreach (var fumaca in particulasFumaca)
        {
            if (fumaca == null) continue;
            var em = fumaca.emission;
            em.enabled      = true;
            em.rateOverTime = 0f;
        }
    }

    void Update() 
    {
        if (rb == null) return;

        if (temWheelColliders)
            AnimarComGetWorldPose();
        else
            AnimarPorFisica();

        AtualizarFumaca();
    }

    // ── Modo preciso com Correção Virtual Integrada ──────────
    private void AnimarComGetWorldPose()
    {
        SincronizarRodas(wcEsquerdas, rodasEsquerdas);
        SincronizarRodas(wcDireitas,  rodasDireitas);
    }

    private void SincronizarRodas(WheelCollider[] wcs, Transform[] visuais)
    {
        int count = Mathf.Min(wcs.Length, visuais.Length);
        for (int i = 0; i < count; i++)
        {
            if (wcs[i] == null || visuais[i] == null) continue;

            wcs[i].GetWorldPose(out Vector3 pos, out Quaternion rot);

            // 1. Aplica a rotação do eixo real + a compensação manual para endireitar a roda
            visuais[i].rotation = rot * Quaternion.Euler(correcaoRotacao);

            // 2. Aplica a posição do eixo real + a compensação de altura para desenterrar a roda
            // (Multiplicar pela rotação garante que o deslocamento acompanhe a suspensão)
            visuais[i].position = pos + (rot * correcaoPosicao);
        }
    }

    // ── Modo fallback (inalterado) ─────────
    private void AnimarPorFisica()
    {
        Vector3 velLocal = transform.InverseTransformDirection(rb.linearVelocity);
        float velFrente  = velLocal.z; 

        float grausBase = (velFrente / raioRoda) * Mathf.Rad2Deg * Time.deltaTime;
        float contribuicaoGiro = (rb.angularVelocity.y * meiaPista / raioRoda) * Mathf.Rad2Deg * Time.deltaTime;

        float giroEsq = grausBase + contribuicaoGiro;
        float giroDir = grausBase - contribuicaoGiro;

        foreach (var roda in rodasEsquerdas)
            if (roda != null) roda.Rotate(eixoDeRotacao, giroEsq, Space.Self);

        foreach (var roda in rodasDireitas)
            if (roda != null) roda.Rotate(eixoDeRotacao, giroDir, Space.Self);
    }

    private void AtualizarFumaca()
    {
       Vector3 velLocal        = transform.InverseTransformDirection(rb.linearVelocity);
       float derrapagemLateral = Mathf.Abs(velLocal.x);
       float intensidadeGiro   = Mathf.Abs(rb.angularVelocity.y);
       float fatorFumaca = 0f;
       
       if (derrapagemLateral > 0.5f) fatorFumaca += derrapagemLateral * 15f;
       if (intensidadeGiro   > 1.5f) fatorFumaca += intensidadeGiro   * 10f;
       
       fatorFumaca = Mathf.Clamp(fatorFumaca, 0f, taxaMaximaFumaca);
       
       foreach (var fumaca in particulasFumaca)
       {
            if(fumaca == null) continue;
            var em = fumaca.emission;
            em.rateOverTime = fatorFumaca;  
       }
    }
}