using UAUIngleza_plc.ViewModels;

namespace UAUIngleza_plc.Pages
{
    public partial class ConfiguracoesPage : ContentPage
    {
        public ConfiguracoesPage(ConfiguracoesViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}