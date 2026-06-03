using UnityEngine;

public class ArenaScript : MonoBehaviour
{
    public SumoAgente Destino;
    public SumoAgente Golden;

    [Header("Configurações do Torneio")]
    public bool modoTreino = true;
    public int totalRounds = 3;

    [Header("Regras de Tempo")]
    public int maxStepsPorRound = 800;
    private int stepsAtual = 0;

    [Header("Recompensas de Round")]
    public float recompensaVitoria  =  1f;
    public float penaltiaDerrota    = -1f;
    public float penaltiaEmpate     = -0.3f;
    public int roundAtual = 1;
    public int VitoriasDestino = 0;
    public int VitoriasGolden  = 0;
    private bool roundEmAndamento = true;

    // NOVO: guard para chamadas duplas de IniciarRound no mesmo frame
    private bool resolvendoRound = false;

    private void FixedUpdate()
    {
        if (!roundEmAndamento) return;

        stepsAtual++;
        if (stepsAtual >= maxStepsPorRound)
            DeclararEmpate();
    }

    public void IniciarRound(SumoAgente perdedor)
    {
        if (!roundEmAndamento || resolvendoRound) return;
        resolvendoRound = true;
        roundEmAndamento = false;

        SumoAgente vencedor = (perdedor == Destino) ? Golden : Destino;

        perdedor.AddReward(penaltiaDerrota);
        vencedor.AddReward(recompensaVitoria);

        if (vencedor == Destino) VitoriasDestino++;
        else VitoriasGolden++;

        Debug.Log($"Round {roundAtual} — Vencedor: {(vencedor == Destino ? "Destino" : "Golden")} | {VitoriasDestino}x{VitoriasGolden}");

        FinalizarOuProximo();
    }

    public void DeclararEmpate()
    {
        if (!roundEmAndamento || resolvendoRound) return;
        resolvendoRound = true;
        roundEmAndamento = false;

        Destino.AddReward(penaltiaEmpate);
        Golden.AddReward(penaltiaEmpate);

        Debug.Log($"Round {roundAtual} — EMPATE por limite de steps.");
        FinalizarOuProximo();
    }

    private void FinalizarOuProximo()
    {
        roundAtual++;

        if (!modoTreino && roundAtual > totalRounds)
        {
            FinalizarPartida();
        }
        else
        {
            Destino.EndEpisode();
            Golden.EndEpisode();
            Invoke(nameof(PrepararProximoRound), 0.3f);
        }
    }

    private void PrepararProximoRound()
    {
        stepsAtual = 0;
        resolvendoRound = false; // NOVO: libera o guard
        roundEmAndamento = true;
    }

    private void FinalizarPartida()
    {
        string resultado = VitoriasDestino > VitoriasGolden ? "Destino vence!"
                         : VitoriasGolden > VitoriasDestino ? "Golden vence!"
                         : "Empate geral!";

        Debug.Log($"Sessão encerrada — {VitoriasDestino}x{VitoriasGolden} — {resultado}");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}