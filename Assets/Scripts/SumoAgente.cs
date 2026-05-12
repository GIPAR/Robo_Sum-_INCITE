using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class SumoAgente : Agent
{
    [Header("Modo de Movimentação")]
    public bool usarWheelColliders = false;

    [Header("Especificações do Agente")]
    public float forcaMotor = 40f;
    public float forcaGiro = 15f;
    public float velocidadeMaxima = 5f;

    [Header("Efeito Magnético (Simulação)")]
    public float forcaMagnetica = 15f;

    [Header("Componentes das Rodas (Física Real)")]
    public WheelCollider[] rodasEsquerdas;
    public WheelCollider[] rodasDireitas;
    public Transform[] visualRodasEsquerdas;
    public Transform[] visualRodasDireitas;

    [Header("Configurações da Arena")]
    public ArenaScript arena;
    public Transform centroArena;
    public float raioDaArena = 10f;

    private Rigidbody rb;
    private Vector3 posicaoInicialLocal;
    private Quaternion rotacaoInicialLocal;
    private Transform adversario;
    private bool isGrounded;
    private bool emContatoComAdversario;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        posicaoInicialLocal = transform.localPosition;
        rotacaoInicialLocal = transform.localRotation;

        foreach (GameObject g in GameObject.FindGameObjectsWithTag("Lutador"))
        {
            if (g != gameObject) { adversario = g.transform; break; }
        }
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.localPosition = posicaoInicialLocal;
        
        float anguloAleatorio = Random.Range(-20f, 20f);
        transform.localRotation = rotacaoInicialLocal * Quaternion.Euler(0, anguloAleatorio, 0);
        
        rb.mass = Random.Range(0.9f, 1.1f);
        forcaMotor = Random.Range(35f, 45f);
        isGrounded = false;
        emContatoComAdversario = false;

        if (usarWheelColliders)
        {
            foreach (var roda in rodasEsquerdas) roda.motorTorque = 0f;
            foreach (var roda in rodasDireitas)  roda.motorTorque = 0f;
        }
    }

    // AQUI ESTÃO OS 9 OLHOS DA IA
    public override void CollectObservations(VectorSensor sensor)
    {
        if (centroArena != null)
        {
            Vector3 minhaDistanciaDoCentro = transform.localPosition - centroArena.localPosition;
            sensor.AddObservation(minhaDistanciaDoCentro.x / raioDaArena);
            sensor.AddObservation(minhaDistanciaDoCentro.z / raioDaArena);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        Vector3 velLocal = transform.InverseTransformDirection(rb.linearVelocity);
        sensor.AddObservation(velLocal.x / velocidadeMaxima);
        sensor.AddObservation(velLocal.z / velocidadeMaxima);
        sensor.AddObservation(transform.forward.x);
        sensor.AddObservation(transform.forward.z);

        if (adversario != null)
        {
            Vector3 dirAdversario = adversario.localPosition - transform.localPosition;
            sensor.AddObservation(dirAdversario.normalized.x);
            sensor.AddObservation(dirAdversario.normalized.z);
            sensor.AddObservation(dirAdversario.magnitude / (raioDaArena * 2f));
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float aceleracao = actions.ContinuousActions[0];
        float giro       = actions.ContinuousActions[1];

        if (isGrounded)
            rb.AddForce(-transform.up * forcaMagnetica, ForceMode.Force);

        if (usarWheelColliders)
        {
            float forcaEsq = (aceleracao + giro) * forcaMotor;
            float forcaDir = (aceleracao - giro) * forcaMotor;
            foreach (var roda in rodasEsquerdas) roda.motorTorque = forcaEsq;
            foreach (var roda in rodasDireitas)  roda.motorTorque = forcaDir;
        }
        else
        {
            float fatorVelocidade = Mathf.Clamp01(1f - (rb.linearVelocity.magnitude / velocidadeMaxima));
            
            rb.AddRelativeForce(Vector3.forward * aceleracao * forcaMotor * fatorVelocidade);
            rb.AddRelativeTorque(Vector3.up * giro * forcaGiro);
            
            Vector3 velLocal = transform.InverseTransformDirection(rb.linearVelocity);
            velLocal.x *= 0.85f; 
            rb.linearVelocity = transform.TransformDirection(velLocal);
        }

        // Imposto de tempo
        AddReward(-0.001f);

        if (adversario != null)
        {
            float distancia = Vector3.Distance(transform.localPosition, adversario.localPosition);
            float velocidadeAtual = rb.linearVelocity.magnitude;

            // Recompensa de Proximidade Dinâmica (quebra do platô)
            if (velocidadeAtual > 0.5f)
            {
                float proximidade = 1f - Mathf.Clamp01(distancia / (raioDaArena * 2f));
                AddReward(proximidade * 0.0005f);
            }

            if (!emContatoComAdversario)
            {
                Vector3 dirAdversario = (adversario.localPosition - transform.localPosition).normalized;
                float velocidadeDeAproximacao = Vector3.Dot(rb.linearVelocity, dirAdversario);
                
                if (velocidadeDeAproximacao > 0.5f)
                {
                    AddReward(velocidadeDeAproximacao * 0.002f); 
                }
            }
        }
    }

    private void FixedUpdate() => AtualizarVisualRodas();

    private void AtualizarVisualRodas()
    {
        if (!usarWheelColliders) return;
        AtualizarMeshesRodas(rodasEsquerdas, visualRodasEsquerdas);
        AtualizarMeshesRodas(rodasDireitas,  visualRodasDireitas);
    }

    private void AtualizarMeshesRodas(WheelCollider[] colliders, Transform[] visuals)
    {
        if (colliders == null || visuals == null) return;
        int count = Mathf.Min(colliders.Length, visuals.Length);
        for (int i = 0; i < count; i++)
        {
            if (colliders[i] == null || visuals[i] == null) continue;
            colliders[i].GetWorldPose(out Vector3 pos, out Quaternion rot);
            visuals[i].SetPositionAndRotation(pos, rot);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Borda")) return;

        if (arena != null)
            arena.IniciarRound(this);
    }

    private void OnCollisionEnter(Collision colisao)
    {
        if (colisao.collider.CompareTag("Piso"))
            isGrounded = true;

        if (colisao.collider.CompareTag("Lutador"))
        {
            emContatoComAdversario = true;
            float impacto = Mathf.Clamp(rb.linearVelocity.magnitude, 0f, 5f);
            AddReward(impacto * 0.01f);
        }
    }

    private void OnCollisionStay(Collision colisao)
    {
        if (colisao.collider.CompareTag("Lutador") && centroArena != null)
        {
            Vector3 direcaoParaBorda = (colisao.transform.localPosition - centroArena.localPosition).normalized;
            float forcaEmpurraoBorda = Vector3.Dot(rb.linearVelocity, direcaoParaBorda);
            
            if (forcaEmpurraoBorda > 0.1f)
            {
                AddReward(Mathf.Clamp(forcaEmpurraoBorda * 0.0003f, 0f, 0.001f));
            }
        }
    }

    private void OnCollisionExit(Collision colisao)
    {
        if (colisao.collider.CompareTag("Piso"))
            isGrounded = false;

        if (colisao.collider.CompareTag("Lutador"))
            emContatoComAdversario = false;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ac = actionsOut.ContinuousActions;
        ac[0] = Input.GetAxis("Vertical");
        ac[1] = Input.GetAxis("Horizontal");
    }
}