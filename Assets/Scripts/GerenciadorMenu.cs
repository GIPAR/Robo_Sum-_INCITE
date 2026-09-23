using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class GerenciadorMenu : MonoBehaviour
{
    [Header("Configuração de Reset")]
    public float tempoParaReset = 3f; 

    private float tempoPressionado = 0f;
    private InputDevice controladorEsquerdo;
    private readonly List<InputDevice> dispositivosMaoEsquerda = new List<InputDevice>();

    void Update()
    {
        if (!controladorEsquerdo.isValid)
        {
            InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, dispositivosMaoEsquerda);
            if (dispositivosMaoEsquerda.Count > 0)
            {
                controladorEsquerdo = dispositivosMaoEsquerda[0];
            }
        }

        if (!controladorEsquerdo.isValid)
        {
            tempoPressionado = 0f;
            return;
        }
        if (controladorEsquerdo.TryGetFeatureValue(CommonUsages.primaryButton, out bool xPressionado) && xPressionado)
        {
            tempoPressionado += Time.deltaTime;

            if (tempoPressionado >= tempoParaReset)
            {
                tempoPressionado = 0f;
                ResetarLobby();
            }
        }
        else
        {
            tempoPressionado = 0f;
        }
    }

    public void IniciarJogo()
    {
        SceneManager.LoadScene("ArenaVR");
    }

    public void ResetarLobby()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}