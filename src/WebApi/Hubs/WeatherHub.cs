/*
    Creating a Hub is as easy as implementing the base class and creating arbitrary methods
    SendWeatherStatusUpdate can be called from the client proxy
    Can also be called server side. (Note: when calling it server side. Some Values in the this.Context might be null)
*/

using Microsoft.AspNetCore.SignalR;

namespace WebApi.Hubs
{
    public sealed class WeatherHub : Hub { }
}