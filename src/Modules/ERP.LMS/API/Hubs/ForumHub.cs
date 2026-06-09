using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ERP.LMS.API.Hubs;

[Authorize]
public class ForumHub : Hub
{
    public async Task JoinSubjectForum(string subjectId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"forum-{subjectId}");

    public async Task LeaveSubjectForum(string subjectId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"forum-{subjectId}");
}
