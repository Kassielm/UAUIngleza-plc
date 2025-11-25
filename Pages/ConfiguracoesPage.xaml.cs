using Sharp7.Rx.Enums;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using UAUIngleza_plc.Models;
using UAUIngleza_plc.Services;

namespace UAUIngleza_plc.Pages;

public partial class ConfiguracoesPage : ContentPage, INotifyPropertyChanged
{
    private readonly IStorageService _storageService;
    private readonly IPLCService _plcService;
    private CompositeDisposable _disposables = new CompositeDisposable();
    private string _ipAddress = "";
    private int _rack = 0;
    private int _slot = 0;
    private string _cameraIp = "";
    private string _statusMessage = "Aguardando configuração...";
    private bool _isConnecting = false;
    private bool _isConnected = false;

    public string IpAddress
    {
        get => _ipAddress;
        set
        {
            if (_ipAddress != value)
            {
                _ipAddress = value;
                OnPropertyChanged();
            }
        }
    }

    public int Rack
    {
        get => _rack;
        set
        {
            if (_rack != value)
            {
                _rack = value;
                OnPropertyChanged();
            }
        }
    }

    public int Slot
    {
        get => _slot;
        set
        {
            if (_slot != value)
            {
                _slot = value;
                OnPropertyChanged();
            }
        }
    }

    public string CameraIp
    {
        get => _cameraIp;
        set
        {
            if (_cameraIp != value)
            {
                _cameraIp = value;
                OnPropertyChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsConnecting
    {
        get => _isConnecting;
        set
        {
            if (_isConnecting != value)
            {
                _isConnecting = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (_isConnected != value)
            {
                _isConnected = value;
                OnPropertyChanged();
            }
        }
    }

    public ConfiguracoesPage()
    {
        InitializeComponent();

        _storageService = ServiceHelper.GetService<IStorageService>();
        _plcService = ServiceHelper.GetService<IPLCService>();

        BindingContext = this;

        LoadConfiguration();
    }

    private async void LoadConfiguration()
    {
        try
        {
            var config = await _storageService.GetConfigAsync();
            if (config != null)
            {
                IpAddress = config.IpAddress ?? string.Empty;
                Rack = config.Rack;
                Slot = config.Slot;
                CameraIp = config.CameraIp ?? string.Empty;
                StatusMessage = "✅ Configuração carregada.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro ao carregar: {ex.Message}";
        }
    }

    private async void OnSaveConfigClicked(object sender, EventArgs e)
    {
        try
        {
            if (!ValidateInputs())
            {
                StatusMessage = "⚠️ Configuração inválida. Verifique os campos.";
                return;
            }
            
            var config = new Models.SystemConfiguration
            {
                IpAddress = IpAddress,
                Rack = Rack,
                Slot = Slot,
                CameraIp = CameraIp
            };

            await _storageService.SaveConfigAsync(config);
            StatusMessage = "✅ Configuração salva com sucesso.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro ao salvar: {ex.Message}";
        }
    }

    private async void OnConnectClicked(object sender, EventArgs e)
    {
        try
        {
            if (!ValidateInputs())
            {
                StatusMessage = "⚠️ Configuração inválida. Verifique os campos.";
                return;
            }

            // Salva antes de conectar
            var config = new Models.SystemConfiguration
            {
                IpAddress = IpAddress,
                Rack = Rack,
                Slot = Slot,
                CameraIp = CameraIp
            };
            await _storageService.SaveConfigAsync(config);

            IsConnecting = true;
            StatusMessage = "🔄 Conectando ao PLC...";

            if (_plcService.IsConnected)
            {
                _plcService.Disconnect();
                await Task.Delay(500);
            }

            bool connected = await _plcService.ConnectAsync();

            if (connected)
            {
                IsConnected = true;
                StatusMessage = "✅ Conectado ao PLC com sucesso.";
                
                await _plcService.StartAutoReconnect();
            }
            else
            {
                IsConnected = false;
                StatusMessage = "❌ Falha ao conectar ao PLC. Verifique as configurações.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro ao conectar: {ex.Message}";
            IsConnected = false;
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private void OnDisconnectClicked(object sender, EventArgs e)
    {
        try
        {
            _plcService.Disconnect();
            IsConnected = false;
            StatusMessage = "🔌 Desconectado do PLC.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro ao desconectar: {ex.Message}";
        }
    }

    private bool ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(IpAddress))
            return false;
        if (Rack < 0 || Slot < 0)
            return false;
        return true;
    }

    public new event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CheckStatus();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _disposables.Clear();
    }

    public void CheckStatus()
    {
        try
        {
            // Subscreve ao status de conexão global do serviço
            var subConexao = _plcService.ConnectionStatus
                .DistinctUntilChanged()
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(
                    state =>
                    {
                        ChangeStyleConnection(state);
                        IsConnected = (state == ConnectionState.Connected);
                    },
                    error =>
                    {
                        Console.WriteLine($"Erro ao monitorar conexão: {error.Message}");
                    });

            _disposables.Add(subConexao);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao subscrever status: {ex.Message}");
        }
    }

    private void ChangeStyleConnection(ConnectionState estado)
    {
        if (estado == ConnectionState.Connected)
        {
            StatusBorder.BackgroundColor = Colors.Green;
            StatusLabel.Text = "PLC ONLINE ✅";
            StatusMessage = "✅ PLC conectado e operacional";
        }
        else
        {
            // Qualquer outro estado é tratado como desconectado
            StatusBorder.BackgroundColor = Colors.Red;
            StatusLabel.Text = "PLC OFFLINE ❌";
            StatusMessage = "❌ PLC desconectado. Tentando reconectar...";
        }
    }
}

public static class ServiceHelper
{
    public static T GetService<T>() where T : class
    {
        return IPlatformApplication.Current?.Services.GetService(typeof(T)) as T;
    }
}