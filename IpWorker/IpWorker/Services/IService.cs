using System.Threading.Tasks;

namespace IpWorker.Services
{
    public interface IService
    {
        string Name { get; }
        Task<object> ProcessData(string data);
    }
}
