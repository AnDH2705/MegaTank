using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace MegaTanks
{
    public sealed partial class CanvasGameUI
    {
        void DrawMap()
        {
            var v=Show(UIScreen.Route);var stage=game.Stage;Text(v,"Route Stage",stage.Definition.stageName.ToUpperInvariant());Click(v,"Route Back",ConfirmHome);
            var scroll=v.Get<ScrollRect>("Route Scroll");var content=scroll.content;
            foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            float viewportWidth=scroll.viewport.rect.width,height=scroll.viewport.rect.height;
            float width=Mathf.Max(viewportWidth,stage.Route.rounds.Count*layout.roundSpacing);content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width);
            System.Func<StageNode,Vector2> point=node=>new Vector2(layout.routeHorizontalPadding+node.round*(width-2*layout.routeHorizontalPadding)/Mathf.Max(1,stage.Route.rounds.Count-1),layout.routeVerticalPadding+(1-Mathf.InverseLerp(.1f,.9f,node.height))*Mathf.Max(0,height-2*layout.routeVerticalPadding));
            foreach(var column in stage.Route.rounds)foreach(var node in column)foreach(int id in node.next)
            {
                var next=stage.Route.Get(id);bool available=node==stage.Current&&stage.CanSelect(id);
                var line=Instantiate(layout.routeConnection,content,false);line.name="Route Connection";var r=line.Get<RectTransform>("$root");var a=point(node);var delta=point(next)-a;
                r.anchoredPosition=new Vector2(a.x,-a.y);r.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,delta.magnitude);r.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
                line.Get<Image>("$root").color=available?layout.routeAvailableConnection:node.completed&&next.entered?layout.routeCompletedConnection:layout.routeFutureConnection;
            }
            foreach(var column in stage.Route.rounds)foreach(var node in column)
            {
                var p=point(node);bool current=node==stage.Current,available=stage.CanSelect(node.id);var type=node.type==StageNodeType.Random&&!node.entered?StageNodeType.Random:node.EncounterType;
                var item=Instantiate(layout.routeNode,content,false);item.name="Node "+node.id;var r=item.Get<RectTransform>("$root");r.anchoredPosition=new Vector2(p.x+r.rect.width*(r.pivot.x-.5f),-p.y+r.rect.height*(r.pivot.y-.5f));
                int id=node.id;Click(item,"$root",()=>stage.SelectNode(id));item.Get<Button>("$root").interactable=available;
                item.Get<Image>("$root").sprite=current?routeSkin.nodeCurrent:available?routeSkin.nodeAvailable:node.completed?routeSkin.nodeNormal:routeSkin.nodeLocked;
                item.Get<Image>("Route Node Icon").sprite=routeSkin.IconFor(type);item.Get<Image>("Route Node Icon").color=new Color(1,1,1,current||available?1:node.completed?.85f:.65f);
                Text(item,"Node Name",StageNodeIcons.Name(type));
                item.Get<Text>("Node Name").color=current?layout.routeCurrentText:available?layout.routeAvailableText:layout.routeFutureText;
                if(current)
                {
                    var marker=Instantiate(layout.routeTank,content,false);marker.name="Current Player Tank";var mr=marker.Get<RectTransform>("$root");
                    mr.anchoredPosition=new Vector2(p.x+layout.markerOffset.x+mr.rect.width*mr.pivot.x,-Mathf.Max(4,p.y+layout.markerOffset.y)-mr.rect.height*(1-mr.pivot.y));
                }
            }
            Canvas.ForceUpdateCanvases();scroll.horizontalNormalizedPosition=width>viewportWidth?Mathf.Clamp01((point(stage.Current).x-layout.routeHorizontalPadding)/(width-viewportWidth)):0;
        }
    }
}
