using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Linq;
using MikeCodesDotNET.Hubs;
using MikeCodesDotNET.Models.Storage;
using MikeCodesDotNET.Services.Blog;
using System.Threading;

namespace MikeCodesDotNET.Controllers;

[Route("api/[controller]")]
public class UpdatesController : Controller
{
    private bool EventTypeSubcriptionValidation
        => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
           "SubscriptionValidation";

    private bool EventTypeNotification
        => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
           "Notification";

    private readonly IHubContext<GridEventsHub> _hubContext;
    private readonly BlogPostStorageService _blogPostImportService;

    CancellationTokenSource _cts;
    CancellationToken _ct;


    public UpdatesController(IHubContext<GridEventsHub> gridEventsHubContext, BlogPostStorageService blogPostImportProcessorService)
    {
        _hubContext = gridEventsHubContext;
        _blogPostImportService = blogPostImportProcessorService;

        _cts = new CancellationTokenSource();
        _ct = _cts.Token;
    }

    [HttpOptions]
    public async Task<IActionResult> Options()
    {
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            var webhookRequestOrigin = HttpContext.Request.Headers["WebHook-Request-Origin"].FirstOrDefault();
            var webhookRequestCallback = HttpContext.Request.Headers["WebHook-Request-Callback"];
            var webhookRequestRate = HttpContext.Request.Headers["WebHook-Request-Rate"];
            HttpContext.Response.Headers.Add("WebHook-Allowed-Rate", "*");
            HttpContext.Response.Headers.Add("WebHook-Allowed-Origin", webhookRequestOrigin);
        }

        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            var jsonContent = await reader.ReadToEndAsync();

            // Check the event type.
            // Return the validation code if it's 
            // a subscription validation request. 
            if (EventTypeSubcriptionValidation)
            {
                return await HandleValidation(jsonContent);
            }
            else if (EventTypeNotification)
            {
                // Check to see if this is passed in using
                // the CloudEvents schema
                if (IsCloudEvent(jsonContent))
                {
                    return await HandleCloudEvent(jsonContent);
                }

                return await HandleGridEvents(jsonContent);
            }

            return BadRequest();
        }
    }


    private async Task<JsonResult> HandleValidation(string jsonContent)
    {
        var gridEvent =
            JsonConvert.DeserializeObject<List<GridEvent<Dictionary<string, string>>>>(jsonContent)
                .First();

        await this._hubContext.Clients.All.SendAsync(
            "gridupdate",
            gridEvent.Id,
            gridEvent.EventType,
            gridEvent.Subject,
            gridEvent.EventTime.ToLongTimeString(),
            jsonContent.ToString());

        // Retrieve the validation code and echo back.
        var validationCode = gridEvent.Data["validationCode"];
        return new JsonResult(new
        {
            validationResponse = validationCode
        });
    }

    private async Task<IActionResult> HandleGridEvents(string jsonContent)
    {
        var events = JArray.Parse(jsonContent);
        foreach (var e in events)
        {
            // Invoke a method on the clients for 
            // an event grid notiification.                        
            var details = JsonConvert.DeserializeObject<GridEvent<dynamic>>(e.ToString());
            await _hubContext.Clients.All.SendAsync(
                "gridupdate",
                details.Id,
                details.EventType,
                details.Subject,
                details.EventTime.ToLongTimeString(),
                e.ToString());
        }

        return Ok();
    }

    private async Task<IActionResult> HandleCloudEvent(string jsonContent)
    {

        var details = JsonConvert.DeserializeObject<CloudEvent<dynamic>>(jsonContent);
        var eventData = JObject.Parse(jsonContent);

        await _hubContext.Clients.All.SendAsync(
            "gridupdate",
            details.Id,
            details.Type,
            details.Subject,
            details.Time,
            eventData.ToString()
        );
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(details.Subject);
            if(!string.IsNullOrEmpty(fileName))
            {                
                switch(details.Type)
                {
                    case "Microsoft.Storage.BlobDeleted":
                        await BlogPostDeleteHandler(fileName);
                        break;
                    case "Microsoft.Storage.BlobCreated":
                        await BlogPostCreatedHandler(fileName);
                        break;
                    case "Microsoft.Storage.BlobRenamed":
                        break;
                    case "Microsoft.Storage.DirectoryCreated":
                        break;
                    case "Microsoft.Storage.DirectoryRenamed":
                        break;
                    case "Microsoft.Storage.DirectoryDeleted":
                        await BlogPostDirectoryDeleteHandler(fileName);
                        break;

                }
            }
            else
                throw new ArgumentNullException(nameof(fileName));
        }
        catch
        {
            return NoContent();
        }

        return Ok();
    }


    private async Task BlogPostDirectoryDeleteHandler(string fileName)
    {
            
    }

    private async Task BlogPostDeleteHandler(string fileName)
    {

    }

    private async Task BlogPostCreatedHandler(string fileName)
    {
        //Handle importing the blog post 
        await _blogPostImportService.ImportPost(fileName, _ct);
    }



    private static bool IsCloudEvent(string jsonContent)
    {
        // Cloud events are sent one at a time, while Grid events
        // are sent in an array. As a result, the JObject.Parse will 
        // fail for Grid events. 
        try
        {
            // Attempt to read one JSON object. 
            var eventData = JObject.Parse(jsonContent);

            // Check for the spec version property.
            var version = eventData["specversion"].Value<string>();
            if (!string.IsNullOrEmpty(version)) return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return false;
    }

}