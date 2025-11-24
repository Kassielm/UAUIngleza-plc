using System.ComponentModel;
using System.Runtime.CompilerServices;
using UAUIngleza_plc.Models;
using UAUIngleza_plc.Services;

namespace UAUIngleza_plc.Pages;

public partial class ConfiguracoesPage : ContentPage, INotifyPropertyChanged
{
    private readonly IStorageService _storageService;
    private readonly IPLCService _plcService;

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
                IpAddress = config.IpAddress;
                Rack = config.Rack;
                Slot = config.Slot;
                CameraIp = config.CameraIp;
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

            IsConnecting = true;
            StatusMessage = "🔄 Conectando ao PLC...";

            var config = new Models.SystemConfiguration
            {
                IpAddress = IpAddress,
                Rack = Rack,
                Slot = Slot
            };

            bool connected = await _plcService.ConnectAsync(config);

            if (connected)
            {
                IsConnected = true;
                StatusMessage = "✅ Conectado ao PLC com sucesso.";
            }
            else
            {
                IsConnected = false;
                StatusMessage = "❌ Falha ao conectar ao PLC.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro ao conectar: {ex.Message}";
        }
        finally
        {
            IsConnecting = false;
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
}

public static class ServiceHelper
{
    public static T GetService<T>() where T : class
    {
        return IPlatformApplication.Current?.Services.GetService(typeof(T)) as T;
    }
}