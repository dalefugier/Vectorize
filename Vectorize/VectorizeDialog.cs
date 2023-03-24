using Eto.Drawing;
using Eto.Forms;
using Rhino;
using Rhino.UI.Controls;
using Rhino.UI.Forms;
using System.Diagnostics;
using VectorizeCommon;

namespace Vectorize
{
  /// <summary>
  /// Vectorize command dialog
  /// </summary>
  public class VectorizeDialog : CommandDialog
  {
    private readonly RhinoDoc m_doc;
    private readonly VectorizeConduit m_conduit;
    private bool m_allow_update_and_redraw = true;

    /// <summary>
    /// Public constructor
    /// </summary>
    public VectorizeDialog(RhinoDoc doc, VectorizeConduit conduit)
    {
      m_doc = doc;
      m_conduit = conduit;

      Resizable = false;
      ShowHelpButton = true;
      HelpButtonClick += (sender, e) => ShowHelpUrl();

      Title = VectorizeCommand.Instance.EnglishName;
      Content = CreateTableLayout();
      Shown += (sender, e) => UpdateAndRedraw();
    }

    /// <summary>
    /// Creates the content of the dialog
    /// </summary>
    private RhinoDialogTableLayout CreateTableLayout()
    {
      // Create controls and define behaviors
      var parameters = m_conduit.Parameters;

      // Threshold (0.0, 100.0)
      var sldThreshold = new Rhino.UI.Controls.Slider(this, true)
      {
        Decimals = 2,
        DrawTextLabels = true,
        ToolTip = PotraceStrings.ThresholdTooltip(false),
        Width = 200
      };
      sldThreshold.SetMinMax(0.0, 100.0);
      sldThreshold.Value1 = parameters.Threshold * 100.0;
      sldThreshold.PropertyChanged += (sender, args) =>
      {
        if (m_allow_update_and_redraw)
        {
          m_allow_update_and_redraw = false;
          parameters.Threshold = sldThreshold.Value1.Value / 100.0;
          sldThreshold.Value1 = parameters.Threshold * 100.0;
          m_allow_update_and_redraw = true;
          UpdateAndRedraw();
        }
      };

      // TurdSize (0 to 100)
      var sldTurdSize = new Rhino.UI.Controls.Slider(this, false)
      {
        Decimals = 0,
        DrawTextLabels = true,
        ToolTip = PotraceStrings.TurdSizeTooltip(false)
      };
      sldTurdSize.SetMinMax(0.0, 100.0);
      sldTurdSize.Value1 = parameters.TurdSize;
      sldTurdSize.PropertyChanged += (sender, args) =>
      {
        if (m_allow_update_and_redraw)
        {
          m_allow_update_and_redraw = false;
          parameters.TurdSize = (int)sldTurdSize.Value1.Value;
          sldTurdSize.Value1 = parameters.TurdSize;
          m_allow_update_and_redraw = true;
          UpdateAndRedraw();
        }
      };

      // AlphaMax (0.0, 1.34)
      var sldAlphaMax = new Rhino.UI.Controls.Slider(this, false)
      {
        Decimals = 2,
        DrawTextLabels = true,
        ToolTip = PotraceStrings.AlphaMaxTooltip(false)
      };
      sldAlphaMax.SetMinMax(0.0, 1.34);
      sldAlphaMax.Value1 = parameters.AlphaMax;
      sldAlphaMax.PropertyChanged += (sender, args) =>
      {
        if (m_allow_update_and_redraw)
        {
          m_allow_update_and_redraw = false;
          parameters.AlphaMax = sldAlphaMax.Value1.Value;
          sldAlphaMax.Value1 = parameters.AlphaMax;
          m_allow_update_and_redraw = true;
          UpdateAndRedraw();
        }
      };

      // OptimizeTolerance (0.0, 1.0)
      var sldOptimizeTolerance = new Rhino.UI.Controls.Slider(this, false)
      {
        Decimals = 2,
        DrawTextLabels = true,
        ToolTip = PotraceStrings.OptimizeToleranceTooltip(false)
      };
      sldOptimizeTolerance.SetMinMax(0.0, 1.0);
      sldOptimizeTolerance.Value1 = parameters.OptimizeTolerance;
      sldOptimizeTolerance.PropertyChanged += (sender, args) =>
      {
        if (m_allow_update_and_redraw)
        {
          m_allow_update_and_redraw = false;
          parameters.OptimizeTolerance = sldOptimizeTolerance.Value1.Value;
          sldOptimizeTolerance.Value1 = parameters.OptimizeTolerance;
          m_allow_update_and_redraw = true;
          UpdateAndRedraw();
        }
      };

      // IncludeBorder
      var chkIncludeBorder = new CheckBox
      {
        Checked = parameters.IncludeBorder,
        ThreeState = false,
        ToolTip = PotraceStrings.IncludeBorderTooltip
      };
      chkIncludeBorder.CheckedChanged += (sender, args) =>
      {
        if (m_allow_update_and_redraw)
        {
          m_allow_update_and_redraw = false;
          parameters.IncludeBorder = chkIncludeBorder.Checked.Value;
          m_allow_update_and_redraw = true;
          UpdateAndRedraw();
        }
      };

      // RestoreDefaults
      var btnRestoreDefaults = new Button { Text = "Restore Defaults" };
      btnRestoreDefaults.Click += (sender, args) =>
      {
        if (m_allow_update_and_redraw)
        {
          m_allow_update_and_redraw = false;
          parameters.SetDefaults();
          sldThreshold.Value1 = parameters.Threshold * 100.0;
          sldTurdSize.Value1 = parameters.TurdSize;
          sldAlphaMax.Value1 = parameters.AlphaMax;
          sldOptimizeTolerance.Value1 = parameters.OptimizeTolerance;
          chkIncludeBorder.Checked = parameters.IncludeBorder;
          m_allow_update_and_redraw = true;
          UpdateAndRedraw();
        }
      };

      // Layout the controls

      var layout = new RhinoDialogTableLayout(false); // { Spacing = new Eto.Drawing.Size(10, 8) };
      //layout.Rows.Add(new TableRow(new TableCell(new LabelSeparator { Text = "Vectorization options" }, true)));

      var table = new TableLayout { Padding = new Eto.Drawing.Padding(8, 0, 0, 0), Spacing = new Size(10, 8) };
      table.Rows.Add(new TableRow(new TableCell(new Label { Text = PotraceStrings.ThresholdLabel(false) }), new TableCell(sldThreshold)));
      table.Rows.Add(new TableRow(new TableCell(new Label { Text = PotraceStrings.TurdSizeLabel(true) }), new TableCell(sldTurdSize)));
      table.Rows.Add(new TableRow(new TableCell(new Label { Text = PotraceStrings.AlphaMaxLabel(true) }), new TableCell(sldAlphaMax)));
      table.Rows.Add(new TableRow(new TableCell(new Label { Text = PotraceStrings.OptimizeToleranceLabel(false) }), new TableCell(sldOptimizeTolerance)));
      table.Rows.Add(new TableRow(new TableCell(new Label { Text = PotraceStrings.IncludeBorderLabel(true) }), new TableCell(chkIncludeBorder)));
      table.Rows.Add(new TableRow(new TableCell(new Label { Text = "" }), new TableCell(btnRestoreDefaults)));
      table.Rows.Add(null);

      layout.Rows.Add(table);
      layout.Rows.Add(null);

      return layout;
    }

    private void UpdateAndRedraw()
    {
      if (m_allow_update_and_redraw && null != m_doc && null != m_conduit)
      {
        m_conduit.TraceBitmap();
        m_doc.Views.Redraw();
      }
    }

    private void ShowHelpUrl()
    {
      Process.Start(VectorizeCommand.HelpUrl);
    }
  }
}
