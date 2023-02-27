using Eto.Forms;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.UI;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using VectorizeCommon;

namespace Vectorize
{
  /// <summary>
  /// Vectorize display conduit.
  /// </summary>
  public class VectorizeConduit : DisplayConduit
  {
    private readonly Eto.Drawing.Bitmap m_etoBitmap;
    private readonly PotraceParameters m_parameters;
    private readonly double m_scale;
    private readonly Color m_color;
    private readonly List<Curve> m_curves = new List<Curve>();
    private PotraceBitmap m_potraceBitmap;
    private BoundingBox m_bbox = BoundingBox.Unset;

    /// <summary>
    /// Public constructor.
    /// </summary>
    public VectorizeConduit(Eto.Drawing.Bitmap bitmap, PotraceParameters parameters, double scale, Color color)
    {
      m_etoBitmap = bitmap;
      m_parameters = parameters;
      m_scale = scale;
      m_color = color;
    }

    /// <summary>
    /// Potrace parameters.
    /// </summary>
    public PotraceParameters Parameters => m_parameters;

    /// <summary>
    /// The list of outline curves created from the path curves.
    /// These curve may end up in the Rhino document.
    /// </summary>
    public List<Curve> Curves
    {
      get => m_curves;
    }

    /// <summary>
    /// DisplayConduit.CalculateBoundingBox override.
    /// </summary>
    protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
    {
      if (m_bbox.IsValid)
        e.IncludeBoundingBox(m_bbox);
    }

    /// <summary>
    /// DisplayConduit.CalculateBoundingBoxZoomExtents override.
    /// </summary>
    protected override void CalculateBoundingBoxZoomExtents(CalculateBoundingBoxEventArgs e)
    {
      CalculateBoundingBox(e);
    }

    /// <summary>
    /// DisplayConduit.DrawOverlay override.
    /// </summary>
    protected override void DrawOverlay(DrawEventArgs e)
    {
      for (var i = 0; i < m_curves.Count; i++)
      {
        if (i == 0 && !m_parameters.IncludeBorder)
          continue;
        e.Display.DrawCurve(m_curves[i], m_color);
      }
    }

    /// <summary>
    /// Trace the bitmap using Potrace.
    /// </summary>
    public void TraceBitmap()
    {
      // Clear curve list and reset conduit bounding box
      m_curves.Clear();
      m_bbox = BoundingBox.Unset;

      // Create Potrace bitmap if needed
      if (null == m_potraceBitmap)
      {
        m_potraceBitmap = new PotraceBitmap(m_etoBitmap, m_parameters.Threshold);
      }
      else if (m_potraceBitmap.Treshold != m_parameters.Threshold)
      {
        m_potraceBitmap.Dispose();
        m_potraceBitmap = new PotraceBitmap(m_etoBitmap, m_parameters.Threshold);
      }

      // Trace the bitmap
      var potrace = Potrace.Trace(m_potraceBitmap, m_parameters);
      if (null == potrace)
        return;

      // The first curve is always the border curve no matter what
      var corners = new Point3d[] {
        Point3d.Origin,
        new Point3d(m_etoBitmap.Width, 0.0, 0.0),
        new Point3d(m_etoBitmap.Width, m_etoBitmap.Height, 0.0),
        new Point3d(0.0, m_etoBitmap.Height, 0.0),
        Point3d.Origin
        };

      var border = new PolylineCurve(corners);
      m_curves.Add(border);

      // Harvest the Potrace path curves
      var potracePath = potrace.Path;
      while (null != potracePath)
      {
        var curve = potracePath.Curve;
        if (null != curve)
          m_curves.Add(curve);
        potracePath = potracePath.Next;
      }

      if (m_curves.Count > 0)
      {
        // Scale the output, per the calculation made in the command.
        if (m_scale != 1.0)
        {
          var xform = Transform.Scale(Point3d.Origin, m_scale);
          for (var i = 0; i < m_curves.Count; i++)
            m_curves[i].Transform(xform);
        }
        m_bbox = m_curves[0].GetBoundingBox(true);
      }
    }
  }
}
