using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Maui.Controls;
using NCalc;

namespace Calculator
{
    public partial class ScientificCalculatorPage : ContentPage
    {
        // Internal expression used for evaluation (tokens: pi, e, pow, etc.)
        private string _expression = "";

        // User-facing expression shown in the DisplayLabel (symbols like "π", "×", "÷")
        private string _displayExpression = "";

        private double _memory = 0;
        private bool _isDegMode = true;

        public ScientificCalculatorPage()
        {
            InitializeComponent();
        }

        // ---------------------------
        // Memory handlers (MC, MR, MS, M+, M-)
        // ---------------------------
        private void OnMemoryClearClicked(object sender, EventArgs e)
        {
            _memory = 0;
        }

        private void OnMemoryRecallClicked(object sender, EventArgs e)
        {
            DisplayLabel.Text = FormatResult(_memory);
            _expression = _memory.ToString(CultureInfo.InvariantCulture);
            _displayExpression = _expression;
        }

        private void OnMemoryStoreClicked(object sender, EventArgs e)
        {
            if (TryGetNumericDisplayValue(out double val))
                _memory = val;
        }

        private void OnMemoryAddClicked(object sender, EventArgs e)
        {
            if (TryGetNumericDisplayValue(out double val))
                _memory += val;
        }

        private void OnMemorySubtractClicked(object sender, EventArgs e)
        {
            if (TryGetNumericDisplayValue(out double val))
                _memory -= val;
        }

        // ---------------------------
        // Button clicks (digits/operators/etc.)
        // ---------------------------
        private void OnButtonClicked(object sender, EventArgs e)
        {
            if (!(sender is Button btn)) return;
            string text = btn.Text ?? "";

            // Normalize some visual characters to tokens for expression.
            switch (text)
            {
                case "×": text = "*"; break;
                case "÷": text = "/"; break;
                case "−": text = "-"; break;
                case ",": text = "."; break;
            }

            // Handle absolute-value button (visual text "|x|")
            if (btn.Text == "|x|")
            {
                // implicit multiplication if needed
                if (!string.IsNullOrEmpty(_expression))
                {
                    char last = _expression.Last();
                    if (char.IsDigit(last) || last == ')' || last == '.')
                    {
                        _expression += "*";
                        _displayExpression += "×";
                    }
                }

                _expression += "abs(";
                // show as vertical bars in display if desired: here we show as |(
                // but we can keep "abs(" for clarity. We'll show as "|(" then close later on =.
                _displayExpression += "|";
                UpdateDisplay();
                return;
            }

            // Constants as symbols in display but tokens in expression
            if (btn.Text == "π")
            {
                if (!string.IsNullOrEmpty(_expression))
                {
                    char last = _expression.Last();
                    if (char.IsDigit(last) || last == ')' || last == '.')
                    {
                        _expression += "*";
                        _displayExpression += "×";
                    }
                }
                _expression += "pi";
                _displayExpression += "π";
                UpdateDisplay();
                return;
            }

            if (btn.Text == "e")
            {
                if (!string.IsNullOrEmpty(_expression))
                {
                    char last = _expression.Last();
                    if (char.IsDigit(last) || last == ')' || last == '.')
                    {
                        _expression += "*";
                        _displayExpression += "×";
                    }
                }
                _expression += "e";
                _displayExpression += "e";
                UpdateDisplay();
                return;
            }

            // 10^x and x^y handling (insert caret '^' in expression; converted before evaluation)
            if (btn.Text == "10^x")
            {
                if (string.IsNullOrEmpty(_expression) || IsBinaryOperator(_expression.Last().ToString()))
                {
                    _expression += "10^";
                    _displayExpression += "10^";
                }
                else
                {
                    _expression += "^";
                    _displayExpression += "^";
                }
                UpdateDisplay();
                return;
            }

            if (btn.Text == "x^y")
            {
                _expression += "^";
                _displayExpression += "^";
                UpdateDisplay();
                return;
            }

            // If user presses a binary operator first, assume "0" as left operand
            if (IsBinaryOperator(text) && string.IsNullOrEmpty(_expression))
            {
                _expression = "0";
                _displayExpression = "0";
            }

            // Special immediate actions
            if (text == "x²")
            {
                HandlePowerOfTwo();
                return;
            }

            if (text == "±")
            {
                ToggleSignOfLastNumber();
                return;
            }

            if (text == "²√x")
            {
                HandleRootSquareOfLastNumber();
                return;
            }

            if (text == "1/x")
            {
                HandleReciprocalOfLastNumber();
                return;
            }

            if (text == "n!")
            {
                HandleFactorialOfCurrentValue();
                return;
            }

            // Normal append: append to both internal and display representations
            _expression += text;
            _displayExpression += ReplaceTokenForDisplay(text);
            UpdateDisplay();
        }

        // ---------------------------
        // Helpers for modifying last number in expression
        // ---------------------------
        private void ToggleSignOfLastNumber()
        {
            if (string.IsNullOrEmpty(_expression))
            {
                _expression = "-";
                _displayExpression = "-";
                UpdateDisplay();
                return;
            }

            int i = _expression.Length - 1;
            while (i >= 0 && (char.IsDigit(_expression[i]) || _expression[i] == '.')) i--;
            int start = i + 1;
            string last = _expression.Substring(start);

            if (string.IsNullOrEmpty(last)) return;

            if (start > 0 && _expression[start - 1] == '-'
                && (start - 1 == 0 || IsBinaryOperator(_expression[start - 2].ToString())))
            {
                _expression = _expression.Remove(start - 1, 1);
                _displayExpression = ReplaceExpressionForDisplay(_expression);
            }
            else
            {
                _expression = _expression.Insert(start, "-");
                _displayExpression = ReplaceExpressionForDisplay(_expression);
            }

            UpdateDisplay();
        }

        private void HandlePowerOfTwo()
        {
            string last = GetLastTokenForUnaryOp();
            if (string.IsNullOrEmpty(last)) return;

            int idx = _expression.LastIndexOf(last, StringComparison.Ordinal);
            _expression = _expression.Remove(idx, last.Length);
            _expression += "(" + last + ")^2";

            _displayExpression = ReplaceExpressionForDisplay(_expression);
            UpdateDisplay();
        }

        private void HandleRootSquareOfLastNumber()
        {
            string last = GetLastTokenForUnaryOp();
            if (string.IsNullOrEmpty(last)) return;
            int idx = _expression.LastIndexOf(last, StringComparison.Ordinal);
            _expression = _expression.Remove(idx, last.Length);
            _expression += "sqrt(" + last + ")";
            _displayExpression = ReplaceExpressionForDisplay(_expression);
            UpdateDisplay();
        }

        private void HandleReciprocalOfLastNumber()
        {
            string last = GetLastTokenForUnaryOp();
            if (string.IsNullOrEmpty(last)) return;

            if (double.TryParse(last, NumberStyles.Any, CultureInfo.InvariantCulture, out double num) && Math.Abs(num) < double.Epsilon)
            {
                DisplayLabel.Text = "Error";
                _expression = "";
                _displayExpression = "";
                return;
            }

            int idx = _expression.LastIndexOf(last, StringComparison.Ordinal);
            _expression = _expression.Remove(idx, last.Length);
            _expression += "(1/(" + last + "))";
            _displayExpression = ReplaceExpressionForDisplay(_expression);
            UpdateDisplay();
        }

        private void HandleFactorialOfCurrentValue()
        {
            if (TryGetNumericDisplayValue(out double val))
            {
                if (val < 0 || val != Math.Truncate(val))
                {
                    DisplayLabel.Text = "Error";
                    _expression = "";
                    _displayExpression = "";
                    return;
                }

                double fact = Factorial((int)val);
                DisplayLabel.Text = FormatResult(fact);
                _expression = fact.ToString(CultureInfo.InvariantCulture);
                _displayExpression = _expression;
            }
            else
            {
                DisplayLabel.Text = "Error";
                _expression = "";
                _displayExpression = "";
            }
        }

        private string GetLastTokenForUnaryOp()
        {
            if (string.IsNullOrEmpty(_expression)) return null;

            if (_expression.EndsWith(")"))
            {
                int depth = 0;
                for (int i = _expression.Length - 1; i >= 0; i--)
                {
                    if (_expression[i] == ')') depth++;
                    else if (_expression[i] == '(') depth--;
                    if (depth == 0)
                        return _expression.Substring(i);
                }
                return _expression;
            }

            int j = _expression.Length - 1;
            while (j >= 0 && (char.IsDigit(_expression[j]) || _expression[j] == '.')) j--;
            return _expression.Substring(j + 1);
        }

        private static double Factorial(int n)
        {
            double res = 1;
            for (int i = 2; i <= n; i++) res *= i;
            return res;
        }

        private static bool IsBinaryOperator(string s)
        {
            return s == "+" || s == "-" || s == "*" || s == "/" || s == "^" || s == "%";
        }

        // ---------------------------
        // Clear / Backspace / Equals
        // ---------------------------
        private void OnClearClicked(object sender, EventArgs e)
        {
            _expression = "";
            _displayExpression = "";
            DisplayLabel.Text = "0";
        }

        private void OnBackspaceClicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_expression))
            {
                _expression = _expression.Remove(_expression.Length - 1);
                _displayExpression = ReplaceExpressionForDisplay(_expression);
                UpdateDisplay();
            }
            else
            {
                DisplayLabel.Text = "0";
            }
        }

        private void OnEqualsClicked(object sender, EventArgs e)
        {
            try
            {
                string expr = _expression.Trim();
                if (string.IsNullOrEmpty(expr)) return;

                // If expression ends with an operator, remove it
                if (expr.Length > 0 && IsBinaryOperator(expr.Last().ToString()))
                {
                    expr = expr.Substring(0, expr.Length - 1);
                }

                // If there are unmatched '(' (e.g. when user used "|(" for abs) convert display abs to internal abs:
                // but more importantly auto-close parentheses before evaluation
                int open = expr.Count(c => c == '(');
                int close = expr.Count(c => c == ')');
                if (open > close) expr += new string(')', open - close);

                expr = PreprocessExpressionForEvaluation(expr);

                double result;
                try
                {
                    result = EvaluateExpression(expr);
                }
                catch (DivideByZeroException)
                {
                    DisplayLabel.Text = "Error";
                    _expression = "";
                    _displayExpression = "";
                    return;
                }
                catch
                {
                    DisplayLabel.Text = "Error";
                    _expression = "";
                    _displayExpression = "";
                    return;
                }

                if (double.IsInfinity(result) || double.IsNaN(result))
                {
                    DisplayLabel.Text = "Error";
                    _expression = "";
                    _displayExpression = "";
                    return;
                }

                DisplayLabel.Text = FormatResult(result);
                _expression = result.ToString(CultureInfo.InvariantCulture);
                _displayExpression = _expression;
            }
            catch
            {
                DisplayLabel.Text = "Error";
                _expression = "";
                _displayExpression = "";
            }
        }

        // ---------------------------
        // Trigonometry / Function buttons
        // Append function token so user can type number then '='
        // ---------------------------
        private void OnFunctionClicked(object sender, EventArgs e)
        {
            if (!(sender is Button btn)) return;
            string func = btn.Text ?? "";

            string token = func switch
            {
                "sin" => "sin(",
                "cos" => "cos(",
                "tan" => "tan(",
                "sin⁻¹" => "asin(",
                "cos⁻¹" => "acos(",
                "tan⁻¹" => "atan(",
                "log" => "log(",
                "ln" => "ln(",
                "abs" => "abs(",
                _ => null
            };

            if (token != null)
            {
                if (!string.IsNullOrEmpty(_expression))
                {
                    char last = _expression.Last();
                    if (char.IsDigit(last) || last == ')' || last == '.')
                    {
                        _expression += "*";
                        _displayExpression += "×"; // visual multiplication
                    }
                }

                _expression += token;
                // display inverse names nicer
                _displayExpression += token.Replace("asin(", "sin⁻¹(").Replace("acos(", "cos⁻¹(").Replace("atan(", "tan⁻¹(");
                UpdateDisplay();
            }
        }

        private double ApplyTrig(Func<double, double> trigFunc, double input)
        {
            double radians = _isDegMode ? input * Math.PI / 180.0 : input;
            return trigFunc(radians);
        }

        private double ApplyInverseTrig(Func<double, double> invFunc, double input)
        {
            double rad = invFunc(input);
            return _isDegMode ? rad * 180.0 / Math.PI : rad;
        }

        // ---------------------------
        // Utilities: parsing, evaluation, preprocessing
        // ---------------------------
        private bool TryGetNumericDisplayValue(out double value)
        {
            value = 0;
            if (DisplayLabel == null) return false;
            return double.TryParse(DisplayLabel.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private double EvaluateExpression(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr))
                throw new Exception("Empty expression");

            var exp = new Expression(expr, EvaluateOptions.IgnoreCase);

            // Provide constants like pi and e
            exp.EvaluateParameter += (name, args) =>
            {
                string param = name.ToLowerInvariant();
                if (param == "pi")
                    args.Result = Math.PI;
                else if (param == "e")
                    args.Result = Math.E;
            };

            exp.EvaluateFunction += (name, args) =>
            {
                string n = name.ToLowerInvariant();
                double p1 = args.Parameters.Length > 0 ? Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture) : 0;
                double p2 = args.Parameters.Length > 1 ? Convert.ToDouble(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture) : 0;

                switch (n)
                {
                    case "pow": args.Result = Math.Pow(p1, p2); break;
                    case "exp": args.Result = Math.Exp(p1); break;
                    case "sqrt": args.Result = Math.Sqrt(p1); break;
                    case "sin": args.Result = _isDegMode ? Math.Sin(p1 * Math.PI / 180.0) : Math.Sin(p1); break;
                    case "cos": args.Result = _isDegMode ? Math.Cos(p1 * Math.PI / 180.0) : Math.Cos(p1); break;
                    case "tan": args.Result = _isDegMode ? Math.Tan(p1 * Math.PI / 180.0) : Math.Tan(p1); break;
                    case "asin": args.Result = _isDegMode ? Math.Asin(p1) * 180.0 / Math.PI : Math.Asin(p1); break;
                    case "acos": args.Result = _isDegMode ? Math.Acos(p1) * 180.0 / Math.PI : Math.Acos(p1); break;
                    case "atan": args.Result = _isDegMode ? Math.Atan(p1) * 180.0 / Math.PI : Math.Atan(p1); break;
                    case "log": args.Result = Math.Log10(p1); break;
                    case "ln": args.Result = Math.Log(p1); break;
                    case "abs": args.Result = Math.Abs(p1); break;
                    case "fact":
                        {
                            int iv = (int)Math.Truncate(p1);
                            if (iv < 0) throw new Exception("Invalid factorial");
                            args.Result = Factorial(iv);
                        }
                        break;
                    default:
                        throw new ArgumentException("Unknown function: " + name);
                }
            };

            object res;
            try
            {
                res = exp.Evaluate();
            }
            catch (EvaluationException e)
            {
                if (e.Message != null && e.Message.ToLowerInvariant().Contains("divide"))
                    throw new DivideByZeroException(e.Message);
                throw;
            }

            if (res is double d && (double.IsInfinity(d) || double.IsNaN(d)))
            {
                throw new DivideByZeroException("Result is infinite or NaN (possible division by zero)");
            }

            return Convert.ToDouble(res, CultureInfo.InvariantCulture);
        }

        private string PreprocessExpressionForEvaluation(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) return expr;

            // Ensure decimal point is '.' for invariant parsing
            expr = expr.Replace(",", ".");

            // Convert '^' caret usage to pow(...) calls
            expr = ConvertCaretsToPow(expr);

            // Also transform our display-style absolute token "|(" back to abs( for evaluation,
            // in case the display had used '|(' representation.
            expr = expr.Replace("|", "abs(");

            return expr;
        }

        private string ConvertCaretsToPow(string expr)
        {
            string token = @"(?<token>(?:\([^\(\)]+\))|(?:[A-Za-z_][A-Za-z0-9_]*\([^\(\)]*\))|(?:-?\d+(\.\d+)?))";
            string pattern = token + @"\s*\^\s*" + token;

            while (true)
            {
                var m = Regex.Match(expr, pattern);
                if (!m.Success) break;

                string left = m.Groups["token"].Captures[0].Value;
                string right = m.Groups["token"].Captures[1].Value;

                string replacement = $"pow({left},{right})";
                expr = expr.Substring(0, m.Index) + replacement + expr.Substring(m.Index + m.Length);
            }

            return expr;
        }

        // ---------------------------
        // Toggle DEG/RAD
        // ---------------------------
        private void OnToggleDegRad(object sender, EventArgs e)
        {
            _isDegMode = !_isDegMode;
            if (DegRadButton != null)
                DegRadButton.Text = _isDegMode ? "DEG" : "RAD";
        }

        // ---------------------------
        // Show/hide function grids
        // ---------------------------
        private void OnShowTrigonometry(object sender, EventArgs e)
        {
            MainButtonGrid.IsVisible = true;
            FunctionButtonGrid.IsVisible = false;
        }

        private void OnShowFunction(object sender, EventArgs e)
        {
            MainButtonGrid.IsVisible = false;
            FunctionButtonGrid.IsVisible = true;
        }

        // ---------------------------
        // Display helpers
        // ---------------------------
        private void UpdateDisplay()
        {
            DisplayLabel.Text = string.IsNullOrEmpty(_displayExpression) ? "0" : _displayExpression;
        }

        private string ReplaceExpressionForDisplay(string expr)
        {
            if (string.IsNullOrEmpty(expr)) return "";

            // Replace token "pi" with symbol 'π' for user-friendly display.
            string disp = expr.Replace("pi", "π");

            // For nicer display, convert '*' to '×' and '/' to '÷' for readability
            disp = disp.Replace("*", "×").Replace("/", "÷");

            // Replace "abs(" with "|(" so user sees absolute value visually.
            disp = disp.Replace("abs(", "|");

            return disp;
        }

        // Replace individual appended token (like "*" to ×) for _displayExpression when appending single text
        private string ReplaceTokenForDisplay(string token)
        {
            return token.Replace("×", "×").Replace("/", "/");
        }

        private static string FormatResult(double value)
        {
            return value.ToString("G12", CultureInfo.InvariantCulture);
        }
    }
}