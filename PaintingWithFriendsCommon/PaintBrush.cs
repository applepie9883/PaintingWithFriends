using Microsoft.Xna.Framework;

namespace PaintingWithFriendsCommon
{
    public struct PaintBrush
    {
        private Color _brushColor;

        // TODO: Look at the setter for this, see what it's doing and find out if it is necessary
        public Color BrushColor
        {
            get { return _brushColor; }

            set { _brushColor = new Color(value.R, value.G, value.B, _brushColor.A); }
        }

        public float BrushSize { get; set; }

        public float Hardness { get; set; }

        private BrushType _brushType;

        public BrushType brushType
        {
            get
            {
                return _brushType;
            }

            set
            {
                _brushType = value;
            }
        }

        public void SetAlpha(byte value)
        {
            _brushColor.A = (byte)(value > 255 ? 255 : (value < 0 ? 0 : value));
        }

        public PaintBrush(Color c, byte alpha, float s, float h, BrushType b) : this()
        {
            BrushColor = c;
            SetAlpha(alpha);
            BrushSize = s;
            Hardness = h;
            brushType = b;
        }
    }

    public enum BrushType
    {
        circle,
        square
    };
}
