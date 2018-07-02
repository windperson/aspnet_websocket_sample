using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EchoApp.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

namespace EchoApp.Controllers
{
    public class BroadcastController : Controller
    {
        private readonly IHubContext<EchoHub> _hubContext;
        private IMemoryCache _memoryCache;
        private const string cacheKey = "msgHistory";

        public BroadcastController(IHubContext<EchoHub> hubContext, IMemoryCache memoryCache)
        {
            _hubContext = hubContext;
            _memoryCache = memoryCache;
        }

        [HttpGet]
        public IActionResult Index()
        {

            _memoryCache.TryGetValue(cacheKey, out List<string> messageHistory);

            if (messageHistory == null)
            {
                messageHistory = new List<string>();
            }
            
            return View(messageHistory);
        }

        [HttpPost]
        public async Task<IActionResult> BroadcastToClient(string sendMessage)
        {
            await _hubContext.Clients.All.SendAsync("OnBroadcast", sendMessage);

            List<string> messageHistory;

            if (_memoryCache.TryGetValue(cacheKey, out messageHistory))
            {
                messageHistory.Add(sendMessage);
            }
            else
            {
                messageHistory = new List<string> {sendMessage};
                _memoryCache.Set(cacheKey, messageHistory,
                    new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.NeverRemove));
            }


            return RedirectToAction(nameof(Index));

        }
    }
}