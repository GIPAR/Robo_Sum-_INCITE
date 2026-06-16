using System.Collections;
using UnityEngine;

public class SumoAnimacaoVermelho : MonoBehaviour
{
    [Header("Configuração Visual")]
    public float duracaoAnimacao = 0.6f;

    [Range(0f, 1f)]
    public float intensidadeVermelho = 1f;

    [Header("Curva de Animação")]
    public AnimationCurve curvaAnimacao;
    private Renderer[]             _renderers;
    private Color[]                _coresOriginais;
    private Coroutine              _animacaoAtiva;
    private MaterialPropertyBlock  _propBlock;

    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColor     = Shader.PropertyToID("_BaseColor"); 
    private static readonly int Color_        = Shader.PropertyToID("_Color");     

    private void Awake()
    {
        _renderers      = GetComponentsInChildren<Renderer>();
        _coresOriginais = new Color[_renderers.Length];
        _propBlock      = new MaterialPropertyBlock(); 

        for (int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].GetPropertyBlock(_propBlock);
            _coresOriginais[i] = _renderers[i].sharedMaterial.color;
        }

        if (curvaAnimacao == null || curvaAnimacao.length == 0)
        {
            curvaAnimacao = new AnimationCurve(
                new Keyframe(0f,    0f,  0f,  4f),
                new Keyframe(0.25f, 1f),
                new Keyframe(1f,    0f, -2f,  0f)
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Borda")) return;

        if (_animacaoAtiva != null)
            StopCoroutine(_animacaoAtiva);

        _animacaoAtiva = StartCoroutine(AnimarVermelho());
    }

    private IEnumerator AnimarVermelho()
    {
        float tempo = 0f;

        while (tempo < duracaoAnimacao)
        {
            tempo += Time.deltaTime;
            float fator = curvaAnimacao.Evaluate(Mathf.Clamp01(tempo / duracaoAnimacao));

            for (int i = 0; i < _renderers.Length; i++)
            {
                Color corAtual = Color.Lerp(
                    _coresOriginais[i],
                    new Color(intensidadeVermelho, 0f, 0f, _coresOriginais[i].a),
                    fator
                );
                _renderers[i].GetPropertyBlock(_propBlock);
                _propBlock.SetColor(Color_, corAtual);
                _propBlock.SetColor(BaseColor, corAtual); 

                if (_renderers[i].sharedMaterial.IsKeywordEnabled("_EMISSION"))
                    _propBlock.SetColor(EmissionColor, corAtual * (fator * 2f));

                _renderers[i].SetPropertyBlock(_propBlock);
            }

            yield return null;
        }

        ResetarCores();
        _animacaoAtiva = null;
    }
    private void ResetarCores()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].GetPropertyBlock(_propBlock);
            _propBlock.SetColor(Color_,     _coresOriginais[i]);
            _propBlock.SetColor(BaseColor,  _coresOriginais[i]);
            _propBlock.SetColor(EmissionColor, Color.black);
            _renderers[i].SetPropertyBlock(_propBlock);
        }
    }
    private void OnDisable()
    {
        if (_animacaoAtiva != null)
        {
            StopCoroutine(_animacaoAtiva);
            _animacaoAtiva = null;
        }
        ResetarCores();
    }
}