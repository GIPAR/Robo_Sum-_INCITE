using UnityEngine;

public class ArenaScript : MonoBehaviour
{
    public SumoAgente Agente1; 
    public SumoAgente Agente2;
    public bool modoTreino = true;
    public int totalRounds = 3;
    public int roundAtual = 1;
    public int VitoriasAgente1 = 0;
    public int VitoriasAgente2 = 0;

    public void IniciarRound(SumoAgente perdedor)
    {
        SumoAgente vencedor = (perdedor == Agente1) ? Agente2 : Agente1;

        // O perdedor caiu ou sofreu punição, e o vencedor ganha sua glória
        perdedor.AddReward(-1f); 
        vencedor.AddReward(1f); 

        if (vencedor == Agente1) VitoriasAgente1++;
        else VitoriasAgente2++;

        Debug.Log($"Round {roundAtual} Finalizado - Vencedor: {(vencedor == Agente1 ? "Agente 1" : "Agente 2")} | Placar: {VitoriasAgente1} x {VitoriasAgente2}");

        roundAtual++;
        if (modoTreino && roundAtual > totalRounds)
        {
            FinalizarPartida();
        }
        else
        {
            perdedor.EndEpisode();
            vencedor.EndEpisode();
        }
    }

    private void FinalizarPartida()
    {
        Debug.Log($"Sessão Finalizada - Placar Final: Robo 1 [{VitoriasAgente1}] x [{VitoriasAgente2}] Robo 2");
        
        if (VitoriasAgente1 > VitoriasAgente2)
            Debug.Log("Robo 1 é o grande vencedor!");
        else if (VitoriasAgente2 > VitoriasAgente1)
            Debug.Log("Robo 2 é o grande vencedor!");
        else
            Debug.Log("Empate!");

        // Pausa o jogo no editor para você ver os resultados
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}