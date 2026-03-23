#!/bin/bash

# === YBYTU: A IRA DA FLORESTA ===
# Script de criação automática de labels no GitHub
# Autor: Pablo Guilherme 🌿

echo "🌿 Criando labels no repositório Ybytu: A Ira da Floresta..."

# ========= Tipo de Issue =========
gh label create "feature" --color "007FFF" --description "Nova funcionalidade ou melhoria de gameplay"
gh label create "bug" --color "D73A4A" --description "Erro, falha ou comportamento inesperado"
gh label create "enhancement" --color "2ECC71" --description "Ajuste visual, performance ou refinamento"
gh label create "documentation" --color "E4E669" --description "Atualização de documentação, README, TCC"
gh label create "audio" --color "F1C40F" --description "Efeitos sonoros, trilha sonora ou mixagem"
gh label create "art" --color "9B59B6" --description "Sprites, animações e elementos visuais"
gh label create "gameplay" --color "3498DB" --description "Mecânicas, balanceamento e lógica de jogo"
gh label create "narrative" --color "F39C12" --description "Textos, cutscenes, história e diálogos"
gh label create "ui-ux" --color "1ABC9C" --description "Interface, menus, HUD e experiência de jogador"

# ========= Workflow / Status =========
gh label create "to do" --color "2980B9" --description "A ser iniciada"
gh label create "in progress" --color "8E44AD" --description "Em desenvolvimento"
gh label create "review" --color "E67E22" --description "Aguardando revisão ou PR"
gh label create "done" --color "2ECC71" --description "Finalizada e testada"
gh label create "blocked" --color "C0392B" --description "Aguardando dependência ou recurso"

# ========= Versão / Ciclo =========
gh label create "alpha-version" --color "27AE60" --description "Fase inicial: fundação e gameplay básico"
gh label create "beta-version" --color "16A085" --description "Mecânicas avançadas, IA, ranking e fases"
gh label create "release-version" --color "145A32" --description "Conteúdo final, otimização e multilíngua"

# ========= Prioridade =========
gh label create "priority: high" --color "E74C3C" --description "Crítica: precisa ser feita antes das demais"
gh label create "priority: medium" --color "F39C12" --description "Importante, mas não urgente"
gh label create "priority: low" --color "27AE60" --description "Pode ser feita depois"

# ========= Temáticas Especiais =========
gh label create "🌿 espírito-da-floresta" --color "00A86B" --description "Tarefas que refletem a essência do projeto"
gh label create "🔥 ira-da-terra" --color "E25822" --description "Melhorias de combate, poder e performance"
gh label create "💧 sussurro-das-águas" --color "3FA7D6" --description "Refatorações e limpeza de código"
gh label create "🪶 sabedoria-da-arara" --color "F4C430" --description "Pesquisa, documentação e aprendizado"

echo "✅ Labels criadas com sucesso!"
