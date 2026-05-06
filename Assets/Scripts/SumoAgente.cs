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
    public Transform[] visualRodasEsquerdas;
    public Transform[] visualRodasDireitas;

    [Header("Configurações da Arena")]
    public ArenaScript arena;
    public Transform centroArena; 
    private Rigidbody rb;
    private Vector3 posicaoInicialLocal;
    private Quaternion rotacaoInicialLocal;
    private Transform adversario;
    private bool isGrounded;

    public override void Initialize() 
    {
        rb = GetComponent<Rigidbody>();
        posicaoInicialLocal = transform.localPosition;
        rotacaoInicialLocal = transform.localRotation;
        foreach(GameObject g in GameObject.FindGameObjectsWithTag("Lutador")){
            if(g != gameObject){
                adversario = g.transform;
                break;
            }
        }
        if(adversario == null){
            Debug.LogWarning($"[{name}] Nenhum adversário com a tag 'Lutador' foi encontrado!");
        }
    }

    public override void OnEpisodeBegin() 
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.localPosition = posicaoInicialLocal;
        transform.localRotation = rotacaoInicialLocal;
        isGrounded = false;
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
            Vector3 minhaDistanciaDoCentro = transform.localPosition - centroArena.localPosition;
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
        sensor.AddObservation(transform.forward.x);
        sensor.AddObservation(transform.forward.z);

        if (adversario != null)
        {
            Vector3 dirAdversario = adversario.localPosition - transform.localPosition;
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

        if(isGrounded){
            rb.AddForce(-transform.up * forcaMagnetica, ForceMode.Force);
        }
        if (usarWheelColliders){
            float forcaEsq = (aceleracao + giro) * forcaMotor;
            float forcaDir = (aceleracao - giro) * forcaMotor;
            foreach (var roda in rodasEsquerdas) roda.motorTorque = forcaEsq;
            foreach (var roda in rodasDireitas) roda.motorTorque = forcaDir;
        }
        else{
            if (rb.linearVelocity.magnitude < velocidadeMaxima) 
            {
                rb.AddRelativeForce(Vector3.forward * aceleracao * forcaMotor);
                rb.AddRelativeTorque(Vector3.up * giro * forcaGiro);
            }
            Vector3 velLocal = transform.InverseTransformDirection(rb.linearVelocity);
            velLocal.x = 0f;
            rb.linearVelocity = transform.TransformDirection(velLocal);
        }
        if (adversario != null){
            AddReward(-Vector3.Distance(transform.localPosition, adversario.localPosition)*0.001f);
            Vector3 direcaoAdversario = (adversario.localPosition - transform.localPosition).normalized;
            float alinhamento = Vector3.Dot(transform.forward, direcaoAdversario);
            if(alinhamento > 0.8f){
                AddReward(0.002f);
            }
        }
        AddReward(-0.0005f); 
        if(isGrounded && transform.localPosition.y < -2f){
            AddReward(-1f);
            EndEpisode();
        }   
    }
    private void AtualizarVisualRodas(){
        if (usarWheelColliders)
        {
            if(!usarWheelColliders) return;
            AtualizarMeshesRodas(rodasEsquerdas, visualRodasEsquerdas);
            AtualizarMeshesRodas(rodasDireitas, visualRodasDireitas);
        }
    }
    private void AtualizarMeshesRodas(WheelCollider[] colliders, Transform[] visuals)
    {
        if(colliders == null || visuals ==null) return;
        int count = Mathf.Min(colliders.Length, visuals.Length);
        for(int i=0; i < count; i++)
        {
            if(colliders[i] == null || visuals[i] == null) continue;
            colliders[i].GetWorldPose(out Vector3 pos, out Quaternion rot);
            visuals[i].SetPositionAndRotation(pos, rot);

        }
    }
    private void OnTriggerEnter(Collider other){
        if(other.CompareTag("Borda")){
            if (arena != null)
            {
                arena.IniciarRound(this);
            }
            else
            {
                Debug.LogWarning($"[{name}] O script 'ArenaScript' não está atribuído no inspetor!");
                AddReward(-1f);
                EndEpisode();
            }
        }
    }

    private void OnCollisionEnter(Collision colisao){
        if(colisao.collider.CompareTag("Piso")){
            isGrounded = true;
        }
    }
    private void OnCollisionExit(Collision colisao){
        if(colisao.collider.CompareTag("Piso")){
            isGrounded = false;
        }
    }
    public override void Heuristic (in ActionBuffers actionsOut){
        var ac = actionsOut.ContinuousActions;
        ac[0]=Input.GetAxis("Vertical");
        ac[1]=Input.GetAxis("Horizontal");
    }
    public void AdversarioCaiu(){
        AddReward(+1f);
        EndEpisode();
    }
}