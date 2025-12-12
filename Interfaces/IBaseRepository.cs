using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UAUIngleza_plc.Models;
using UAUIngleza_plc.Services;

namespace UAUIngleza_plc.Interfaces
{
    public interface IBaseRepository
    {
        Task<List<T>> GetAsync<T>() where T : class;
        Task<T?> GetOneAsync<T>(int id) where T : class;
        Task SaveAsync<T>(T type) where T : class;
        Task UpdateAsync<T>(T type) where T : class;
        Task DeleteAsync<T>(T type) where T : class;
    }
}
