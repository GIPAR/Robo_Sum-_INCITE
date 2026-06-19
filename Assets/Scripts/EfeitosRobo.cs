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

    private ParticleSystem.EmissionModule[] modulosEmissao;
    private int countEsquerdas;
    private int countDireitas;

    // ✅ Calculado uma única vez (correcaoRotacao não muda em runtime)
    private Quaternion qRotacaoCorrigida;

    // ✅ Cache da última velocidade local para a fumaça usar em Update
    // sem precisar reler o Rigidbody fora do passo físico
    private Vector3 ultimaVelLocal;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        temWheelColliders = wcEsquerdas != null && wcEsquerdas.Length > 0
                         && wcDireitas  != null && wcDireitas.Length  > 0;

        countEsquerdas = Mathf.Min(wcEsquerdas?.Length ?? 0, rodasEsquerdas?.Length ?? 0);
        countDireitas  = Mathf.Min(wcDireitas?.Length ?? 0, rodasDireitas?.Length ?? 0);

        // ✅ Calculado uma vez só, fora do loop
        qRotacaoCorrigida = Quaternion.Euler(correcaoRotacao);

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

    // ✅ Sincronização física movida para FixedUpdate — mesmo passo que
    // WheelCollider e Rigidbody usam internamente. Elimina o jitter visual
    // causado por ler GetWorldPose em frames que não coincidem com o
    // passo físico (mesmo padrão já corrigido em GirarRodas.cs).
    void FixedUpdate()
    {
        if (rb == null) return;

        ultimaVelLocal = transform.InverseTransformDirection(rb.linearVelocity);

        if (temWheelColliders)
        {
            SincronizarRodas(wcEsquerdas, rodasEsquerdas, countEsquerdas);
            SincronizarRodas(wcDireitas,  rodasDireitas,  countDireitas);
        }
        else
        {
            AnimarPorFisica(ultimaVelLocal, Time.fixedDeltaTime);
        }
    }

    // ✅ Fumaça permanece em Update — é puramente cosmética e se beneficia
    // de mais atualizações por segundo para parecer suave, sem precisar
    // estar sincronizada com o passo físico.
    void Update()
    {
        if (rb == null) return;
        AtualizarFumaca(ultimaVelLocal);
    }

    private void SincronizarRodas(WheelCollider[] wcs, Transform[] visuais, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (wcs[i] == null || visuais[i] == null) continue;
            wcs[i].GetWorldPose(out Vector3 pos, out Quaternion rot);
            visuais[i].rotation = rot * qRotacaoCorrigida;
            visuais[i].position = pos + (rot * correcaoPosicao);
        }
    }

    private void AnimarPorFisica(Vector3 velLocal, float deltaTime)
    {
        float invRaio    = 1f / (raioRoda > 0f ? raioRoda : 0.001f);
        float fatorTempo = Mathf.Rad2Deg * deltaTime;

        float grausBase        = (velLocal.z * invRaio) * fatorTempo;
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

        for (int i = 0; i < modulosEmissao.Length; i++)
            modulosEmissao[i].rateOverTime = fatorFumaca;
    }
}