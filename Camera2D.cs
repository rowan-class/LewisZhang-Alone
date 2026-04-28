using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public class Camera2D
{
    private Rectangle _viewBounds = new(0, 0, WorldConfig.WorldWidth, WorldConfig.WorldHeight);

    public Rectangle ViewBounds => _viewBounds;

    public void ShowOverview()
    {
        _viewBounds = new Rectangle(0, 0, WorldConfig.WorldWidth, WorldConfig.WorldHeight);
    }

    public void ShowCloseVehicleView()
    {
        _viewBounds = WorldConfig.CloseViewBounds;
    }

    public Matrix GetViewMatrix()
    {
        float scaleX = (float)WorldConfig.ScreenWidth / _viewBounds.Width;
        float scaleY = (float)WorldConfig.ScreenHeight / _viewBounds.Height;

        return Matrix.CreateTranslation(-_viewBounds.X, -_viewBounds.Y, 0f)
            * Matrix.CreateScale(scaleX, scaleY, 1f);
    }
}
