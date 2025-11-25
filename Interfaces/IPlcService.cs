namespace UAUIngleza_plc.Interfaces
{
    public interface IPlcService
    {
        Task<bool> Connect();
        Task<bool> EnsureConnection();
    }
}