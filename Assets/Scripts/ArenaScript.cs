using UnityEngine;

public class ArenaScript : MonoBehaviour
{
    public SumoAgente Destino; 
    public SumoAgente Golden;
    
    [Header("Configurações do Torneio")]
    [Tooltip("Ative para treinar a IA infinitamente. Desative para simular um torneio de rounds limitados.")]
    public bool modoTreino = true;
    public int totalRounds = 3;

    [Header("Regras de Tempo (Anti-Empate por Steps)")]
    [Tooltip("Quantidade máxima de passos físicos (FixedUpdate) até o round ser declarado empate. 1500 passos = ~30s de simulação.")]
    public int maxStepsPorRound = 1500; 
    private int stepsAtual = 0;

    public int roundAtual = 1;
    public int VitoriasDestino = 0;
    public int VitoriasGolden = 0;
    private bool roundEmAndamento = true; 

    private void FixedUpdate()
    {
        if (roundEmAndamento)
        {
            stepsAtual++;

            if (stepsAtual >= maxStepsPorRound)
            {
                DeclararEmpate();
            }
        }
    }

    public void IniciarRound(SumoAgente perdedor)
    {
        if (!roundEmAndamento) return; 
        roundEmAndamento = false; 
        
        SumoAgente vencedor = (perdedor == Destino) ? Golden : Destino;
        
        perdedor.AddReward(-1f); 
        vencedor.AddReward(1f); 

        if (vencedor == Destino) VitoriasDestino++;
        else VitoriasGolden++;

        Debug.Log($"Round {roundAtual} Finalizado - Vencedor: {(vencedor == Destino ? "Destino" : "Golden")} | Placar: {VitoriasDestino} x {VitoriasGolden}");
        
        FinalizarOuProximo();
    }

    public void DeclararEmpate()
    {
        if (!roundEmAndamento) return;
        roundEmAndamento = false;

        Destino.AddReward(-0.5f);
        Golden.AddReward(-0.5f);

        Debug.Log($"Round {roundAtual} Finalizado - EMPATE por limite de passos!");

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
        roundEmAndamento = true;
    }

    private void FinalizarPartida()
    {
        Debug.Log($"Sessão Finalizada - Placar Final: Destino [{VitoriasDestino}] x [{VitoriasGolden}] Golden");
        
        if (VitoriasDestino > VitoriasGolden)
            Debug.Log("Destino é o grande vencedor!");
        else if (VitoriasGolden > VitoriasDestino)
            Debug.Log("Golden é o grande vencedor!");
        else
            Debug.Log("O Torneio terminou em Empate Geral!");

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}