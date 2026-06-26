#!/bin/bash
# ============================================
# 🌿 YBYTU: A IRA DA FLORESTA
# Script para criar issues automaticamente no GitHub
# Autor: Pablo Guilherme
# ============================================

REPO="PabloGJBS/YbytuAIraDaFloresta"

echo "🌱 Criando issues no repositório $REPO..."
echo ""

# Carrega uma única vez os títulos das issues existentes (open + closed)
# para evitar duplicatas em execuções repetidas do script.
echo "🔎 Carregando issues existentes de $REPO..."
EXISTING_TITLES=$(gh issue list --repo "$REPO" --state all --limit 500 --json title --jq '.[].title')
echo ""

# Função auxiliar
create_issue() {
  local title="$1"
  local body="$2"
  local labels="$3"

  # Guard: pula se já existir issue com o mesmo título (case-sensitive, match exato)
  if printf '%s\n' "$EXISTING_TITLES" | grep -Fxq "$title"; then
    echo "⏭️  Já existe, pulando: $title"
    return 0
  fi

  echo "📦 Criando issue: $title"
  if gh issue create --repo "$REPO" --title "$title" --body "$body" --label "$labels"; then
    # Atualiza cache local para evitar duplicatas na mesma execução
    EXISTING_TITLES=$(printf '%s\n%s' "$EXISTING_TITLES" "$title")
  fi
  sleep 2
}

# =========================
# === LISTA DE FEATURES ===
# =========================

create_issue "💡 Feature F0 – Template de Contribuição Aberta" \
"## 🌿 Contexto
O projeto Ybytu: A Ira da Floresta será um jogo de código aberto, incentivando a colaboração da comunidade para aprimorar arte, mecânicas e narrativa.

## 🎯 Objetivo
Criar e documentar o modelo de contribuição (issue template e guia de boas práticas) que servirá como base para contribuições externas.

## 📘 Ações esperadas
- Adicionar templates de *issues* e *pull requests* em \`.github/ISSUE_TEMPLATE/\`
- Criar arquivo \`CONTRIBUTING.md\` explicando padrões de código e fluxo de contribuição
- Garantir que todos os PRs passem pelas regras e Actions definidas

> Esta issue representa o início da jornada open source do projeto Ybytu 🌳" \
"documentation,open-source,alpha-version,to do"


# ---------- ALPHA ----------

create_issue "🎨 Feature F1 – Arte Base do Jogo" \
"## 🌿 Contexto
O espírito da floresta dá forma a Ybytu.

## 🎯 Objetivo
Criar sprites de personagem principal, inimigos e cenários da floresta com identidade amazônica." \
"feature,art,alpha-version,to do"

create_issue "🎵 Feature F2 – Trilha Sonora e Efeitos Sonoros" \
"## 🌿 Contexto
Os sons da floresta guiam o jogador em sua jornada.

## 🎯 Objetivo
Produzir trilha sonora e efeitos ambientais (vento, chuva, passos, ataques)." \
"feature,audio,alpha-version,to do"

create_issue "📜 Feature F3 – Narrativa e Prólogo" \
"## 🌿 Contexto
Ybytu desperta após o massacre de sua tribo, guiado pela arara sagrada.

## 🎯 Objetivo
Desenvolver a introdução do jogo mostrando o massacre e o despertar do herói." \
"feature,narrative,alpha-version,to do"

create_issue "⚙️ Feature F4 – Estrutura Básica do Gameplay" \
"## 🌿 Contexto
Ybytu aprende a se mover pela floresta.

## 🎯 Objetivo
Implementar movimentação, pulos e colisões básicas do personagem." \
"feature,gameplay,alpha-version,to do"

create_issue "🧭 Feature F5 – Tutorial de Gameplay e Introdução Interativa" \
"## 🌿 Contexto
A arara espiritual ensina os primeiros passos ao guerreiro.

## 🎯 Objetivo
Criar uma fase inicial guiada para ensinar comandos ao jogador." \
"feature,gameplay,alpha-version,to do"

create_issue "🕹️ Feature F6 – Tela de Pausa e Menu de Configurações" \
"## 🌿 Contexto
O guerreiro precisa descansar entre as batalhas.

## 🎯 Objetivo
Implementar tela de pausa com opções de som e retorno ao menu principal." \
"feature,ui-ux,alpha-version,to do"

# ---------- BETA ----------
create_issue "⚔️ Feature F7 – Sistema de Combate" \
"## 🌿 Contexto
Ybytu canaliza a força ancestral para lutar.

## 🎯 Objetivo
Adicionar ataques leves e pesados, combos e sistema de dano responsivo." \
"feature,gameplay,beta-version,to do"

create_issue "👺 Feature F8 – Inimigos e IA Básica" \
"## 🌿 Contexto
Os invasores destroem a floresta; o herói contra-ataca.

## 🎯 Objetivo
Criar inimigos com comportamento de patrulha, ataque e reação ao jogador." \
"feature,gameplay,beta-version,to do"

create_issue "🪓 Feature F9 – Criação do Primeiro Chefe de Fase" \
"## 🌿 Contexto
O primeiro guardião da floresta desperta para testar o guerreiro.

## 🎯 Objetivo
Desenvolver chefe com padrões de ataque únicos e cutscene de introdução." \
"feature,gameplay,beta-version,to do"

create_issue "🌲 Feature F10 – Mensagens Educativas Interativas" \
"## 🌿 Contexto
Totens espirituais ensinam sobre respeito à natureza.

## 🎯 Objetivo
Adicionar totens e diálogos sobre sustentabilidade durante as fases." \
"feature,narrative,beta-version,to do"

create_issue "💥 Feature F11 – Sistema de Pontuação e Avaliação (D → SSS+)" \
"## 🌿 Contexto
A força de Ybytu é julgada pelos espíritos da floresta.

## 🎯 Objetivo
Criar sistema de pontuação e ranks baseados em performance, combos e dano sofrido." \
"feature,gameplay,beta-version,to do"

create_issue "🏆 Feature F12 – Sistema de Ranking Global" \
"## 🌿 Contexto
Os guerreiros são reconhecidos por sua bravura.

## 🎯 Objetivo
Adicionar ranking global de pontuações máximas." \
"feature,gameplay,beta-version,to do"

# ---------- RELEASE ----------
create_issue "🗺️ Feature F13 – Tela de Mapa com Fases Desbloqueáveis" \
"## 🌿 Contexto
O herói percorre o mapa ancestral da floresta.

## 🎯 Objetivo
Criar mapa de progressão com opção de revisitar fases anteriores." \
"feature,ui-ux,release-version,to do"

create_issue "🔥 Feature F14 – Segunda Fase: A Floresta em Chamas" \
"## 🌿 Contexto
A floresta agoniza e Ybytu enfrenta o fogo e a destruição.

## 🎯 Objetivo
Desenvolver nova fase com cenário devastado e novos inimigos." \
"feature,gameplay,release-version,to do"

create_issue "🌻 Feature F15 – Sistema de Reflorestamento e Recompensa" \
"## 🌿 Contexto
Cada vitória restaura um fragmento da vida na floresta.

## 🎯 Objetivo
Implementar sistema de regeneração da natureza após derrotar inimigos." \
"feature,gameplay,release-version,to do"

create_issue "🍃 Feature F16 – Sistema de Itens Utilizáveis e Colecionáveis" \
"## 🌿 Contexto
Ybytu encontra artefatos e ervas mágicas.

## 🎯 Objetivo
Permitir coleta e uso de itens especiais (cura, poder, sementes espirituais)." \
"feature,gameplay,release-version,to do"

create_issue "🌍 Feature F17 – Sistema de Troca de Idioma" \
"## 🌿 Contexto
A mensagem da floresta deve ecoar em várias línguas.

## 🎯 Objetivo
Adicionar suporte multilíngue (português, inglês, espanhol)." \
"feature,ui-ux,release-version,to do"

create_issue "🕊️ Feature F18 – Animação Final com Mensagem sobre Sustentabilidade" \
"## 🌿 Contexto
O ciclo da natureza se completa e a floresta renasce.

## 🎯 Objetivo
Criar animação final poética com mensagem ambiental." \
"feature,narrative,release-version,to do"

# ---------- ALPHA – FUNDAÇÃO TÉCNICA E UI ESTRUTURAL ----------

create_issue "🧱 Feature F19 – Setup Técnico do Projeto" \
"## 🌿 Contexto
Antes de Ybytu caminhar pela floresta, é preciso preparar a terra onde a jornada acontecerá.

## 🎯 Objetivo
Estabelecer a fundação técnica do projeto Unity 2D para que as demais features da alpha tenham uma base consistente.

## 📘 Ações esperadas
- Configurar URP 2D Renderer e camadas (\`Player\`, \`Enemy\`, \`Ground\`, \`Hazard\`, \`Interactable\`)
- Padronizar estrutura de pastas (\`Assets/Art\`, \`Audio\`, \`Prefabs\`, \`Scripts/Core\`, \`Scripts/Gameplay\`, \`UI\`)
- Ativar e configurar Git LFS para sprites, áudio e texturas
- Revisar \`.gitignore\` do Unity e arquivos de projeto
- Documentar convenções no \`CONTRIBUTING.md\`" \
"feature,gameplay,alpha-version,to do,priority: high"

create_issue "🎮 Feature F20 – Sistema de Input (Input Actions)" \
"## 🌿 Contexto
Os gestos do guerreiro precisam de uma linguagem clara entre jogador e mundo.

## 🎯 Objetivo
Criar o asset de Input Actions do novo Input System como base para movimento, combate, pausa e interação.

## 📘 Ações esperadas
- Criar \`PlayerControls.inputactions\` com mapas \`Gameplay\` e \`UI\`
- Definir ações: Move, Jump, Attack, Interact, Pause
- Gerar classe C# e expor eventos para os controllers
- Suporte a teclado/mouse e gamepad" \
"feature,gameplay,alpha-version,to do"

create_issue "📷 Feature F21 – Câmera 2D com Seguimento" \
"## 🌿 Contexto
Os olhos da floresta acompanham cada passo do guerreiro.

## 🎯 Objetivo
Configurar uma câmera 2D que siga o personagem com suavidade e respeite os limites da fase.

## 📘 Ações esperadas
- Implementar câmera com Cinemachine 2D (\`CinemachineVirtualCamera\` + \`Framing Transposer\`)
- Configurar dead zone e damping
- Adicionar \`Confiner2D\` para limitar a área visível
- Preparar hooks para shake em eventos futuros (dano, impacto)" \
"feature,gameplay,alpha-version,to do"

create_issue "🎬 Feature F22 – Fluxo de Cenas e SceneManager" \
"## 🌿 Contexto
A jornada de Ybytu é contada em atos, cada cena é uma página da floresta.

## 🎯 Objetivo
Criar um gerenciador de cenas e o fluxo básico entre Boot, Menu, Prólogo, Fase 1 e Game Over.

## 📘 Ações esperadas
- Criar cenas \`Boot\`, \`MainMenu\`, \`Prologo\`, \`Fase1\`, \`GameOver\`
- Implementar \`SceneLoader\` com carregamento assíncrono e tela de transição (fade)
- Cena \`Boot\` inicializa managers persistentes (Audio, Settings, Input)
- Documentar ordem no Build Settings" \
"feature,gameplay,alpha-version,to do"

create_issue "💾 Feature F23 – Persistência de Configurações" \
"## 🌿 Contexto
As escolhas do jogador devem ser lembradas pela floresta entre visitas.

## 🎯 Objetivo
Implementar camada de persistência para configurações (áudio, idioma, controles) usada por F6 e futuras features.

## 📘 Ações esperadas
- Criar \`SettingsManager\` (singleton persistente) salvando em \`PlayerPrefs\` ou JSON
- Expor volume de Master/Music/SFX via AudioMixer
- Suporte para salvar idioma (preparando F17) e rebinds de controle
- Carregar configurações na cena Boot" \
"feature,ui-ux,alpha-version,to do"

create_issue "🏠 Feature F24 – Tela de Menu Principal" \
"## 🌿 Contexto
O portal da floresta recebe o jogador com o canto dos pássaros.

## 🎯 Objetivo
Criar o menu principal do jogo como primeira tela interativa após o splash.

## 📘 Ações esperadas
- Opções: Novo Jogo, Continuar, Configurações, Créditos, Sair
- Arte/ambientação alinhada à identidade amazônica (placeholder aceitável)
- Navegação por teclado e gamepad
- Integração com \`SceneLoader\` (F22) e \`SettingsManager\` (F23)" \
"feature,ui-ux,alpha-version,to do"

create_issue "🎞️ Feature F25 – Tela de Splash e Logo" \
"## 🌿 Contexto
Antes da jornada começar, o espírito da floresta se apresenta.

## 🎯 Objetivo
Criar uma tela de splash inicial com logo do projeto e créditos de autoria do TCC.

## 📘 Ações esperadas
- Tela exibida na cena \`Boot\` antes do Menu Principal
- Logo do jogo, créditos (autor/TCC), logo Unity
- Fade in/out e duração configurável
- Skip via input após carregamento mínimo" \
"feature,ui-ux,alpha-version,to do"

create_issue "❤️ Feature F26 – HUD Básica In-Game" \
"## 🌿 Contexto
O guerreiro precisa ouvir o pulsar do próprio coração durante a batalha.

## 🎯 Objetivo
Criar uma HUD mínima que mostre vida, item atual e indicador de objetivo durante o gameplay.

## 📘 Ações esperadas
- Barra (ou ícones) de vida do jogador
- Slot de item/poder atual
- Indicador textual de objetivo/tutorial (consumido por F5)
- Canvas escalável em múltiplas resoluções" \
"feature,ui-ux,alpha-version,to do"

create_issue "💬 Feature F27 – Sistema de Diálogo e Caixa de Texto" \
"## 🌿 Contexto
A arara sagrada sussurra ensinamentos antigos ao guerreiro.

## 🎯 Objetivo
Construir um sistema de diálogo reutilizável que sirva de base para o prólogo (F3) e o tutorial (F5).

## 📘 Ações esperadas
- Caixa de texto com efeito typewriter e avanço por input
- Suporte a múltiplas páginas e falante nomeado (ex.: Arara, Ybytu)
- Dados de diálogo em ScriptableObject ou JSON (preparando localização F17)
- API simples para disparar diálogo a partir de qualquer script" \
"feature,narrative,alpha-version,to do"

create_issue "📘 Feature F28 – Templates de Issue/PR e Guia de Contribuição" \
"## 🌿 Contexto
Para que outros caminhantes possam trilhar a floresta junto com Ybytu, é preciso demarcar o caminho.

## 🎯 Objetivo
Adicionar os templates de issue e pull request e o guia de contribuição ao repositório.

## 📘 Ações esperadas
- Criar \`.github/ISSUE_TEMPLATE/feature.md\` e \`bug_report.md\` com campos Contexto / Objetivo / Ações esperadas / Critérios de aceitação
- Criar \`.github/PULL_REQUEST_TEMPLATE.md\`
- Escrever \`CONTRIBUTING.md\` com padrões de commit, branches e revisão
- Referenciar labels e milestones existentes" \
"documentation,alpha-version,to do"


echo ""
echo "✅ Todas as issues foram criadas com sucesso!"
