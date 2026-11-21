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
            Panel(286,127,1028,646);Text(324,164,760,choiceTitle,title,76);
            Text(324,244,940,"Chọn một mục để áp dụng. Esc để giữ nguyên.",small);
            if(Button(1114,164,162,52,"Đóng"))modal="";
            Surface(312,286,976,460,Color.white,catsFrame:"Roster");
            BeginScroll(new Rect(326,300,948,432),scroll,new Rect(0,0,928,Mathf.Ceil(choiceLabels.Length/2f)*74));
            for(int i=0;i<choiceLabels.Length;i++)
                if(Button(i%2*466,i/2*74,450,62,choiceLabels[i],normalSpriteKey:"blue")) {modal="";choiceAction?.Invoke(i);}
            EndScroll();
        }
    }
}
