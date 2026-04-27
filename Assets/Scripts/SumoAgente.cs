using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class SumoAgente : Agent
{
    [Header("Modo de Movimentação")]
    [Tooltip("Marque para usar os WheelColliders. Desmarque para usar física simples (Arcade).")]
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

    [Header("Configurações da Arena")]
    public Transform adversario; 
    public Transform centroArena; 
    public float raioDaArena = 15f;

    private Rigidbody rb;
    private Vector3 posicaoInicial;
    private Quaternion rotacaoInicial;

    public override void Initialize() 
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 3f; 
        posicaoInicial = transform.position;
        rotacaoInicial = transform.rotation;
    }

    public override void OnEpisodeBegin() 
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (this.gameObject.name == "RoboLutador") // O seu robô
        {
            transform.localPosition = new Vector3(0f, 0.6f, 1.5f);
            transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }
        else 
        {
            transform.localPosition = new Vector3(0f, 0.6f, -1.5f);
            transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }

        if (usarWheelColliders)
        {
            foreach (var roda in rodasEsquerdas) roda.motorTorque = 0f;
            foreach (var roda in rodasDireitas) roda.motorTorque = 0f;
        }
    }
    public override void CollectObservations(VectorSensor sensor) 
    {
        if (centroArena != null)
        {
            Vector3 minhaDistanciaDoCentro = transform.position - centroArena.position;
            sensor.AddObservation(minhaDistanciaDoCentro.x);
            sensor.AddObservation(minhaDistanciaDoCentro.z);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        Vector3 velLocal = transform.InverseTransformDirection(rb.linearVelocity); 
        sensor.AddObservation(velLocal.x);
        sensor.AddObservation(velLocal.z);

        if (adversario != null)
        {
            Vector3 dirAdversario = adversario.position - transform.position;
            sensor.AddObservation(dirAdversario.normalized.x); 
            sensor.AddObservation(dirAdversario.normalized.z);
            sensor.AddObservation(dirAdversario.magnitude); 
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
        float giro = actions.ContinuousActions[1]; 

        rb.AddForce(-transform.up * forcaMagnetica);

        if (usarWheelColliders)
        {
            // Lógica de Tank Drive para os WheelColliders
            float forcaEsq = (aceleracao + giro) * forcaMotor;
            float forcaDir = (aceleracao - giro) * forcaMotor;

            foreach (var roda in rodasEsquerdas) roda.motorTorque = forcaEsq;
            foreach (var roda in rodasDireitas) roda.motorTorque = forcaDir;
        }
        else
        {
            // Lógica Arcade (Movimento Simples no Rigidbody)
            if (rb.linearVelocity.magnitude < velocidadeMaxima) 
            {
                rb.AddRelativeForce(Vector3.forward * aceleracao * forcaMotor);
                rb.AddRelativeTorque(Vector3.up * giro * forcaGiro);
            }
        }

        // Recompensas
        if (adversario != null)
        {
            float distanciaAdversario = Vector3.Distance(transform.position, adversario.position);
            AddReward(-distanciaAdversario * 0.001f); 
        }
        
        // Queda
        if (centroArena != null)
        {
            float distanciaDoCentro = Vector3.Distance(transform.position, centroArena.position);
            if (distanciaDoCentro > raioDaArena)
            {
                ArenaScript arena = FindFirstObjectByType<ArenaScript>();
                if (arena != null)
                {
                    arena.IniciarRound(this); 
                }
            }
        }
        
        AddReward(-0.0005f); 
    }

    public override void Heuristic(in ActionBuffers actionsOut) 
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Vertical"); 
        continuousActionsOut[1] = Input.GetAxis("Horizontal"); 
    }
}