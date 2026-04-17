using UnityEngine;

public class RoboScript : MonoBehaviour
{
    [Header("Motor e Inércia (Sensação de Peso)")]
    public float forcaImpulso = 1800f; 
    public float velocidadeMaxima = 10f; 
    
    [Tooltip("Tempo (em segundos) que o motor leva para chegar na força máxima. Aumente para deixar a arrancada mais pesada.")]
    public float tempoDeArrancada = 2.5f; 
    
    public float forcaGirarCorpo = 45f; 

    [Header("Ajuste de Direção")]
    [Range(0f, 1f)]
    public float aderenciaLateral = 0.85f; 

    [Header("Física das Rodas (Wheel Colliders)")]
    public WheelCollider wcFrenteEsq;
    public WheelCollider wcFrenteDir;
    public WheelCollider wcTrasEsq;
    public WheelCollider wcTrasDir;

    [Header("Visual das Rodas (Cilindros)")]
    public Transform visualFrenteEsq;
    public Transform visualFrenteDir;
    public Transform visualTrasEsq;
    public Transform visualTrasDir;

    private Rigidbody rb;
    private Quaternion offsetCilindro = Quaternion.Euler(0f, 0f, 90f);
    
    // Variável secreta para controlar a força subindo aos poucos
    private float forcaAtualDoMotor = 0f; 

    void Start()
    {
        rb = GetComponent<Rigidbody>(); 
    }

    void FixedUpdate()
    {
        float comandoAcelerar = Input.GetAxis("Vertical");
        float comandoVirar    = Input.GetAxis("Horizontal");

        // 1. Faz os pneus virarem visualmente
        float direcao = comandoVirar * 30f; 
        wcFrenteEsq.steerAngle = direcao;
        wcFrenteDir.steerAngle = direcao;

        // 2. Faz as rodas girarem em falso
        float motorFalso = comandoAcelerar * 100f;
        wcFrenteEsq.motorTorque = motorFalso;
        wcFrenteDir.motorTorque = motorFalso;
        wcTrasEsq.motorTorque = motorFalso;
        wcTrasDir.motorTorque = motorFalso;

        // 3. O NOVO SEGREDO: Aceleração Progressiva (Sem Tranco)
        float forcaAlvo = comandoAcelerar * forcaImpulso;
        float velocidadeDeRampa = forcaImpulso / tempoDeArrancada;

        // Move a força atual suavemente até a força alvo
        forcaAtualDoMotor = Mathf.MoveTowards(forcaAtualDoMotor, forcaAlvo, velocidadeDeRampa * Time.fixedDeltaTime);

        if (Mathf.Abs(comandoAcelerar) > 0.01f)
        {
            rb.linearDamping = 0.2f; 

            Vector3 velPlana = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

            if (velPlana.magnitude < velocidadeMaxima)
            {
                // Agora usamos a 'forcaAtualDoMotor' que sobe devagarzinho, tirando o tranco
                Vector3 forcaParaFrente = transform.forward * forcaAtualDoMotor;
                rb.AddForce(forcaParaFrente, ForceMode.Force); 
            }
        }
        else
        {
            // Transição suave para o freio
            rb.linearDamping = Mathf.Lerp(rb.linearDamping, 2f, Time.fixedDeltaTime * 3f); 
            // Zera a força do motor rapidamente quando solta o botão
            forcaAtualDoMotor = Mathf.MoveTowards(forcaAtualDoMotor, 0f, (velocidadeDeRampa * 2f) * Time.fixedDeltaTime);
        }

        // 4. Força o robô a rotacionar
        if (comandoVirar != 0)
        {
            float sentido = (comandoAcelerar < 0) ? -1f : 1f; 
            float multiplicadorGiro = (comandoAcelerar == 0) ? 0.6f : 1f; 
            
            float giro = comandoVirar * forcaGirarCorpo * sentido * multiplicadorGiro * Time.fixedDeltaTime;
            Quaternion rotacaoExtra = Quaternion.Euler(0f, giro, 0f);
            rb.MoveRotation(rb.rotation * rotacaoExtra);
        }

        // 5. Anti-Derrapagem (Pneu Virtual)
        Vector3 velocidadeLocal = transform.InverseTransformDirection(rb.linearVelocity);
        velocidadeLocal.x *= (1f - aderenciaLateral); 
        rb.linearVelocity = transform.TransformDirection(velocidadeLocal);

        // 6. Atualiza os cilindros pretos
        AtualizarRodaVisual(wcFrenteEsq, visualFrenteEsq);
        AtualizarRodaVisual(wcFrenteDir, visualFrenteDir);
        AtualizarRodaVisual(wcTrasEsq, visualTrasEsq);
        AtualizarRodaVisual(wcTrasDir, visualTrasDir);
    }

    private void AtualizarRodaVisual(WheelCollider colisor, Transform visual)
    {
        Vector3 posicao;
        Quaternion rotacao;
        colisor.GetWorldPose(out posicao, out rotacao);
        visual.position = posicao;
        visual.rotation = rotacao * offsetCilindro;
    }
}