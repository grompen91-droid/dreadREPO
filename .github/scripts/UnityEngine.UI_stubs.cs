namespace UnityEngine.UI
{
    public class RawImage : MonoBehaviour
    {
        public Color color { get; set; }
        public RectTransform rectTransform { get; }
        public Texture2D texture { get; set; }
    }

    public class RectTransform : Component
    {
        public Vector2 sizeDelta { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector2 localScale { get; set; }
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
    }
}
