using Sharp7.Rx.Enums;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using UAUIngleza_plc.Services;

namespace UAUIngleza_plc.Pages;

public partial class CameraPage : ContentPage, INotifyPropertyChanged
{
    private readonly IStorageService _storageService;
    private readonly IPLCService _plcService;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private string _cameraIp = "";
    private bool _isPlcConnected = false;

    public bool IsPlcConnected
    {
        get => _isPlcConnected;
        set
        {
            if (_isPlcConnected != value)
            {
                _isPlcConnected = value;
                OnPropertyChanged();
            }
        }
    }

    public CameraPage(IStorageService storageService, IPLCService plcService)
    {
        InitializeComponent();
        _storageService = storageService;
        _plcService = plcService;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadConfiguration();
        SubscribeToPlcStatus();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _disposables.Clear();
    }

    private void SubscribeToPlcStatus()
    {
        try
        {
            var statusSubscription = _plcService.ConnectionStatus
                .DistinctUntilChanged()
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(
                    state =>
                    {
                        IsPlcConnected = (state == ConnectionState.Connected);
                        Console.WriteLine($"Status PLC na Camera: {(IsPlcConnected ? "Conectado" : "Desconectado")}");
                    },
                    error =>
                    {
                        Console.WriteLine($"Erro ao monitorar status PLC: {error.Message}");
                        IsPlcConnected = false;
                    });

            _disposables.Add(statusSubscription);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao subscrever status PLC: {ex.Message}");
        }
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
                _cameraIp = config.CameraIp ?? string.Empty;
                Console.WriteLine($"IP Carregado: {_cameraIp}");

                SetUrl(_cameraIp);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao carregar config: {ex.Message}");
        }
    }

    private async void OnRecipeClicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            string recipeNumber = button.CommandParameter?.ToString() ?? "0";
            
            Console.WriteLine($"?? Receita {recipeNumber} selecionada");

            if (!IsPlcConnected)
            {
                await DisplayAlert("Aviso", "PLC não está conectado!", "OK");
                return;
            }

            try
            {
                // Aqui você pode adicionar a lógica para enviar a receita ao PLC
                // Exemplo: await _plcService.Plc.SetValue<short>($"DB1.DBW{recipeNumber}", 1);
                
                await DisplayAlert("Receita Selecionada", $"Receita {recipeNumber} foi selecionada", "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao selecionar receita: {ex.Message}");
                await DisplayAlert("Erro", $"Erro ao selecionar receita: {ex.Message}", "OK");
            }
        }
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
