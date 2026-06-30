# 🤖 Projeto Robo Sumo - INCITE

Bem-vindo ao **Projeto Robo Sumo**! Este projeto utiliza aprendizado de máquina (Machine Learning) para treinar inteligências artificiais no controle de robôs de sumô dentro de uma arena virtual (Dohyo).

O treinamento é feito utilizando a engine da Unity em conjunto com o framework **ML-Agents**.

---

## 📋 Pré-requisitos

Para executar este projeto na sua máquina, você precisará das seguintes ferramentas instaladas:

- **Unity Engine**: Uma versão recente da Unity (v6000.0.44f1).
- **Anaconda** (ou Miniconda): Gerenciador de ambientes virtuais Python.
- **Unity ML-Agents**: Biblioteca do ML-Agents devidamente instalada no seu ambiente virtual do Anaconda.

---

## 🛠️ Como abrir o projeto na Unity

1. Abra o **Unity Hub** e adicione a pasta raiz do projeto que você baixou.
2. Abra o projeto na Unity.
3. Na janela inferior **Project**, navegue pelos diretórios:
   Assets ➔ Scenes
4. Dê um duplo clique no arquivo **SampleScene** para carregar a arena principal (Dohyo) e os robôs.

---

## 🚀 Como executar o Treinamento da IA

Para iniciar a comunicação entre a rede neural no Python e o ambiente 3D na Unity, siga os passos abaixo no seu **Anaconda Prompt**:

1. Ative o seu ambiente virtual do ML-Agents:
   conda activate mlagents

2. Navegue até o diretório onde o projeto está salvo na sua máquina (substitua o caminho abaixo pelo local correto no seu PC):
   cd "D:\LocalDoArquivo\RoboSumo"

3. Inicie o comando de treinamento utilizando o seu arquivo de configuração:
   mlagents-learn trainer_config.yaml --run-id=treino_7

4. **Iniciando a Simulação:**
   Assim que o terminal exibir a mensagem informando que está ouvindo a porta (ex: "Listening on port 5004"), **não feche o Anaconda**. Deixe a janela aberta em segundo plano.
   
5. Volte para a janela da **Unity** e pressione o botão **Play** no topo do editor. 

Pronto! Agora você poderá assistir aos robôs batalhando e aprendendo em tempo real.

---

## 📊 Como acompanhar o treinamento (TensorBoard)

Para visualizar os gráficos de inteligência e aprendizado da sua IA em tempo real (como pontuação e duração das lutas), utilize o TensorBoard:

1. Abra uma **nova** janela do Anaconda Prompt (não feche a janela que está rodando o treinamento da Unity).
2. Ative o ambiente virtual novamente:
   conda activate mlagents
3. Navegue até a mesma pasta do projeto:
   cd "D:\LocalDoArquivo\RoboSumo"
4. Digite o comando abaixo para iniciar o servidor do TensorBoard:
   tensorboard --logdir results
5. Abra o seu navegador de internet (Chrome, Edge, Opera, etc.) e acesse o seguinte endereço:
   http://localhost:6006

---

## 📝 Observações Importantes (Comandos e Configurações)

Durante o desenvolvimento e treinamento, você precisará manipular parâmetros e gerenciar sessões interrompidas. Abaixo estão os conceitos cruciais para o uso do ML-Agents:

### O Arquivo `.yaml` (Ex: `trainer_config.yaml`)
Este é o arquivo de configuração principal da sua IA. É nele que você define as "regras matemáticas" do cérebro do robô.
- É aqui que você ajusta o número máximo de passos do treino (`max_steps`), a taxa de aprendizado (`learning_rate`) e os módulos de recompensa (como os sinais extrínsecos e intrínsecos/curiosidade). 
- O nome do comportamento (ex: *Lutador* ou *SumoBrain*) dentro deste arquivo deve ser **exatamente igual** ao campo *Behavior Name* no Inspector da Unity.

### O parâmetro `--force` (Começar do Zero)
Se você tentar rodar um treino com um `--run-id` que já existe (ex: `treino_7`), o Anaconda dará um erro para proteger seus dados antigos. 
Se você quiser **apagar** o progresso antigo e começar um treino do zero usando o mesmo nome, adicione `--force` ao final do comando:
   mlagents-learn trainer_config.yaml --run-id=treino_7 --force

### O parâmetro `--resume` (Continuar Treinamento)
Se você fechou a Unity, o computador desligou, ou se você apenas quis pausar o treinamento para avaliar os resultados, você pode **continuar exatamente de onde parou**. 
Para carregar o cérebro da última sessão salva e retomar o aprendizado usando o mesmo nome, adicione `--resume` ao final do comando:
   mlagents-learn trainer_config.yaml --run-id=treino_7 --resume
