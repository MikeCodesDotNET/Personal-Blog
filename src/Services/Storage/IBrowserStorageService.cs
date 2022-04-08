using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MikeCodesDotNET.Services
{
    public interface IBrowserStorageService
    {
        Task<T> GetItem<T>(string key);
        Task SetItem<T>(string key, T value);
        Task RemoveItem(string key);
    }
}
