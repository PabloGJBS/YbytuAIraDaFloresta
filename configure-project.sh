#!/bin/bash
# ============================================
# 🌿 YBYTU: A IRA DA FLORESTA
# Guia de Configuração do GitHub Project (Kanban)
# Autor: Pablo Guilherme
# ============================================
#
# ⚠️ NOTA IMPORTANTE:
# A API do GitHub Projects v2 NÃO suporta criar Views programaticamente.
# Este script contém apenas documentação dos passos manuais necessários.
#
# ============================================

PROJECT_URL="https://github.com/users/PabloGJBS/projects/2"

echo "🌿 Ybytu: A Ira da Floresta - Configuração do Kanban"
echo "============================================"
echo ""
echo "⚠️  A API do GitHub não permite criar Views automaticamente."
echo "    Siga os passos manuais abaixo para configurar seu projeto."
echo ""
echo "============================================"
echo "📋 PASSO 1: Criar as Views por Versão"
echo "============================================"
echo ""
echo "   1. Acesse: $PROJECT_URL"
echo "   2. Clique em '+ New view' (ao lado de 'My Items')"
echo "   3. Escolha 'Board' como layout"
echo "   4. Crie as suas views (exemplo abaixo deste projeto):"
echo ""
echo "      🌱 Roadmap Alpha"
echo "      ⚙️  Roadmap Beta"
echo "      🚀 Roadmap Release"
echo ""
echo "============================================"
echo "📋 PASSO 2: Configurar Filtros nas Views"
echo "============================================"
echo ""
echo "   Em cada view, adicione o filtro de label correspondente:"
echo ""
echo "   • Roadmap Alpha  → label:alpha-version"
echo "   • Roadmap Beta   → label:beta-version"
echo "   • Roadmap Release → label:release-version"
echo ""
echo "============================================"
echo "🌿 A floresta agradece sua organização!"
echo ""
