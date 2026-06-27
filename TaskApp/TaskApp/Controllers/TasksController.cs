using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskApp.Models;
using TaskApp.Services;
using TaskStatus = TaskApp.Models.TaskStatus;

namespace TaskApp.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly IProjectService _projectService;

    public TasksController(ITaskService taskService, IProjectService projectService)
    {
        _taskService = taskService;
        _projectService = projectService;
    }

    // JWT sub claim is stored without claim-type remapping (MapInboundClaims = false)
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("Sub claim missing from token."));

    // GET /api/projects/{projectId}/tasks?status=Todo&priority=High
    [HttpGet("projects/{projectId:int}/tasks")]
    public async Task<IActionResult> GetByProject(
        int projectId,
        [FromQuery] TaskStatus? status,
        [FromQuery] Priority? priority)
    {
        var project = await _projectService.GetByIdAsync(projectId);
        if (project is null) return NotFound(new { error = "Project not found." });
        if (project.OwnerId != CurrentUserId) return Forbid();

        var tasks = await _taskService.GetByProjectAsync(projectId, status, priority);
        return Ok(tasks);
    }

    // POST /api/projects/{projectId}/tasks
    [HttpPost("projects/{projectId:int}/tasks")]
    public async Task<IActionResult> Create(int projectId, [FromBody] CreateTaskRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var project = await _projectService.GetByIdAsync(projectId);
        if (project is null) return NotFound(new { error = "Project not found." });
        if (project.OwnerId != CurrentUserId) return Forbid();

        var taskItem = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            Status = TaskStatus.Todo,          // always default on create
            Priority = request.Priority,
            DueDate = request.DueDate,
            OwnerId = CurrentUserId,
            ProjectId = projectId
        };

        var created = await _taskService.CreateAsync(taskItem);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // GET /api/tasks/{id}
    [HttpGet("tasks/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var task = await _taskService.GetByIdAsync(id);
        if (task is null) return NotFound(new { error = "Task not found." });
        if (task.Project.OwnerId != CurrentUserId) return Forbid();

        return Ok(task);
    }

    // PUT /api/tasks/{id}
    [HttpPut("tasks/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var task = await _taskService.GetByIdAsync(id);
        if (task is null) return NotFound(new { error = "Task not found." });
        if (task.Project.OwnerId != CurrentUserId) return Forbid();

        // Apply the update fields; OwnerId/ProjectId are not user-controllable via this endpoint
        task.Title = request.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;

        var updated = await _taskService.UpdateAsync(task);
        return Ok(updated);
    }

    // DELETE /api/tasks/{id}
    [HttpDelete("tasks/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var task = await _taskService.GetByIdAsync(id);
        if (task is null) return NotFound(new { error = "Task not found." });
        if (task.Project.OwnerId != CurrentUserId) return Forbid();

        await _taskService.DeleteAsync(id);
        return NoContent();
    }
}
