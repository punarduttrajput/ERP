using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ERP.Timetable.API.Hubs;

[Authorize]
public class TimetableHub : Hub
{
    public async Task JoinSemesterGroup(string semesterId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"timetable-{semesterId}");
    }

    public async Task LeaveSemesterGroup(string semesterId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"timetable-{semesterId}");
    }
}
