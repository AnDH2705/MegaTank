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
            Panel(330,140,940,620);Text(370,178,720,choiceTitle,title,75);
            Text(370,264,810,"Chọn một mục để áp dụng. Esc để giữ nguyên.",small);
            if(Button(1090,178,140,52,"Đóng"))modal="";
            BeginScroll(new Rect(370,318,860,385),scroll,new Rect(0,0,820,Mathf.Ceil(choiceLabels.Length/2f)*78));
            for(int i=0;i<choiceLabels.Length;i++)
                if(Button(i%2*413,i/2*78,395,64,choiceLabels[i])) {modal="";choiceAction?.Invoke(i);}
            EndScroll();
        }
    }
}
