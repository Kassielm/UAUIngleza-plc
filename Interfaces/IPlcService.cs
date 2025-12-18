using Sharp7.Rx;
using Sharp7.Rx.Enums;

namespace UAUIngleza_plc.Interfaces
{
    public interface IPlcService
    {
        Sharp7Plc? Plc { get; }
        IObservable<ConnectionState> ConnectionStatus { get; }
        bool IsConnected { get; }
        Task<bool> ConnectAsync();
        void Disconnect();
        Task StartAutoReconnect();
        void StopAutoReconnect();
        IObservable<T> ObserveAddress<T>(
            string address,
            TransmissionMode mode = TransmissionMode.OnChange
        )
            where T : struct;
        void Dispose();
    }
}
