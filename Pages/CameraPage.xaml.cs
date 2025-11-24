using UAUIngleza_plc.Services;

namespace UAUIngleza_plc.Pages;

public partial class CameraPage : ContentPage
{
    private readonly IStorageService _storageService;

    private string _cameraIp = "";
    public CameraPage(IStorageService storageService)
    {
        InitializeComponent();
        _storageService = storageService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadConfiguration();
    }

    public void SetUrl(string url)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            CameraWebView.Source = url;
        }
    }

    private async Task LoadConfiguration()
    {
        try
        {
            var config = await _storageService.GetConfigAsync();

            if (config != null)
            {
                _cameraIp = config.CameraIp;
                Console.WriteLine($"IP Carregado: {_cameraIp}");

                SetUrl(_cameraIp);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao carregar config: {ex.Message}");
        }
    }
}
