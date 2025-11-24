namespace UAUIngleza_plc.Pages;

public partial class CameraPage : ContentPage
{
    public CameraPage()
    {
        InitializeComponent();
        SetUrl("https://google.com");
    }

    public void SetUrl(string url)
    {
        CameraWebView.Source = url;
    }
}
