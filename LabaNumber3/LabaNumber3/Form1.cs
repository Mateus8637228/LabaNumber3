using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using org.mariuszgromada.math.mxparser;

namespace LabaNumber3
{
  public partial class Form1 : Form
  {
    private TextBox txtA, txtB, txtE, txtFormula;
    private Label lblResult;
    private Chart chart1;
    private MenuStrip menuStrip1;
    private ToolStripMenuItem calcMenuItem, plotMenuItem, clearMenuItem;

    public Form1()
    {
      this.Text = "Лаба №4: Метод дихотомии";
      this.Size = new Size(1100, 700);
      this.StartPosition = FormStartPosition.CenterScreen;

      License.iConfirmNonCommercialUse("Student Lab");

      InitializeCustomComponents();
    }

    private void InitializeCustomComponents()
    {
      // === 1. Меню ===
      menuStrip1 = new MenuStrip();
      calcMenuItem = new ToolStripMenuItem("Рассчитать");
      plotMenuItem = new ToolStripMenuItem("Построить график");
      clearMenuItem = new ToolStripMenuItem("Очистить");

      calcMenuItem.Click += CalcMenuItem_Click;
      plotMenuItem.Click += PlotMenuItem_Click;
      clearMenuItem.Click += ClearMenuItem_Click;

      menuStrip1.Items.Add(calcMenuItem);
      menuStrip1.Items.Add(plotMenuItem);
      menuStrip1.Items.Add(clearMenuItem);
      this.Controls.Add(menuStrip1);
      this.MainMenuStrip = menuStrip1;

      // === 2. Панель ввода ===
      Panel leftPanel = new Panel();
      leftPanel.Dock = DockStyle.Left;
      leftPanel.Width = 320;
      leftPanel.Padding = new Padding(10);
      leftPanel.Top = menuStrip1.Bottom;
      leftPanel.BackColor = Color.WhiteSmoke;
      this.Controls.Add(leftPanel);

      // Создаем поля с валидацией
      CreateInputPair(leftPanel, "a (начало):", 30, out txtA);
      CreateInputPair(leftPanel, "b (конец):", 80, out txtB);
      CreateInputPair(leftPanel, "e (погрешность):", 130, out txtE);
      CreateInputPair(leftPanel, "f(x):", 180, out txtFormula);

      // Значения по умолчанию
      txtA.Text = "-5";
      txtB.Text = "5";
      txtE.Text = "0,001";
      txtFormula.Text = "x^2+2*x-6";

      // === 3. Результат ===
      lblResult = new Label();
      lblResult.Location = new Point(10, 240);
      lblResult.Size = new Size(290, 400);
      lblResult.Text = "Здесь будет результат...";
      lblResult.Font = new Font("Consolas", 10, FontStyle.Regular);
      lblResult.BackColor = Color.White;
      lblResult.BorderStyle = BorderStyle.FixedSingle;
      lblResult.Padding = new Padding(5);
      leftPanel.Controls.Add(lblResult);

      // === 4. График ===
      chart1 = new Chart();
      chart1.Dock = DockStyle.Fill;

      ChartArea chartArea = new ChartArea("MainArea");
      chartArea.AxisX.MajorGrid.LineColor = Color.LightGray;
      chartArea.AxisY.MajorGrid.LineColor = Color.LightGray;
      chart1.ChartAreas.Add(chartArea);

      Legend legend = new Legend("MainLegend");
      legend.Docking = Docking.Top;
      chart1.Legends.Add(legend);

      this.Controls.Add(chart1);
      chart1.BringToFront();

      SetupChartSeries();
    }

    private void CreateInputPair(Panel parent, string labelText, int yPos, out TextBox textBox)
    {
      Label lbl = new Label();
      lbl.Text = labelText;
      lbl.Location = new Point(10, yPos);
      lbl.AutoSize = true;
      parent.Controls.Add(lbl);

      textBox = new TextBox();
      textBox.Location = new Point(150, yPos - 3);
      textBox.Width = 150;

      // ПОДКЛЮЧАЕМ ВАЛИДАЦИЮ ВВОДА "ПО ПОНЯТИЯМ"
      textBox.KeyPress += TextBox_KeyPress;

      parent.Controls.Add(textBox);
    }

    // ================= СТРОГАЯ ВАЛИДАЦИЯ ВВОДА =================

    private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
    {
      TextBox tb = (TextBox)sender;

      // 1. Разрешаем управляющие клавиши (Backspace, Delete, Enter и т.д.)
      if (char.IsControl(e.KeyChar)) return;

      // 2. Запрещаем пробелы (глазом не моргнешь - не вставишь)
      if (e.KeyChar == ' ')
      {
        e.Handled = true;
        return;
      }

      // 3. Разрешаем цифры
      if (char.IsDigit(e.KeyChar))
      {
        // ПРОВЕРКА НА ВЕДУЩИЙ НОЛЬ
        // Если курсор в начале, и мы пытаемся ввести 0, и следующий символ (если есть) - цифра
        if (e.KeyChar == '0' && tb.SelectionStart == 0)
        {
          // Если в поле уже что-то есть, и это не запятая/точка
          if (tb.Text.Length > 0 && tb.Text[0] != ',' && tb.Text[0] != '.')
          {
            // Если следующий символ не запятая, блокируем ввод 0
            // Например: вводим "0", потом "1" -> получаем "01". Это плохо.
            if (tb.Text.Length > 1 && char.IsDigit(tb.Text[1]))
            {
              e.Handled = true;
              return;
            }
          }
        }
        return;
      }

      // 4. Разрешаем запятую (только одну)
      if (e.KeyChar == ',')
      {
        if (tb.Text.Contains(",")) e.Handled = true;
        return;
      }

      // 5. Разрешаем минус (только в начале и только один)
      if (e.KeyChar == '-')
      {
        if (tb.SelectionStart != 0 || tb.Text.Contains("-")) e.Handled = true;
        return;
      }

      // 6. Все остальное (буквы, символы) - блокируем
      e.Handled = true;
    }

    // ================= ЛОГИКА =================

    private void ClearMenuItem_Click(object sender, EventArgs e)
    {
      txtA.Text = "-5";
      txtB.Text = "5";
      txtE.Text = "0,001";
      txtFormula.Text = "x^2+2*x-6";
      lblResult.Text = "";
      foreach (var series in chart1.Series) series.Points.Clear();
    }

    private void PlotMenuItem_Click(object sender, EventArgs e)
    {
      if (ValidateInputs(out double a, out double b, out double eps, out string formula))
      {
        DrawGraph(formula, a, b);
      }
    }

    private void CalcMenuItem_Click(object sender, EventArgs e)
    {
      if (!ValidateInputs(out double a, out double b, out double eps, out string formula)) return;

      int decimals = GetDecimalPlaces(eps);
      string outputText = "";

      chart1.Series["Корень"].Points.Clear();
      chart1.Series["Минимум"].Points.Clear();

      DrawGraph(formula, a, b);

      double fa = EvaluateFunction(formula, a);
      double fb = EvaluateFunction(formula, b);

      if (double.IsNaN(fa) || double.IsInfinity(fa) || double.IsNaN(fb) || double.IsInfinity(fb))
      {
        MessageBox.Show("Функция не определена на концах интервала.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
      }

      // 1. Поиск корня (Дихотомия)
      if (fa * fb > 0)
      {
        outputText += "На концах интервала функция имеет одинаковые знаки.\nМетод дихотомии не гарантирует нахождение корня.\n\n";
      }

      double left = a, right = b;
      double x0 = 0, fx0 = 0;
      int iter = 0;

      while (Math.Abs(right - left) > eps && iter < 10000)
      {
        x0 = (left + right) / 2.0;
        fx0 = EvaluateFunction(formula, x0);

        if (double.IsNaN(fx0)) break;

        if (EvaluateFunction(formula, left) * fx0 < 0)
          right = x0;
        else
          left = x0;

        iter++;
      }

      if (Math.Abs(fx0) < 0.1)
      {
        outputText += $"[КОРЕНЬ]\nx = {Math.Round(x0, decimals)}\nf(x) = {Math.Round(fx0, decimals + 2)}\nИтераций: {iter}\n\n";
        chart1.Series["Корень"].Points.AddXY(x0, fx0);
      }
      else
      {
        outputText += "Корень не найден или функция имеет разрыв.\n\n";
      }

      // 2. Поиск минимума (Перебор)
      double minX = a;
      double minY = EvaluateFunction(formula, a);
      double scanStep = (b - a) / 200.0;

      for (double x = a; x <= b; x += scanStep)
      {
        double y = EvaluateFunction(formula, x);
        if (!double.IsNaN(y) && !double.IsInfinity(y) && y < minY)
        {
          minY = y;
          minX = x;
        }
      }

      outputText += $"[МИНИМУМ]\nx = {Math.Round(minX, decimals)}\nf(x) = {Math.Round(minY, decimals + 2)}";
      chart1.Series["Минимум"].Points.AddXY(minX, minY);

      lblResult.Text = outputText;
    }

    private double EvaluateFunction(string formula, double xValue)
    {
      Argument x = new Argument("x", xValue);
      Expression e = new Expression(formula, x);
      return e.calculate();
    }

    private bool ValidateInputs(out double a, out double b, out double eps, out string formula)
    {
      a = b = eps = 0;
      formula = txtFormula.Text;

      // ДОПОЛНИТЕЛЬНАЯ ПРОВЕРКА НА ПРОБЕЛЫ И ВЕДУЩИЕ НУЛИ (если вставили через Ctrl+V)
      if (txtA.Text.Contains(" ") || txtB.Text.Contains(" ") || txtE.Text.Contains(" "))
      {
        MessageBox.Show("Пробелы в числах недопустимы!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
      }

      // Проверка на "01", "02" и т.д.
      if (IsLeadingZero(txtA.Text) || IsLeadingZero(txtB.Text) || IsLeadingZero(txtE.Text))
      {
        MessageBox.Show("Числа не могут начинаться с нуля (кроме 0,xxx)!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
      }

      if (string.IsNullOrWhiteSpace(formula))
      {
        MessageBox.Show("Введите функцию f(x)!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false;
      }

      if (!double.TryParse(txtA.Text.Replace('.', ','), out a) ||
          !double.TryParse(txtB.Text.Replace('.', ','), out b) ||
          !double.TryParse(txtE.Text.Replace('.', ','), out eps))
      {
        MessageBox.Show("Некорректный ввод чисел.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); return false;
      }
      if (a >= b)
      {
        MessageBox.Show("'a' должно быть меньше 'b'.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); return false;
      }
      if (eps <= 0)
      {
        MessageBox.Show("Погрешность 'e' должна быть больше нуля.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); return false;
      }
      return true;
    }

    // Проверка на ведущий ноль (например "01", "001", но не "0" и не "0,5")
    private bool IsLeadingZero(string input)
    {
      if (string.IsNullOrEmpty(input)) return false;
      input = input.Trim();
      // Если первый символ '0' и длина больше 1
      if (input.Length > 1 && input[0] == '0')
      {
        // Если второй символ НЕ запятая и НЕ точка, то это ошибка (например "01")
        if (input[1] != ',' && input[1] != '.')
          return true;
      }
      return false;
    }

    private void DrawGraph(string formula, double a, double b)
    {
      chart1.Series["F(x)"].Points.Clear();
      double step = (b - a) / 500.0;

      for (double x = a; x <= b; x += step)
      {
        double y = EvaluateFunction(formula, x);
        if (!double.IsNaN(y) && !double.IsInfinity(y) && Math.Abs(y) < 1000)
        {
          chart1.Series["F(x)"].Points.AddXY(x, y);
        }
      }
      chart1.ChartAreas[0].RecalculateAxesScale();
    }

    private int GetDecimalPlaces(double eps)
    {
      int places = 0;
      while (eps < 1 && places < 10) { eps *= 10; places++; }
      return places;
    }

    private void SetupChartSeries()
    {
      chart1.Series.Clear();

      Series lineSeries = new Series("F(x)");
      lineSeries.ChartType = SeriesChartType.Spline;
      lineSeries.BorderWidth = 2;
      lineSeries.Color = Color.Blue;
      chart1.Series.Add(lineSeries);

      Series rootSeries = new Series("Корень");
      rootSeries.ChartType = SeriesChartType.Point;
      rootSeries.MarkerSize = 12;
      rootSeries.MarkerStyle = MarkerStyle.Circle;
      rootSeries.Color = Color.Red;
      chart1.Series.Add(rootSeries);

      Series minSeries = new Series("Минимум");
      minSeries.ChartType = SeriesChartType.Point;
      minSeries.MarkerSize = 15;
      minSeries.MarkerStyle = MarkerStyle.Star5;
      minSeries.Color = Color.Green;
      chart1.Series.Add(minSeries);
    }
  }
}