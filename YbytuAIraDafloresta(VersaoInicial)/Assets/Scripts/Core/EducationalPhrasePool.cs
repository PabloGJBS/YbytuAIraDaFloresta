using System.Collections.Generic;
using UnityEngine;

public static class EducationalPhrasePool
{
    public static readonly string[] Phrases =
    {
        "A cada minuto, o Brasil perde uma área de floresta do tamanho de vários campos de futebol. O que levou séculos para crescer desaparece em segundos, e quase nunca volta a ser o que era.",
        "Uma floresta viva faz muito mais do que parecer bonita: ela puxa a chuva, segura o solo, esfria o ar e guarda o carbono que aquece o planeta. Derrubada, ela deixa de proteger todos nós, inclusive quem vive longe dela.",
        "Os povos originários são os maiores guardiões da floresta. Onde há terra indígena demarcada e respeitada, a mata continua de pé, porque para eles a terra não é mercadoria: é casa, é parente, é vida.",
        "As queimadas que aparecem nas notícias raramente são acidentes. A maioria é provocada de propósito para abrir espaço ao garimpo, à pecuária e à grilagem, transformando floresta viva em pasto e cinza.",
        "Uma única árvore pode abrigar centenas de espécies de insetos, aves, plantas e fungos que dependem só dela. Quando ela cai, não é uma árvore que se perde: é um mundo inteiro que se apaga.",
        "Reflorestar é devolver à terra o que foi tirado dela. Cada muda plantada hoje é sombra, fruto e abrigo para amanhã, e um lembrete de que destruir é rápido, mas reconstruir leva uma vida inteira.",
        "A água que chega limpa à sua torneira começa muito longe, na floresta. São as árvores que capturam a chuva, alimentam os rios voadores e mantêm as nascentes vivas. Sem mata, as torneiras secam, mesmo nas grandes cidades.",
    };

    private static List<string> bag;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        bag = null;
    }

    public static string Take()
    {
        if (bag == null || bag.Count == 0)
        {
            bag = new List<string>(Phrases);
            for (int i = bag.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (bag[i], bag[j]) = (bag[j], bag[i]);
            }
        }
        int last = bag.Count - 1;
        string phrase = bag[last];
        bag.RemoveAt(last);
        return phrase;
    }
}
