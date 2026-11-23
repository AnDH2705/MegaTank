using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CatTanks
{
    public sealed partial class CatTanksGame
    {
        string choiceTitle;
        string[] choiceLabels;
        Action<int> choiceAction;
        void Pick(string heading,IEnumerable<string> labels,Action<int> accept)
        {
            choiceTitle=heading;choiceLabels=labels.ToArray();choiceAction=accept;modal="choice";scroll=Vector2.zero;
        }
        void ChoiceModal()
        {
            PopupFrame(286,150,1028,620,choiceTitle,true);
            Text(312,200,976,"Chọn một mục để áp dụng · chạm ra ngoài để giữ nguyên",small,28,align:TextAnchor.UpperCenter);
            Surface(312,246,976,500,Color.white,catsFrame:"Roster");
            BeginScroll(new Rect(326,260,948,472),scroll,new Rect(0,0,928,Mathf.Ceil(choiceLabels.Length/2f)*80));
            for(int i=0;i<choiceLabels.Length;i++)
                if(Button(i%2*466,i/2*80,450,68,choiceLabels[i],normalSpriteKey:"blue")) {modal="";choiceAction?.Invoke(i);}
            EndScroll();
        }
    }
}
