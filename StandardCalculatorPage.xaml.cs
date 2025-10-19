using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Calculator
{
    // Bu sınıf, standart hesap makinesi sayfasının mantığını içerir.
    public partial class StandardCalculatorPage : ContentPage
    {
        // İlk girilen sayı
        private double _firstNumber;

        // Seçilen işlem (+, -, ×, ÷ vb.)
        private string _operation;

        // İşlem butonuna basılıp basılmadığını kontrol eder
        private bool _isOperationClicked;

        // Bellekte saklanan değer
        private double _memory;

        // Yeni giriş yapılıp yapılmadığını takip eder
        private bool _isNewInput;

        // Son yapılan işlemi metin olarak tutar (ekranda gösterilir)
        private string _lastOperationString;

        public StandardCalculatorPage()
        {
            InitializeComponent(); // XAML arayüzünü başlatır
            OperationDisplay.Text = ""; // Üstteki işlem ekranını boş başlatır
        }

        // ------------------------------
        // Rakam butonuna basıldığında çalışır
        // ------------------------------
        private void OnNumberClicked(object sender, EventArgs e)
        {
            Button button = (Button)sender; // Basılan butonu alır

            // Yeni giriş başladıysa veya ekran sıfırsa, mevcut sayıyı değiştir
            if (_isNewInput || Display.Text == "0" || Display.Text.Contains("Error"))
            {
                Display.Text = button.Text;
                _isNewInput = false;
            }
            else
            {
                // Aksi halde rakamı mevcut sayının sonuna ekle
                Display.Text += button.Text;
            }

            UpdateOperationDisplay(); // İşlem ekranını güncelle
        }

        // ------------------------------
        // Virgül (ondalık) butonuna basıldığında çalışır
        // ------------------------------
        private void OnDecimalClicked(object sender, EventArgs e)
        {
            // Ekranda daha önce virgül yoksa ekle
            if (!Display.Text.Contains(",") && !Display.Text.Contains("."))
            {
                Display.Text += ".";
            }
        }

        // ------------------------------
        // İşlem (+, -, ×, ÷) butonuna basıldığında çalışır
        // ------------------------------
        private void OnOperatorClicked(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            // Mevcut ekrandaki değeri birinci sayı olarak al
            double.TryParse(Display.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out _firstNumber);

            _operation = button.Text; // İşlemi sakla
            _isOperationClicked = true; // İşlem tuşuna basıldı
            _isNewInput = true; // Yeni giriş bekleniyor
            _lastOperationString = $"{_firstNumber} {_operation}"; // Ekranda gösterilecek işlem metni
            OperationDisplay.Text = _lastOperationString;
        }

        // ------------------------------
        // "=" tuşuna basıldığında çalışır, sonucu hesaplar
        // ------------------------------
        private void OnEqualsClicked(object sender, EventArgs e)
        {
            double secondNumber;
            // İkinci sayıyı oku
            double.TryParse(Display.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out secondNumber);
            double result = 0;

            // √ (karekök) işlemi
            if (_operation == "sqrt")
            {
                OperationDisplay.Text = $"√({secondNumber})";

                // Negatif sayının karekökü alınamaz
                if (secondNumber < 0)
                {
                    Display.Text = "Error";
                    OperationDisplay.Text = "Error";
                }
                else
                {
                    // Kareköklü sonucu hesapla
                    result = Math.Sqrt(secondNumber);
                    Display.Text = result.ToString(CultureInfo.InvariantCulture);
                    OperationDisplay.Text = $"√({secondNumber}) = {result.ToString(CultureInfo.InvariantCulture)}";
                }

                _operation = null;
                _isNewInput = true;
                return;
            }

            // 1/x (ters alma) işlemi
            if (_operation == "recip")
            {
                OperationDisplay.Text = $"1/({secondNumber})";

                // 0’a bölünme hatası kontrolü
                if (Math.Abs(secondNumber) < 1e-12)
                {
                    Display.Text = "Error";
                    OperationDisplay.Text = "Error";
                }
                else
                {
                    // Sayının tersini hesapla
                    result = 1.0 / secondNumber;
                    Display.Text = result.ToString(CultureInfo.InvariantCulture);
                    OperationDisplay.Text = $"1/({secondNumber}) = {result.ToString(CultureInfo.InvariantCulture)}";
                }

                _operation = null;
                _isNewInput = true;
                return;
            }

            // Temel işlemler (+, -, ×, ÷)
            try
            {
                string opDisplay = $"{_firstNumber} {_operation} {secondNumber} = ";

                switch (_operation)
                {
                    case "+":
                        result = _firstNumber + secondNumber;
                        break;
                    case "-":
                        result = _firstNumber - secondNumber;
                        break;
                    case "×":
                        result = _firstNumber * secondNumber;
                        break;
                    case "÷":
                        // Sıfıra bölme hatası kontrolü
                        if (Math.Abs(secondNumber) < 1e-12)
                            throw new DivideByZeroException();
                        result = _firstNumber / secondNumber;
                        break;
                }

                // Sonucu ekranda göster
                Display.Text = result.ToString(CultureInfo.InvariantCulture);
                OperationDisplay.Text = opDisplay + result.ToString(CultureInfo.InvariantCulture);
            }
            catch (DivideByZeroException)
            {
                // Sıfıra bölme hatası
                Display.Text = "Error";
                OperationDisplay.Text = "Error";
            }
            catch (Exception)
            {
                // Diğer genel hatalar
                Display.Text = "Error";
                OperationDisplay.Text = "Error";
            }

            _isOperationClicked = false;
            _isNewInput = true;
        }

        // ------------------------------
        // "C" tuşu → tüm işlemi temizler
        // ------------------------------
        private void OnClearClicked(object sender, EventArgs e)
        {
            Display.Text = "0";
            _operation = null;
            _firstNumber = 0;
            _isOperationClicked = false;
            _isNewInput = false;
            OperationDisplay.Text = "";
        }

        // ------------------------------
        // "CE" tuşu → sadece mevcut ekranı temizler
        // ------------------------------
        private void OnClearEntryClicked(object sender, EventArgs e)
        {
            Display.Text = "0";
        }

        // ------------------------------
        // "←" (geri sil) tuşu → son karakteri siler
        // ------------------------------
        private void OnBackspaceClicked(object sender, EventArgs e)
        {
            if (Display.Text.Length > 1)
                Display.Text = Display.Text.Substring(0, Display.Text.Length - 1);
            else
                Display.Text = "0";
        }

        // ------------------------------
        // "%" tuşu → yüzde hesabı yapar
        // ------------------------------
        private void OnPercentClicked(object sender, EventArgs e)
        {
            double number;
            if (double.TryParse(Display.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out number))
            {
                double result = number / 100.0;
                OperationDisplay.Text = $"{number} % = {result.ToString(CultureInfo.InvariantCulture)}";
                Display.Text = result.ToString(CultureInfo.InvariantCulture);
            }
        }

        // ------------------------------
        // 1/x tuşu → sayının tersini almak için işlem moduna geçer
        // ------------------------------
        private void OnReciprocalClicked(object sender, EventArgs e)
        {
            _operation = "recip"; // işlem türü ters alma
            _isOperationClicked = true;
            _isNewInput = true;

            // Kullanıcıya işlem bekleniyor bilgisi göster
            OperationDisplay.Text = "1/(";
            Display.Text = "0";
        }

        // ------------------------------
        // x² (karesini al) butonu
        // ------------------------------
        private void OnSquareClicked(object sender, EventArgs e)
        {
            double number;
            if (double.TryParse(Display.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out number))
            {
                double result = number * number;
                OperationDisplay.Text = $"{number.ToString(CultureInfo.InvariantCulture)}² = {result.ToString(CultureInfo.InvariantCulture)}";
                Display.Text = result.ToString(CultureInfo.InvariantCulture);
            }
        }

        // ------------------------------
        // √ (karekök) butonu
        // ------------------------------
        private void OnSqrtClicked(object sender, EventArgs e)
        {
            _operation = "sqrt";
            _isOperationClicked = true;
            _isNewInput = true;
            OperationDisplay.Text = "√(";
            Display.Text = "0";
        }

        // ------------------------------
        // ± tuşu → işaret değiştirir
        // ------------------------------
        private void OnSignClicked(object sender, EventArgs e)
        {
            double number;
            if (double.TryParse(Display.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out number))
            {
                double result = -number;
                OperationDisplay.Text = $"{result.ToString(CultureInfo.InvariantCulture)}";
                Display.Text = result.ToString(CultureInfo.InvariantCulture);
            }
        }

        // ------------------------------
        // Hafıza (Memory) tuşları: MC, MR, M+, M-, MS
        // ------------------------------
        private void OnMemoryClicked(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            double number;
            double.TryParse(Display.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out number);

            switch (button.Text)
            {
                case "MC": // Belleği temizle
                    _memory = 0;
                    OperationDisplay.Text = "Memory cleared";
                    break;

                case "MR": // Belleği çağır
                    Display.Text = _memory.ToString(CultureInfo.InvariantCulture);
                    OperationDisplay.Text = $"Memory recall: {_memory.ToString(CultureInfo.InvariantCulture)}";
                    break;

                case "M+": // Belleğe ekle
                    _memory += number;
                    OperationDisplay.Text = $"Memory add: {_memory.ToString(CultureInfo.InvariantCulture)}";
                    break;

                case "M-": // Bellekten çıkar
                    _memory -= number;
                    OperationDisplay.Text = $"Memory subtract: {_memory.ToString(CultureInfo.InvariantCulture)}";
                    break;

                case "MS": // Belleğe kaydet
                    _memory = number;
                    OperationDisplay.Text = $"Memory saved: {_memory.ToString(CultureInfo.InvariantCulture)}";
                    break;
            }
        }

        // ------------------------------
        // Ekranın üst kısmındaki işlem metnini günceller
        // ------------------------------
        private void UpdateOperationDisplay()
        {
            if (!string.IsNullOrEmpty(_operation) && !_isOperationClicked && _operation != "sqrt")
            {
                OperationDisplay.Text = $"{Display.Text} {_operation}";
            }
        }
    }
}
