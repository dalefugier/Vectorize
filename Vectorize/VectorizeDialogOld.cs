using Eto.Drawing;
using Eto.Forms;
using Rhino;
using Rhino.Runtime;
using Rhino.UI.Controls;
using Rhino.UI.Forms;
using System;
using VectorizeCommon;

namespace Vectorize
{
  internal class VectorizeDialogOld : CommandDialog
  {
    private readonly RhinoDoc m_doc;
    private readonly VectorizeConduit m_conduit;
    private bool m_allow_update_and_redraw = true;

    /// <summary>
    /// Public constructor
    /// </summary>
    public VectorizeDialogOld(RhinoDoc doc, VectorizeConduit conduit)
    {
      m_doc = doc;
      m_conduit = conduit;

      Resizable = false;
      ShowHelpButton = true;
      HelpButtonClick += (sender, e) => ShowHelpUrl();

      Width = 350;
      Title = "Vectorize";
      Content = CreateTableLayout();
      Shown += (sender, e) => UpdateAndRedraw();
    }
    /// <summary>
    /// Creates the content of the dialog
    /// </summary>
    private RhinoDialogTableLayout CreateTableLayout()
    {
      // Create controls and define behaviors
      PotraceParameters parameters = m_conduit.Parameters;

      // Threshold slider (0.0 - 100.0)
      Eto.Forms.Slider sliderThreshold = new Eto.Forms.Slider
      {
        MaxValue = 100,
        MinValue = 0,
        TickFrequency = 25,
        ToolTip = PotraceStrings.ThresholdTooltip(false),
        Value = (int)parameters.ThresholdUi,
        Width = 220
      };

      // Threshold stepper (0.0 - 100.0)
      NumericUpDownWithUnitParsing stepperThreshold = new NumericUpDownWithUnitParsing
      {
        DecimalPlaces = 0,
        Increment = 1.0,
        MaxValue = 100.0,
        MinValue = 0.0,
        ToolTip = PotraceStrings.ThresholdTooltip(false),
        Value = parameters.ThresholdUi,
        ValueUpdateMode = NumericUpDownWithUnitParsingUpdateMode.WhenDoneChanging,
        Width = 45
      };

      sliderThreshold.ValueChanged += (sender, args) =>
      {
        if (m_allow_update_and_redraw)
        {
          m_allow_update_and_redraw = false;
          parameters.ThresholdUi = sliderThreshold.Value;
          stepperThreshold.Value = parameters.ThresholdUi;
          m_allow_update_and_redraw = true;
        }
      };

      sliderThreshold.MouseUp += (sender, args) =>
      {
        if (m_allow_update_and_redraw)
        {
          m_allow_update_and_redraw = false;
          parameters.ThresholdUi = sliderThreshold.Value;
          stepperThreshold.Value = parameters.ThresholdUi;
          m_allow_update_and_redraw = true;
          UpdateAndRedraw();
        }
      };

      stepperThreshold.ValueChanged += (sender, args) =>
      {
        if (m_allow_update_and_redraw)
        {
          m_allow_update_and_redraw = false;
          parameters.ThresholdUi = stepperThreshold.Value;
          sliderThreshold.Value = (int)parameters.ThresholdUi;
          m_allow_update_and_redraw = true;
          UpdateAndRedraw();
        }
      };

      // TurnPolicy (enum)
      DropDown dropdownTurnPolicy = new DropDown
      {
        ToolTip = PotraceStrings.TurnPolicyTooltip
      };

      foreach (string str in Enum.GetNames(typeof(PotraceTurnPolicy)))
        dropdownTurnPolicy.Items.Add(str);
      dropdownTurnPolicy.SelectedIndex = (int)parameters.TurnPolicy;

      dropdownTurnPolicy.SelectedIndexChanged += (sender, args) =>
      {
        if (dropdownTurnPolicy.SelectedIndex != 0)
        {
          parameters.TurnPolicy = (PotraceTurnPolicy)dropdownTurnPolicy.SelectedIndex;
          UpdateAndRedraw();
        }
      };

      // TurdSize stepper (0 - 100)
      NumericUpDownWithUnitParsing stepperTurdSize = new NumericUpDownWithUnitParsing
      {
        DecimalPlaces = 0,
        Increment = 1.0,
        MaxValue = 100.0,
        MinValue = 0.0,
        ToolTip = PotraceStrings.TurdSizeTooltip(false),
        Value = parameters.TurdSize,
        ValueUpdateMode = NumericUpDownWithUnitParsingUpdateMode.WhenDoneChanging
      };
      stepperTurdSize.ValueChanged += (sender, args) =>
      {
        parameters.TurdSize = (int)stepperTurdSize.Value;
        UpdateAndRedraw();
      };

      NumericUpDownWithUnitParsing stepperAlphaMax = new NumericUpDownWithUnitParsing
      {
        DecimalPlaces = 1,
        Increment = 0.1,
        MaxValue = 4 / 3,
        MinValue = 0.0,
        ToolTip = PotraceStrings.AlphaMaxTooltip(false),
        Value = parameters.AlphaMax,
        ValueUpdateMode = NumericUpDownWithUnitParsingUpdateMode.WhenDoneChanging
      };
      stepperAlphaMax.ValueChanged += (sender, args) =>
      {
        parameters.AlphaMax = stepperAlphaMax.Value;
        UpdateAndRedraw();
      };

      CheckBox checkBorder = new CheckBox
      {
        Checked = parameters.IncludeBorder,
        ThreeState = false,
        ToolTip = PotraceStrings.IncludeBorderTooltip,
      };
      checkBorder.CheckedChanged += (sender, args) =>
      {
        parameters.IncludeBorder = checkBorder.Checked.Value;
        UpdateAndRedraw();
      };

      CheckBox checkOptimize = new CheckBox
      {
        Checked = parameters.OptimizeCurve,
        ThreeState = false,
        ToolTip = PotraceStrings.OptimizeCurveTooltip
      };

      NumericUpDownWithUnitParsing stepperOptimizeTolerance = new NumericUpDownWithUnitParsing
      {
        DecimalPlaces = m_doc.ModelDistanceDisplayPrecision,
        Enabled = parameters.OptimizeCurve,
        Increment = 0.1,
        MaxValue = 1.0,
        MinValue = 0.0,
        ToolTip = PotraceStrings.OptimizeToleranceTooltip(false),
        Value = parameters.OptimizeTolerance,
        ValueUpdateMode = NumericUpDownWithUnitParsingUpdateMode.WhenDoneChanging,
      };

      checkOptimize.CheckedChanged += (sender, args) =>
      {
        parameters.OptimizeCurve = checkOptimize.Checked.Value;
        stepperOptimizeTolerance.Enabled = parameters.OptimizeCurve;
        UpdateAndRedraw();
      };

      stepperOptimizeTolerance.ValueChanged += (sender, args) =>
      {
        parameters.OptimizeTolerance = stepperOptimizeTolerance.Value;
        UpdateAndRedraw();
      };

      Button buttonDefaults = new Button { Text = "Restore Defaults" };
      buttonDefaults.Click += (sender, args) =>
      {
        if (m_allow_update_and_redraw)
        {
          m_allow_update_and_redraw = false;
          parameters.SetDefaults();
          sliderThreshold.Value = (int)parameters.ThresholdUi;
          stepperThreshold.Value = parameters.ThresholdUi;
          dropdownTurnPolicy.SelectedIndex = (int)parameters.TurnPolicy;
          stepperTurdSize.Value = parameters.TurdSize;
          stepperAlphaMax.Value = parameters.AlphaMax;
          checkBorder.Checked = parameters.IncludeBorder;
          checkOptimize.Checked = parameters.OptimizeCurve;
          stepperOptimizeTolerance.Value = parameters.OptimizeTolerance;
          m_allow_update_and_redraw = true;
          UpdateAndRedraw();
        }
      };

      // Layout the controls

      Size minimum_size = new Eto.Drawing.Size(150, 0);

      RhinoDialogTableLayout layout = new RhinoDialogTableLayout(false) { Spacing = new Eto.Drawing.Size(10, 8) };
      layout.Rows.Add(new TableRow(new TableCell(new LabelSeparator { Text = "Vectorization options" }, true)));

      Panel panel0 = new Panel { MinimumSize = minimum_size, Content = new Label() { Text = PotraceStrings.ThresholdLabel(false) } };
      TableLayout table0 = new TableLayout { Padding = new Eto.Drawing.Padding(8, 0, 0, 0) };
      table0.Rows.Add(new TableRow(new TableCell(panel0), new TableCell(stepperThreshold), new TableCell(sliderThreshold, true)));
      layout.Rows.Add(table0);

      Panel panel1 = new Panel { MinimumSize = minimum_size, Content = new Label() { Text = PotraceStrings.TurnPolicyLabel } };
      Panel panel2 = new Panel { MinimumSize = minimum_size, Content = new Label() { Text = PotraceStrings.IncludeBorderLabel(true) } };

      TableLayout table1 = new TableLayout { Padding = new Eto.Drawing.Padding(8, 0, 0, 0), Spacing = new Size(10, 8) };
      table1.Rows.Add(new TableRow(new TableCell(panel1), new TableCell(dropdownTurnPolicy)));
      table1.Rows.Add(new TableRow(new TableCell(new Label() { Text = PotraceStrings.TurdSizeLabel(false) }), new TableCell(stepperTurdSize)));
      table1.Rows.Add(new TableRow(new TableCell(new Label() { Text = PotraceStrings.AlphaMaxLabel(true) }), new TableCell(stepperAlphaMax)));
      table1.Rows.Add(new TableRow(new TableCell(panel2), new TableCell(checkBorder)));

      layout.Rows.Add(table1);

      layout.Rows.Add(new TableRow(new TableCell(new LabelSeparator { Text = "Curve optimization" }, true)));

      Panel panel3 = new Panel { MinimumSize = minimum_size, Content = new Label() { Text = PotraceStrings.OptimizeCurveLabel } };
      TableLayout table2 = new TableLayout { Padding = new Eto.Drawing.Padding(8, 0, 0, 0), Spacing = new Size(10, 8) };
      table2.Rows.Add(new TableRow(new TableCell(panel3), new TableCell(checkOptimize)));
      table2.Rows.Add(new TableRow(new TableCell(new Label() { Text = PotraceStrings.OptimizeToleranceLabel(false) }), new TableCell(stepperOptimizeTolerance)));
      table2.Rows.Add(null);
      table2.Rows.Add(new TableRow(new TableCell(new Label() { Text = "" }), new TableCell(buttonDefaults)));
      layout.Rows.Add(table2);

      return layout;
    }

    private void UpdateAndRedraw()
    {
      if (m_allow_update_and_redraw && null != m_doc && null != m_conduit)
      {
        m_allow_update_and_redraw = false;

        RhinoApp.SetCommandPrompt("Tracing image, please wait");
        m_conduit.TraceBitmap();
        m_doc.Views.Redraw();
        string msg = HostUtils.RunningOnOSX ? "Apply" : "OK";
        RhinoApp.SetCommandPrompt($"Vectorize options. Press {msg} when done");

        m_allow_update_and_redraw = true;
      }
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