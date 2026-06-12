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

    // --- VARIÁVEIS DE CACHE (OTIMIZAÇÃO DE PERFORMANCE) ---
    private ParticleSystem.EmissionModule[] modulosEmissao;
    private int countEsquerdas;
    private int countDireitas;
    private Quaternion qRotacaoCorrigida;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        temWheelColliders = wcEsquerdas != null && wcEsquerdas.Length > 0
                         && wcDireitas  != null && wcDireitas.Length  > 0;

        // Calcula os limites dos loops uma única vez no Start
        countEsquerdas = Mathf.Min(wcEsquerdas?.Length ?? 0, rodasEsquerdas?.Length ?? 0);
        countDireitas  = Mathf.Min(wcDireitas?.Length ?? 0, rodasDireitas?.Length ?? 0);

        // Cache dos módulos de emissão para não chamar .emission a cada frame (evita Garbage Collection)
        if (particulasFumaca != null)
        {
            modulosEmissao = new ParticleSystem.EmissionModule[particulasFumaca.Length];
            for (int i = 0; i < particulasFumaca.Length; i++)
            {
                if (particulasFumaca[i] != null)
                {
                    modulosEmissao[i] = particulasFumaca[i].emission;
                    modulosEmissao[i].enabled = true;
                    modulosEmissao[i].rateOverTime = 0f;
                }
            }
        }
    }

    void Update()
    {
        if (rb == null) return;

        // OTIMIZAÇÃO: Transforma a velocidade do mundo para local UMA única vez por frame
        Vector3 velLocal = transform.InverseTransformDirection(rb.linearVelocity);

        if (temWheelColliders)
        {
            // Calcula a trigonometria do Quaternion uma única vez por frame, poupando a CPU
            qRotacaoCorrigida = Quaternion.Euler(correcaoRotacao);
            
            SincronizarRodas(wcEsquerdas, rodasEsquerdas, countEsquerdas);
            SincronizarRodas(wcDireitas,  rodasDireitas, countDireitas);
        }
        else
        {
            // Passa a velocidade local já calculada
            AnimarPorFisica(velLocal);
        }

        AtualizarFumaca(velLocal);
    }

    // ── Modo preciso com Correção Virtual Integrada ──────────
    private void SincronizarRodas(WheelCollider[] wcs, Transform[] visuais, int count)
    {
        // Loop 'for' tradicional é ligeiramente mais rápido que 'foreach' no Update da Unity
        for (int i = 0; i < count; i++)
        {
            if (wcs[i] == null || visuais[i] == null) continue;

            wcs[i].GetWorldPose(out Vector3 pos, out Quaternion rot);

            // 1. Aplica a rotação do eixo real + a compensação manual
            visuais[i].rotation = rot * qRotacaoCorrigida;

            // 2. Aplica a posição + a compensação de altura
            visuais[i].position = pos + (rot * correcaoPosicao);
        }
    }

    // ── Modo fallback (inalterado na lógica, otimizado na matemática) ─────────
    private void AnimarPorFisica(Vector3 velLocal)
    {
        // OTIMIZAÇÃO: Inverso da divisão (Multiplicar é mais rápido) e prevenção de divisão por 0
        float invRaio = 1f / (raioRoda > 0f ? raioRoda : 0.001f); 
        float fatorTempo = Mathf.Rad2Deg * Time.deltaTime;

        float grausBase = (velLocal.z * invRaio) * fatorTempo;
        float contribuicaoGiro = (rb.angularVelocity.y * meiaPista * invRaio) * fatorTempo;

        float giroEsq = grausBase + contribuicaoGiro;
        float giroDir = grausBase - contribuicaoGiro;

        for (int i = 0; i < rodasEsquerdas.Length; i++)
            if (rodasEsquerdas[i] != null) rodasEsquerdas[i].Rotate(eixoDeRotacao, giroEsq, Space.Self);

        for (int i = 0; i < rodasDireitas.Length; i++)
            if (rodasDireitas[i] != null) rodasDireitas[i].Rotate(eixoDeRotacao, giroDir, Space.Self);
    }

    private void AtualizarFumaca(Vector3 velLocal)
    {
        if (modulosEmissao == null) return;

        float derrapagemLateral = Mathf.Abs(velLocal.x);
        float intensidadeGiro   = Mathf.Abs(rb.angularVelocity.y);
        float fatorFumaca = 0f;
       
        if (derrapagemLateral > 0.5f) fatorFumaca += derrapagemLateral * 15f;
        if (intensidadeGiro   > 1.5f) fatorFumaca += intensidadeGiro   * 10f;
       
        fatorFumaca = Mathf.Clamp(fatorFumaca, 0f, taxaMaximaFumaca);
       
        // OTIMIZAÇÃO: Aplica diretamente na matriz em cache, sem criar instâncias novas
        for (int i = 0; i < modulosEmissao.Length; i++)
        {
            modulosEmissao[i].rateOverTime = fatorFumaca;  
        }
    }
}