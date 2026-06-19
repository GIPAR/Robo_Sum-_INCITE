using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class SumoAgente : Agent
{
    [Header("Especificações do Agente")]
    public float forcaMotor       = 280f;
    public float forcaGiro        = 15f;
    public float velocidadeMaxima = 8f;
    public float forcaMagnetica   = 15f;

    [Header("Configurações da Arena")]
    public ArenaScript arena;
    public Transform   centroArena;
    public float       raioDaArena = 10f;

    [Header("Recompensas")]
    public float recompensaEmpurrao    = 0.003f;
    public float recompensaOrientacao  = 0.0003f;
    public float penaltiaGiroExcessivo = 0.0005f;
    public float recompensaEmbate      = 0.01f;

    // removidos: recompensaContatoContinuo, distanciaExploracaoMinima
    // reward de vitória/derrota é responsabilidade do ArenaScript

    private Rigidbody       rb;
    private Rigidbody       rbAdversario;
    private Transform       adversario;
    private Vector3         posicaoInicialLocal;
    private Quaternion      rotacaoInicialLocal;
    private Vector3         posAdversarioAnterior;
    private bool            emContatoComAdversario;
    private bool            podeMovimentar = false;
    private WheelCollider[] rodas;

    // ─────────────────────────────────────────
    public override void Initialize()
    {
        rb    = GetComponent<Rigidbody>();
        rodas = GetComponentsInChildren<WheelCollider>();

        rb.centerOfMass   = new Vector3(0f, -0.25f, 0.05f);
        rb.sleepThreshold = 0f;
        rb.mass           = 3.1f;   // fixo — sem variação por episódio

        posicaoInicialLocal = transform.localPosition;
        rotacaoInicialLocal = transform.localRotation;

        BuscarAdversario();
    }

    private void BuscarAdversario()
    {
        foreach (GameObject g in GameObject.FindGameObjectsWithTag("Lutador"))
        {
            if (g == gameObject) continue;
            adversario   = g.transform;
            rbAdversario = g.GetComponent<Rigidbody>();
            return;
        }
        Debug.LogWarning($"[{gameObject.name}] Adversário não encontrado!");
    }

    public override void OnEpisodeBegin()
    {
        StopAllCoroutines();
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        emContatoComAdversario = false;
        podeMovimentar         = false;

        if (adversario == null) BuscarAdversario();

        foreach (WheelCollider roda in rodas)
        {
            roda.enabled     = false;
            roda.motorTorque = 0f;
            roda.brakeTorque = 0f;
        }

        transform.localPosition = posicaoInicialLocal + new Vector3(0f, 0.12f, 0f);
        transform.localRotation = rotacaoInicialLocal
            * Quaternion.Euler(0f, Random.Range(-20f, 20f), 0f);

        foreach (WheelCollider roda in rodas)
            roda.enabled = true;

        if (adversario != null)
            posAdversarioAnterior = adversario.localPosition;

        StartCoroutine(AguardarFisica());
    }

    private System.Collections.IEnumerator AguardarFisica()
    {
        for (int i = 0; i < 5; i++)
            yield return new WaitForFixedUpdate();

        float timeout = 0f;
        while (timeout < 1.5f)
        {
            if (VerificarChao())
            {
                podeMovimentar = true;
                yield break;
            }
            timeout += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        podeMovimentar = true;
    }

    // ─────────────────────────────────────────
    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 centro = centroArena != null ? centroArena.localPosition : Vector3.zero;
        Vector3 delta  = transform.localPosition - centro;

        sensor.AddObservation(delta.x / raioDaArena);
        sensor.AddObservation(delta.z / raioDaArena);

        Vector3 velLocal = transform.InverseTransformDirection(rb.linearVelocity);
        sensor.AddObservation(velLocal.x / velocidadeMaxima);
        sensor.AddObservation(velLocal.z / velocidadeMaxima);
        sensor.AddObservation(transform.forward.x);
        sensor.AddObservation(transform.forward.z);

        if (adversario != null)
        {
            Vector3 dirAdv = adversario.localPosition - transform.localPosition;
            sensor.AddObservation(dirAdv.normalized.x);
            sensor.AddObservation(dirAdv.normalized.z);
            sensor.AddObservation(dirAdv.magnitude / (raioDaArena * 2f));
            sensor.AddObservation(
                Vector3.Distance(adversario.localPosition, centro) / raioDaArena
            );

            if (rbAdversario != null)
            {
                Vector3 dirBordaAdv = (adversario.localPosition - centro).normalized;
                float   velAdvBorda = Vector3.Dot(rbAdversario.linearVelocity, dirBordaAdv);
                sensor.AddObservation(Mathf.Clamp(velAdvBorda / velocidadeMaxima, -1f, 1f));
            }
            else sensor.AddObservation(0f);
        }
        else
        {
            for (int i = 0; i < 5; i++) sensor.AddObservation(0f);
        }
    }

    private bool VerificarChao()
    {
        foreach (WheelCollider roda in rodas)
            if (roda != null && roda.isGrounded) return true;
        return false;
    }

    // ─────────────────────────────────────────
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!podeMovimentar) return;

        float   aceleracao = actions.ContinuousActions[0];
        float   giro       = actions.ContinuousActions[1];
        Vector3 angVel     = rb.angularVelocity;

        // ── Estabilização angular ──
        float   fatorEstab = emContatoComAdversario ? 30f : 15f;
        float   amortXZ    = emContatoComAdversario ? 8f  : 4f;
        Vector3 correcao   = Vector3.Cross(transform.up, Vector3.up);
        rb.AddTorque(correcao * fatorEstab, ForceMode.Force);
        rb.AddTorque(new Vector3(-angVel.x * amortXZ, 0f, -angVel.z * amortXZ), ForceMode.Force);

        bool isGrounded = VerificarChao();

        if (isGrounded)
        {
            rb.AddForce(Vector3.down * forcaMagnetica * rb.mass, ForceMode.Force);

            float fator = Mathf.Clamp01(1f - rb.linearVelocity.magnitude / velocidadeMaxima);
            rb.AddRelativeForce(Vector3.forward * aceleracao * forcaMotor * fator);
            rb.AddRelativeTorque(Vector3.up * giro * forcaGiro, ForceMode.Acceleration);
        }
        else if (rb.linearVelocity.y > 0.1f)
        {
            rb.AddForce(Vector3.down * 40f * rb.mass, ForceMode.Force);
        }

        // ── Downforce extra em contato ──
        if (emContatoComAdversario)
        {
            float velImpacto     = rb.linearVelocity.magnitude;
            float downforceExtra = Mathf.Lerp(10f, 40f, velImpacto / velocidadeMaxima);
            rb.AddForce(Vector3.down * downforceExtra * rb.mass, ForceMode.Force);

            if (rb.linearVelocity.y > 0f)
                rb.AddForce(
                    Vector3.down * rb.linearVelocity.y * rb.mass * 20f, ForceMode.Force
                );
        }

        // ── Anti-drift proporcional ──
        Vector3 velLocalDrift = transform.InverseTransformDirection(rb.linearVelocity);
        if (Mathf.Abs(velLocalDrift.x) > 0.05f)
        {
            float giroAbs    = Mathf.Clamp01(Mathf.Abs(giro));
            float fatorDrift = Mathf.Clamp(
                velLocalDrift.x * rb.mass * Mathf.Lerp(3f, 1f, giroAbs), -50f, 50f
            );
            rb.AddRelativeForce(Vector3.left * fatorDrift, ForceMode.Force);
        }

        // ── Penalidade base por step (incentiva terminar o round) ──
        AddReward(-0.001f);

        // ── Anti-spinning: penaliza giro no lugar sem deslocamento ──
        if (Mathf.Abs(angVel.y) > 1f && rb.linearVelocity.magnitude < 0.3f)
            AddReward(-penaltiaGiroExcessivo * Mathf.Abs(angVel.y));

        if (adversario == null || centroArena == null) return;

        Vector3 centro2     = centroArena.localPosition;
        Vector3 dirParaAdv  = (adversario.localPosition - transform.localPosition).normalized;
        float   alinhamento = Vector3.Dot(transform.forward, dirParaAdv);

        // ── Orientação: reward por encarar o adversário em movimento ──
        if (alinhamento > 0.5f && rb.linearVelocity.magnitude > 0.3f)
            AddReward(recompensaOrientacao * alinhamento);

        // ── Aproximação: reward por avançar em direção ao adversário ──
        if (!emContatoComAdversario)
        {
            float velAprox = Vector3.Dot(rb.linearVelocity, dirParaAdv);
            if (velAprox > 0.5f) AddReward(velAprox * 0.002f);
        }

        // ── Empurrão: reward por deslocar o adversário para a borda ──
        if (emContatoComAdversario)
        {
            Vector3 dirBorda = (adversario.localPosition - centro2).normalized;
            float   empurrao = Vector3.Dot(
                adversario.localPosition - posAdversarioAnterior, dirBorda
            );
            if (empurrao > 0f)
            {
                float distAdv     = Vector3.Distance(adversario.localPosition, centro2);
                float fatorPerigo = 1f - Mathf.Clamp01(distAdv / raioDaArena);
                AddReward(Mathf.Clamp(
                    empurrao * recompensaEmpurrao * (1f + fatorPerigo), 0f, 0.005f
                ));
            }
        }

        posAdversarioAnterior = adversario.localPosition;
    }

    // ─────────────────────────────────────────
    private void OnCollisionStay(Collision col)
    {
        if (!col.collider.CompareTag("Lutador")) return;

        foreach (ContactPoint contato in col.contacts)
        {
            if (contato.normal.y < -0.1f)
                rb.AddForce(
                    Vector3.down * Mathf.Abs(contato.normal.y) * forcaMagnetica * rb.mass * 2f,
                    ForceMode.Force
                );
        }
    }

    // Toque na borda → notifica a arena, que aplicará rewards e EndEpisode
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Borda")) arena?.IniciarRound(this);
    }

    private void OnCollisionEnter(Collision col)
    {
        if (col.collider.CompareTag("Lutador"))
        {
            emContatoComAdversario = true;

            float impacto = Mathf.Clamp(rb.linearVelocity.magnitude, 0f, 5f);
            AddReward(impacto * recompensaEmbate);

            if (adversario != null)
            {
                Vector3 dir = (adversario.localPosition - transform.localPosition).normalized;
                if (Vector3.Dot(transform.forward, dir) > 0.7f)
                    AddReward(0.02f * (impacto / 5f));
            }
        }
    }

    private void OnCollisionExit(Collision col)
    {
        if (col.collider.CompareTag("Lutador"))
            emContatoComAdversario = false;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ac = actionsOut.ContinuousActions;
        ac[0] = Input.GetAxis("Vertical");
        ac[1] = Input.GetAxis("Horizontal");
    }
}