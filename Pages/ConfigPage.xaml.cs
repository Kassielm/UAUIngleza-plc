using UAUIngleza_plc.Interfaces;
using UAUIngleza_plc.Services;

namespace UAUIngleza_plc.Pages
{
    public partial class ConfigPage : ContentPage
    {
        private readonly IConfigRepository _configRepository;

        public ConfigPage(IConfigRepository configRepository)
        {
            InitializeComponent();
            _configRepository = configRepository;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadConfiguration();
        }

        private async Task LoadConfiguration()
        {
            try
            {
                var config = await _configRepository.GetOneAsync<Models.SystemConfiguration>(0);

                if (config != null)
                {
                    IpEntry.Text = config.IpAddress ?? "192.168.2.1";
                    RackEntry.Text = config.Rack.ToString();
                    SlotEntry.Text = config.Slot.ToString();
                    CameraEntry.Text = config.CameraIp ?? "192.168.0.101";
                }
                else
                {
                    IpEntry.Text = "192.168.2.1";
                    RackEntry.Text = "0";
                    SlotEntry.Text = "1";
                    CameraEntry.Text = "192.168.0.101";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar configuração: {ex.Message}");
                IpEntry.Text = "192.168.2.1";
                RackEntry.Text = "0";
                SlotEntry.Text = "1";
                CameraEntry.Text = "192.168.0.101";
            }
        }

        private async void OnSaveConfigClicked(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateInputs())
                {
                    await DisplayAlertAsync(
                        "Erro",
                        "⚠️ Preencha todos os campos corretamente",
                        "OK"
                    );
                    return;
                }

                var config = new Models.SystemConfiguration
                {
                    IpAddress = IpEntry.Text?.Trim() ?? "192.168.2.1",
                    Rack = int.TryParse(RackEntry.Text, out int rack) ? rack : 0,
                    Slot = int.TryParse(SlotEntry.Text, out int slot) ? slot : 1,
                    CameraIp = CameraEntry.Text?.Trim() ?? "192.168.0.101",
                };

                await _configRepository.SaveAsync(config);

                await DisplayAlertAsync("Sucesso", "✅ Configuração salva com sucesso!", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", $"❌ Erro ao salvar: {ex.Message}", "OK");
            }
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(IpEntry.Text))
                return false;

            if (!int.TryParse(RackEntry.Text, out int rack) || rack < 0)
                return false;

            if (!int.TryParse(SlotEntry.Text, out int slot) || slot < 0)
                return false;

            return true;
        }
    }
}
