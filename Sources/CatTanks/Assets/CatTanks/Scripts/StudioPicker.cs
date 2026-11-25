using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CatTanks
{
    public sealed partial class CatTanksGame
    {
        string choiceTitle;
        string[] choiceLabels,choiceIcons;
        Action<int> choiceAction;
        // icons (tuỳ chọn): có hình thì popup hiện dạng lưới thẻ như danh sách chọn của game idle; không có thì dạng danh sách.
        void Pick(string heading,IEnumerable<string> labels,Action<int> accept,IEnumerable<string> icons=null)
        {
            choiceTitle=heading;choiceLabels=labels.ToArray();choiceIcons=icons?.ToArray();choiceAction=accept;modal="choice";scroll=Vector2.zero;
        }
        void ChoiceModal()
        {
            PopupFrame(286,150,1028,620,choiceTitle,true);
            Text(312,200,976,"Chọn một mục để áp dụng · chạm ra ngoài để giữ nguyên",small,28,align:TextAnchor.UpperCenter);
            Surface(312,246,976,500,Color.white,catsFrame:"Roster");
            bool grid=choiceIcons!=null&&choiceIcons.Length==choiceLabels.Length;
            BeginScroll(new Rect(326,260,948,472),scroll,new Rect(0,0,928,grid?Mathf.Ceil(choiceLabels.Length/3f)*186:Mathf.Ceil(choiceLabels.Length/2f)*80));
            for(int i=0;i<choiceLabels.Length;i++)
            {
                bool picked=grid
                    ?IllustratedButton(i%3*316,i/3*186,300,174,choiceLabels[i],choiceIcons[i],content:StudioButtonLayout.Content.Tile)
                    :Button(i%2*466,i/2*80,450,68,choiceLabels[i],normalSpriteKey:"blue");
                if(grid)Enlarge(18);
                if(picked){modal="";choiceAction?.Invoke(i);}
            }
            EndScroll();
        }
    }
}
