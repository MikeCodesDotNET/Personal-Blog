using Microsoft.JSInterop;
using System.Text.Json;
using System.Threading.Tasks;

namespace MikeCodesDotNET.Services;
public class BrowserStorageService : IBrowserStorageService
{

    private IJSRuntime _jsRuntime;

    public BrowserStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<T> GetItem<T>(string key)
    {
        var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);

        if(json == null)
            return default;

        return JsonSerializer.Deserialize<T>(json);
    }

    public async Task SetItem<T>(string key, T value)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, JsonSerializer.Serialize(value));
    }

    public async Task RemoveItem(string key)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
    }

}
