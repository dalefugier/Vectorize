using Eto.Drawing;
using Eto.Forms;
using Rhino;
using Rhino.Runtime;
using Rhino.UI.Controls;
using Rhino.UI.Forms;
using System.ComponentModel;
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
      RhinoDialogTableLayout layout = new RhinoDialogTableLayout(false);

      // Threshold slider (0.0, 100.0)
      Rhino.UI.Controls.Slider thresholdSlider = new Rhino.UI.Controls.Slider(layout, true)
      {
        Decimals = 2,
        DrawTextLabels = true,
        ToolTip = PotraceStrings.ThresholdTooltip(false),
        Value1 = m_conduit.Parameters.ThresholdUi,
        Width = 200
      };
      thresholdSlider.SetMinMax(0.0, 100.0);
      thresholdSlider.PropertyChanged += OnThresholdPropertyChanged;

      // TurdSize slider (0 to 100)
      Rhino.UI.Controls.Slider turdSizeSlider = new Rhino.UI.Controls.Slider(this, false)
      {
        Decimals = 0,
        DrawTextLabels = true,
        ToolTip = PotraceStrings.TurdSizeTooltip(false),
        Value1 = m_conduit.Parameters.TurdSize
      };
      turdSizeSlider.SetMinMax(0.0, 100.0);
      turdSizeSlider.PropertyChanged += OnTurdSizePropertyChanged;

      // AlphaMax slider (0.0, 1.34)
      Rhino.UI.Controls.Slider alphaMaxSlider = new Rhino.UI.Controls.Slider(this, false)
      {
        Decimals = 2,
        DrawTextLabels = true,
        ToolTip = PotraceStrings.AlphaMaxTooltip(false),
        Value1 = m_conduit.Parameters.AlphaMax
      };
      alphaMaxSlider.SetMinMax(0.0, 1.34);
      alphaMaxSlider.PropertyChanged += OnAlphaMaxPropertyChanged;

      // OptimizeTolerance slider (0.0, 1.0)
      Rhino.UI.Controls.Slider optimizeToleranceSlider = new Rhino.UI.Controls.Slider(this, false)
      {
        Decimals = 2,
        DrawTextLabels = true,
        ToolTip = PotraceStrings.OptimizeToleranceTooltip(false),
        Value1 = m_conduit.Parameters.OptimizeTolerance
      };
      optimizeToleranceSlider.SetMinMax(0.0, 1.0);
      optimizeToleranceSlider.PropertyChanged += OnOptimizeTolerancePropertyChanged;

      // IncludeBorder checkbox
      CheckBox includeBorderCheckBox = new CheckBox
      {
        Checked = m_conduit.Parameters.IncludeBorder,
        ThreeState = false,
        ToolTip = PotraceStrings.IncludeBorderTooltip
      };
      includeBorderCheckBox.CheckedChanged += (sender, args) =>
      {
        bool value = includeBorderCheckBox.Checked.Value;
        if (m_conduit.Parameters.IncludeBorder != value)
        {
          m_conduit.Parameters.IncludeBorder = value;
          m_doc.Views.Redraw();
        }
      };

      // RestoreDefaults button
      Button restoreDefaultsButton = new Button { Text = "Restore Defaults" };
      restoreDefaultsButton.Click += (sender, args) =>
      {
        m_conduit.Parameters.SetDefaults();
        thresholdSlider.Value1 = m_conduit.Parameters.ThresholdUi;
        turdSizeSlider.Value1 = m_conduit.Parameters.TurdSize;
        alphaMaxSlider.Value1 = m_conduit.Parameters.AlphaMax;
        optimizeToleranceSlider.Value1 = m_conduit.Parameters.OptimizeTolerance;
        includeBorderCheckBox.Checked = m_conduit.Parameters.IncludeBorder;
        UpdateAndRedraw();
      };

      // Layout the controls
      TableLayout table = new TableLayout { Padding = new Eto.Drawing.Padding(8, 0, 0, 0), Spacing = new Size(10, 8) };
      table.Rows.Add(new TableRow(new TableCell(new Label { Text = PotraceStrings.ThresholdLabel(false) }), new TableCell(thresholdSlider)));
      table.Rows.Add(new TableRow(new TableCell(new Label { Text = PotraceStrings.TurdSizeLabel(true) }), new TableCell(turdSizeSlider)));
      table.Rows.Add(new TableRow(new TableCell(new Label { Text = PotraceStrings.AlphaMaxLabel(true) }), new TableCell(alphaMaxSlider)));
      table.Rows.Add(new TableRow(new TableCell(new Label { Text = PotraceStrings.OptimizeToleranceLabel(false) }), new TableCell(optimizeToleranceSlider)));
      table.Rows.Add(new TableRow(new TableCell(new Label { Text = PotraceStrings.IncludeBorderLabel(true) }), new TableCell(includeBorderCheckBox)));
      table.Rows.Add(new TableRow(new TableCell(new Label { Text = "" }), new TableCell(restoreDefaultsButton)));
      table.Rows.Add(null);

      layout.Rows.Add(table);
      layout.Rows.Add(null);

      return layout;
    }

    /// <summary>
    /// Threshold slider handler
    /// </summary>
    private void OnThresholdPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
      if (sender is Rhino.UI.Controls.Slider slider && args.PropertyName.Equals("OnEndChanged"))
      {
        double value = (double)slider.Value1;
        if (m_conduit.Parameters.ThresholdUi != value)
        {
          m_conduit.Parameters.ThresholdUi = value;
          UpdateAndRedraw();
        }
      }
    }

    /// <summary>
    /// TurdSize slider handler
    /// </summary>
    private void OnTurdSizePropertyChanged(object sender, PropertyChangedEventArgs args)
    {
      if (sender is Rhino.UI.Controls.Slider slider && args.PropertyName.Equals("OnEndChanged"))
      {
        int value = (int)slider.Value1;
        if (m_conduit.Parameters.TurdSize != value)
        {
          m_conduit.Parameters.TurdSize = value;
          UpdateAndRedraw();
        }
      }
    }

    /// <summary>
    /// AlphaMax slider handler
    /// </summary>
    private void OnAlphaMaxPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
      if (sender is Rhino.UI.Controls.Slider slider && args.PropertyName.Equals("OnEndChanged"))
      {
        double value = (double)slider.Value1;
        if (m_conduit.Parameters.AlphaMax != value)
        {
          m_conduit.Parameters.AlphaMax = value;
          UpdateAndRedraw();
        }
      }
    }

    /// <summary>
    /// OptimizeTolerance slider handler
    /// </summary>
    private void OnOptimizeTolerancePropertyChanged(object sender, PropertyChangedEventArgs args)
    {
      if (sender is Rhino.UI.Controls.Slider slider && args.PropertyName.Equals("OnEndChanged"))
      {
        double value = (double)slider.Value1;
        if (m_conduit.Parameters.OptimizeTolerance != value)
        {
          m_conduit.Parameters.OptimizeTolerance = value;
          UpdateAndRedraw();
        }
      }
    }

    /// <summary>
    /// Trace the bitmap and redraw the results.
    /// </summary>
    private void UpdateAndRedraw()
    {
      if (null == m_doc || null == m_conduit)
        return;

      RhinoApp.SetCommandPrompt("Tracing image, please wait");
      m_conduit.TraceBitmap();
      m_doc.Views.Redraw();
      string msg = HostUtils.RunningOnOSX ? "Apply" : "OK";
      RhinoApp.SetCommandPrompt($"Vectorize options. Press {msg} when done");
    }

    /// <summary>
    /// Show the help url.
    /// </summary>
    private void ShowHelpUrl()
    {
      VectorizeCommand.ShowHelpUrl();
    }
  }
}
