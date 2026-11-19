using UnityEngine;
using UnityEngine.UI;
namespace MegaTanks
{
    public sealed class EnemyHealthBarUI : MonoBehaviour
    {
        [SerializeField] RectTransform display;
        [SerializeField] Image fill;
        [SerializeField] bool useEnemyColor=true;
        [SerializeField] bool fitArtworkWidth=true;
        [SerializeField, Min(0)] float artworkWidthFraction=.65f;
        [SerializeField] Vector2 widthLimits=new Vector2(.9f,1.8f);
        [SerializeField,Min(.1f)] float commandoWidth=.7f;
        [SerializeField] int commandoSortingOrder=80;
        public float HealthFraction=>fill.fillAmount;
        public Color FillColor=>fill.color;
        public void Configure(float top,float artworkWidth,Color color,Camera camera)
        {
            transform.localPosition=new Vector3(0,top,0);
            GetComponent<Canvas>().worldCamera=camera;
            if(fitArtworkWidth)display.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Mathf.Clamp(artworkWidth*artworkWidthFraction,widthLimits.x,widthLimits.y)/Mathf.Abs(transform.lossyScale.x));
            if(useEnemyColor)fill.color=color;SetHealth(1);
        }
        public void SetHealth(float fraction)=>fill.fillAmount=Mathf.Clamp01(fraction);
        public void ConfigureCommando(float top,Color color,Camera camera)
        {
            Configure(top,commandoWidth,color,camera);
            if(fitArtworkWidth)display.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,commandoWidth/Mathf.Abs(transform.lossyScale.x));
            var canvas=GetComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=commandoSortingOrder;
        }
    }
}
