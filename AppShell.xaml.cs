using Microsoft.Maui.Controls;

namespace Calculator
{
    // AppShell sınıfı, uygulamanın genel navigasyon yapısını yönetir.
    // Flyout menüsünde tanımlanan sayfalar arasındaki yönlendirmeleri buradan kontrol ederiz.
    public partial class AppShell : Shell
    {
        // Yapıcı metod (Constructor)
        // Bu metod, AppShell başlatıldığında otomatik olarak çağrılır.
        public AppShell()
        {
            InitializeComponent(); // XAML dosyasındaki bileşenleri başlatır.

            // ScientificCalculatorPage sayfası için özel bir rota (yol) kaydı yapılır.
            // Böylece kullanıcı bilimsel hesap makinesi sayfasına yönlendirilebilir.
            Routing.RegisterRoute("Calculator/Scientific", typeof(ScientificCalculatorPage));
        }

        // Ayarlar butonuna tıklandığında çalışacak olay metodu (Event Handler)
        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            // Kullanıcı "Settings" butonuna bastığında,
            // Shell yapısı üzerinden SettingsPage sayfasına geçiş yapılır.
            await Shell.Current.GoToAsync(nameof(SettingsPage));
        }
    }
}
