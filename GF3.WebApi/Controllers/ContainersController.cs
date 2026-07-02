using BusinessLogicLayer.Services.Abstractions;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Infrastructure;
using WebApi.Contracts.Containers;
using WebApi.Contracts.Containers.Graphs;
using WebApi.Contracts.Containers.Graphs.CellStyles;
using WebApi.Contracts.Containers.Graphs.Employees;
using WebApi.Contracts.Containers.Graphs.Slots;
using WebApi.Contracts.Containers.SchedulePresets;
using WebApi.Mappers;
using WebApi.Realtime;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AuthRoles.Manager)]
/// <summary>
/// Main HTTP API for containers and all nested graph resources.
/// This controller intentionally mirrors the aggregate structure from the business layer,
/// which makes the route tree predictable for frontend code and keeps ownership boundaries explicit.
/// </summary>
public class ContainersController(
    IContainerService containerService,
    IWorkflowLogService workflowLogService,
    IRealtimeNotifier realtimeNotifier,
    IManagerEditLockService? editLockService = null,
    IScheduleLastUpdateService? scheduleLastUpdateService = null) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ContainerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ContainerDto>>> GetAll(CancellationToken cancellationToken)
    {
        var containers = await containerService.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return Ok(containers.Select(x => x.ToApiDto()));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ContainerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ContainerDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var container = await containerService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (container is null)
        {
            return NotFound(CreateNotFoundProblem($"Container with id {id} was not found."));
        }

        return Ok(container.ToApiDto());
    }

    [HttpGet("{containerId:int}/graphs")]
    [ProducesResponseType(typeof(IEnumerable<GraphDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<GraphDto>>> GetGraphs(int containerId, CancellationToken cancellationToken)
    {
        var graphs = await containerService.GetGraphsAsync(containerId, cancellationToken).ConfigureAwait(false);
        if (graphs is null)
        {
            return NotFound(CreateNotFoundProblem($"Container with id {containerId} was not found."));
        }

        var lastUpdates = await GetLastUpdatesAsync(graphs.Select(graph => graph.Id), cancellationToken)
            .ConfigureAwait(false);
        return Ok(graphs.Select(graph => graph.ToGraphDto(lastUpdates.GetValueOrDefault(graph.Id))));
    }

    [HttpGet("{containerId:int}/schedule-presets")]
    [ProducesResponseType(typeof(IEnumerable<SchedulePresetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<SchedulePresetDto>>> GetSchedulePresets(int containerId, CancellationToken cancellationToken)
    {
        var presets = await containerService.GetSchedulePresetsAsync(containerId, cancellationToken).ConfigureAwait(false);
        if (presets is null)
        {
            return NotFound(CreateNotFoundProblem($"Container with id {containerId} was not found."));
        }

        return Ok(presets.Select(x => x.ToSchedulePresetDto()));
    }

    [HttpPost("{containerId:int}/schedule-presets")]
    [ProducesResponseType(typeof(SchedulePresetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SchedulePresetDto>> CreateSchedulePreset(int containerId, [FromBody] CreateSchedulePresetRequest request, CancellationToken cancellationToken)
    {
        var created = await containerService.CreateSchedulePresetAsync(containerId, request.ToCreateModel(containerId), cancellationToken).ConfigureAwait(false);
        var dto = created.ToSchedulePresetDto();
        await LogManagerActionAsync($"Created schedule preset {dto.Name}.", cancellationToken).ConfigureAwait(false);
        await realtimeNotifier
            .NotifyManagerDataChangedAsync(ManagerEditResourceTypes.Container, containerId.ToString(), "manager-schedule-preset-created", containerId)
            .ConfigureAwait(false);
        return CreatedAtAction(nameof(GetSchedulePresets), new { containerId }, dto);
    }

    [HttpGet("{containerId:int}/graphs/{graphId:int}")]
    [ProducesResponseType(typeof(GraphDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GraphDto>> GetGraphById(int containerId, int graphId, CancellationToken cancellationToken)
    {
        var graph = await containerService.GetGraphByIdAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
        if (graph is null)
        {
            return NotFound(CreateNotFoundProblem($"Graph with id {graphId} was not found."));
        }

        var lastUpdates = await GetLastUpdatesAsync([graph.Id], cancellationToken).ConfigureAwait(false);
        return Ok(graph.ToGraphDto(lastUpdates.GetValueOrDefault(graph.Id)));
    }

    [HttpPost("{containerId:int}/graphs")]
    [ProducesResponseType(typeof(GraphDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GraphDto>> CreateGraph(int containerId, [FromBody] CreateGraphRequest request, CancellationToken cancellationToken)
    {
        var created = await containerService.CreateGraphAsync(containerId, request.ToCreateModel(containerId), cancellationToken).ConfigureAwait(false);
        var dto = created.ToGraphDto();
        var isPublished = created.PublicationStatus == SchedulePublicationStatus.Public;
        await LogManagerActionAsync(
            $"{(isPublished ? "Published" : "Created")} {DescribeSchedule(created)}.",
            cancellationToken).ConfigureAwait(false);
        await NotifyGraphChangedAsync(
            containerId,
            dto.Id,
            isPublished ? "manager-schedule-published" : "manager-schedule-created").ConfigureAwait(false);
        return CreatedAtAction(nameof(GetGraphById), new { containerId, graphId = dto.Id }, dto);
    }

    [HttpPut("{containerId:int}/graphs/{graphId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateGraph(int containerId, int graphId, [FromBody] UpdateGraphRequest request, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(ManagerEditLockTargets.Schedule(containerId, graphId), "This schedule") is { } conflict)
        {
            return conflict;
        }

        var existing = await containerService.GetGraphByIdAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
        var updatedModel = request.ToUpdateModel(containerId, graphId);
        var becamePublic =
            existing?.PublicationStatus != SchedulePublicationStatus.Public &&
            updatedModel.PublicationStatus == SchedulePublicationStatus.Public;

        await containerService.UpdateGraphAsync(containerId, graphId, updatedModel, cancellationToken).ConfigureAwait(false);
        var logModel = await containerService.GetGraphByIdAsync(containerId, graphId, cancellationToken).ConfigureAwait(false)
            ?? updatedModel;
        await LogManagerActionAsync(
            $"{(becamePublic ? "Published" : "Updated")} {DescribeSchedule(logModel)}.",
            cancellationToken).ConfigureAwait(false);
        await NotifyGraphChangedAsync(
            containerId,
            graphId,
            becamePublic ? "manager-schedule-published" : "manager-schedule-updated").ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("{containerId:int}/graphs/{graphId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteGraph(int containerId, int graphId, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(ManagerEditLockTargets.Schedule(containerId, graphId), "This schedule") is { } conflict)
        {
            return conflict;
        }

        var existing = await containerService.GetGraphByIdAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
        await containerService.DeleteGraphAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
        await LogManagerActionAsync(
            $"Deleted {(existing is null ? "schedule" : DescribeSchedule(existing))}.",
            cancellationToken).ConfigureAwait(false);
        await NotifyGraphChangedAsync(containerId, graphId, "manager-schedule-deleted").ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{containerId:int}/graphs/generate-preview")]
    [ProducesResponseType(typeof(GenerateGraphResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GenerateGraphResponse>> GenerateGraphPreview(
        int containerId,
        [FromBody] GenerateGraphPreviewRequest request,
        CancellationToken cancellationToken)
    {
        // Preview never writes to the database. It reuses the generation pipeline and returns
        // the in-memory slot set so the editor can show the result before persisting anything.
        var result = await containerService
            .GenerateGraphPreviewAsync(
                containerId,
                request.ToPreviewModel(containerId),
                request.Employees.Select(x => x.ToPreviewModel(request.GraphId ?? 0)),
                progress: null,
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(new GenerateGraphResponse
        {
            ContainerId = result.ContainerId,
            GraphId = result.GraphId,
            GeneratedSlotsCount = result.GeneratedSlotsCount,
            WrittenSlotsCount = result.WrittenSlotsCount,
            Slots = result.Slots.Select(x => x.ToGraphSlotDto())
        });
    }

    [HttpPost("{containerId:int}/graphs/{graphId:int}/generate")]
    [ProducesResponseType(typeof(GenerateGraphResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GenerateGraphResponse>> GenerateGraph(int containerId, int graphId, [FromBody] GenerateGraphRequest request, CancellationToken cancellationToken)
    {
        if (!request.DryRun &&
            CreateEditLockConflictResult(ManagerEditLockTargets.Schedule(containerId, graphId), "This schedule") is { } conflict)
        {
            return conflict;
        }

        var result = await containerService
            .GenerateGraphAsync(containerId, graphId, request.Overwrite, request.DryRun, progress: null, cancellationToken)
            .ConfigureAwait(false);

        // Returning slots is optional for persisted generation because some callers only care
        // about counts. Dry-run always returns slots because there is no database write to inspect later.
        var includeSlots = request.DryRun || request.ReturnSlots;
        if (!request.DryRun && result.WrittenSlotsCount > 0)
        {
            var graph = await containerService.GetGraphByIdAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
            await LogManagerActionAsync(
                $"Generated {result.WrittenSlotsCount} shifts for {DescribeScheduleOrFallback(graph)}.",
                cancellationToken).ConfigureAwait(false);
            await NotifyGraphChangedAsync(containerId, graphId, "manager-schedule-generated").ConfigureAwait(false);
        }

        return Ok(new GenerateGraphResponse
        {
            ContainerId = result.ContainerId,
            GraphId = result.GraphId,
            GeneratedSlotsCount = result.GeneratedSlotsCount,
            WrittenSlotsCount = result.WrittenSlotsCount,
            Slots = includeSlots ? result.Slots.Select(x => x.ToGraphSlotDto()) : null
        });
    }

    [HttpGet("{containerId:int}/graphs/{graphId:int}/slots")]
    [ProducesResponseType(typeof(IEnumerable<GraphSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<GraphSlotDto>>> GetGraphSlots(int containerId, int graphId, CancellationToken cancellationToken)
    {
        var slots = await containerService.GetGraphSlotsAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
        if (slots is null)
        {
            return NotFound(CreateNotFoundProblem($"Graph with id {graphId} was not found."));
        }

        return Ok(slots.Select(x => x.ToGraphSlotDto()));
    }

    [HttpPut("{containerId:int}/graphs/{graphId:int}/slots")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReplaceGraphSlots(int containerId, int graphId, [FromBody] ReplaceGraphSlotsRequest request, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(ManagerEditLockTargets.Schedule(containerId, graphId), "This schedule") is { } conflict)
        {
            return conflict;
        }

        await containerService.ReplaceGraphSlotsAsync(containerId, graphId, request.ToReplaceModels(graphId), cancellationToken).ConfigureAwait(false);
        var graph = await containerService.GetGraphByIdAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
        await LogManagerActionAsync(
            $"Saved the schedule matrix for {DescribeScheduleOrFallback(graph)} with {request.Slots.Count} shifts.",
            cancellationToken).ConfigureAwait(false);
        await NotifyGraphChangedAsync(containerId, graphId, "manager-schedule-slots-replaced").ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{containerId:int}/graphs/{graphId:int}/slots")]
    [ProducesResponseType(typeof(GraphSlotDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GraphSlotDto>> CreateGraphSlot(int containerId, int graphId, [FromBody] CreateGraphSlotRequest request, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(ManagerEditLockTargets.Schedule(containerId, graphId), "This schedule") is { } conflict)
        {
            return conflict;
        }

        var created = await containerService.CreateGraphSlotAsync(containerId, graphId, request.ToCreateModel(graphId), cancellationToken).ConfigureAwait(false);
        var graph = await containerService.GetGraphByIdAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
        await LogManagerActionAsync(
            $"Added {DescribeShift(created, graph)} to {DescribeScheduleOrFallback(graph)}.",
            cancellationToken).ConfigureAwait(false);
        await NotifyGraphChangedAsync(containerId, graphId, "manager-schedule-slot-created").ConfigureAwait(false);
        return CreatedAtAction(nameof(GetGraphSlots), new { containerId, graphId }, created.ToGraphSlotDto());
    }

    [HttpPut("{containerId:int}/graphs/{graphId:int}/slots/{slotId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateGraphSlot(int containerId, int graphId, int slotId, [FromBody] UpdateGraphSlotRequest request, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(ManagerEditLockTargets.Schedule(containerId, graphId), "This schedule") is { } conflict)
        {
            return conflict;
        }

        await containerService.UpdateGraphSlotAsync(containerId, graphId, slotId, request.ToUpdateModel(graphId, slotId), cancellationToken).ConfigureAwait(false);
        var graph = await containerService.GetGraphByIdAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
        var updatedSlot = graph?.Slots.FirstOrDefault(slot => slot.Id == slotId) ?? request.ToUpdateModel(graphId, slotId);
        await LogManagerActionAsync(
            $"Updated {DescribeShift(updatedSlot, graph)} in {DescribeScheduleOrFallback(graph)}.",
            cancellationToken).ConfigureAwait(false);
        await NotifyGraphChangedAsync(containerId, graphId, "manager-schedule-slot-updated").ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("{containerId:int}/graphs/{graphId:int}/slots/{slotId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteGraphSlot(int containerId, int graphId, int slotId, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(ManagerEditLockTargets.Schedule(containerId, graphId), "This schedule") is { } conflict)
        {
            return conflict;
        }

        var graph = await containerService.GetGraphByIdAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
        var deletedSlot = graph?.Slots.FirstOrDefault(slot => slot.Id == slotId);
        await containerService.DeleteGraphSlotAsync(containerId, graphId, slotId, cancellationToken).ConfigureAwait(false);
        await LogManagerActionAsync(
            $"Deleted {(deletedSlot is null ? "a shift" : DescribeShift(deletedSlot, graph))} from {DescribeScheduleOrFallback(graph)}.",
            cancellationToken).ConfigureAwait(false);
        await NotifyGraphChangedAsync(containerId, graphId, "manager-schedule-slot-deleted").ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("{containerId:int}/graphs/{graphId:int}/employees")]
    [ProducesResponseType(typeof(IEnumerable<GraphEmployeeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<GraphEmployeeDto>>> GetGraphEmployees(int containerId, int graphId, CancellationToken cancellationToken)
    {
        var employees = await containerService.GetGraphEmployeesAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
        if (employees is null)
        {
            return NotFound(CreateNotFoundProblem($"Graph with id {graphId} was not found."));
        }

        return Ok(employees.Select(x => x.ToGraphEmployeeDto()));
    }

    [HttpPost("{containerId:int}/graphs/{graphId:int}/employees")]
    [ProducesResponseType(typeof(GraphEmployeeDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<GraphEmployeeDto>> AddGraphEmployee(int containerId, int graphId, [FromBody] AddGraphEmployeeRequest request, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(ManagerEditLockTargets.Schedule(containerId, graphId), "This schedule") is { } conflict)
        {
            return conflict;
        }

        var created = await containerService.AddGraphEmployeeAsync(containerId, graphId, request.ToAddModel(graphId), cancellationToken).ConfigureAwait(false);
        var graph = await containerService.GetGraphByIdAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
        var assignment = graph?.Employees.FirstOrDefault(employee => employee.EmployeeId == created.EmployeeId) ?? created;
        await LogManagerActionAsync(
            $"Added {DescribeEmployee(assignment)} to {DescribeScheduleOrFallback(graph)}.",
            cancellationToken).ConfigureAwait(false);
        await NotifyGraphChangedAsync(containerId, graphId, "manager-schedule-employee-added").ConfigureAwait(false);
        return CreatedAtAction(nameof(GetGraphEmployees), new { containerId, graphId }, created.ToGraphEmployeeDto());
    }

    [HttpPut("{containerId:int}/graphs/{graphId:int}/employees/{graphEmployeeId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateGraphEmployee(int containerId, int graphId, int graphEmployeeId, [FromBody] UpdateGraphEmployeeRequest request, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(ManagerEditLockTargets.Schedule(containerId, graphId), "This schedule") is { } conflict)
        {
            return conflict;
        }

        await containerService.UpdateGraphEmployeeAsync(containerId, graphId, graphEmployeeId, request.ToUpdateModel(graphId, graphEmployeeId), cancellationToken).ConfigureAwait(false);
        var graph = await containerService.GetGraphByIdAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
        var assignment = graph?.Employees.FirstOrDefault(employee => employee.Id == graphEmployeeId);
        await LogManagerActionAsync(
            $"Updated {(assignment is null ? "an employee assignment" : DescribeEmployee(assignment))} in {DescribeScheduleOrFallback(graph)}.",
            cancellationToken).ConfigureAwait(false);
        await NotifyGraphChangedAsync(containerId, graphId, "manager-schedule-employee-updated").ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("{containerId:int}/graphs/{graphId:int}/employees/{graphEmployeeId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveGraphEmployee(int containerId, int graphId, int graphEmployeeId, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(ManagerEditLockTargets.Schedule(containerId, graphId), "This schedule") is { } conflict)
        {
            return conflict;
        }

        var graph = await containerService.GetGraphByIdAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
        var assignment = graph?.Employees.FirstOrDefault(employee => employee.Id == graphEmployeeId);
        await containerService.RemoveGraphEmployeeAsync(containerId, graphId, graphEmployeeId, cancellationToken).ConfigureAwait(false);
        await LogManagerActionAsync(
            $"Removed {(assignment is null ? "an employee" : DescribeEmployee(assignment))} from {DescribeScheduleOrFallback(graph)}.",
            cancellationToken).ConfigureAwait(false);
        await NotifyGraphChangedAsync(containerId, graphId, "manager-schedule-employee-removed").ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("{containerId:int}/graphs/{graphId:int}/cell-styles")]
    [ProducesResponseType(typeof(IEnumerable<GraphCellStyleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<GraphCellStyleDto>>> GetGraphCellStyles(int containerId, int graphId, CancellationToken cancellationToken)
    {
        var styles = await containerService.GetGraphCellStylesAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
        if (styles is null)
        {
            return NotFound(CreateNotFoundProblem($"Graph with id {graphId} was not found."));
        }

        return Ok(styles.Select(x => x.ToGraphCellStyleDto()));
    }

    [HttpPut("{containerId:int}/graphs/{graphId:int}/cell-styles")]
    [ProducesResponseType(typeof(GraphCellStyleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GraphCellStyleDto>> UpsertGraphCellStyle(int containerId, int graphId, [FromBody] UpsertGraphCellStyleRequest request, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(ManagerEditLockTargets.Schedule(containerId, graphId), "This schedule") is { } conflict)
        {
            return conflict;
        }

        var style = await containerService.UpsertGraphCellStyleAsync(containerId, graphId, request.ToUpsertModel(graphId), cancellationToken).ConfigureAwait(false);
        var graph = await containerService.GetGraphByIdAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
        await LogManagerActionAsync(
            $"Updated a cell style in {DescribeScheduleOrFallback(graph)}.",
            cancellationToken).ConfigureAwait(false);
        await NotifyGraphChangedAsync(containerId, graphId, "manager-schedule-style-updated").ConfigureAwait(false);
        return Ok(style.ToGraphCellStyleDto());
    }

    [HttpDelete("{containerId:int}/graphs/{graphId:int}/cell-styles/{styleId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteGraphCellStyle(int containerId, int graphId, int styleId, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(ManagerEditLockTargets.Schedule(containerId, graphId), "This schedule") is { } conflict)
        {
            return conflict;
        }

        await containerService.DeleteGraphCellStyleAsync(containerId, graphId, styleId, cancellationToken).ConfigureAwait(false);
        var graph = await containerService.GetGraphByIdAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
        await LogManagerActionAsync(
            $"Deleted a cell style from {DescribeScheduleOrFallback(graph)}.",
            cancellationToken).ConfigureAwait(false);
        await NotifyGraphChangedAsync(containerId, graphId, "manager-schedule-style-deleted").ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost]
    [ProducesResponseType(typeof(ContainerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ContainerDto>> Create([FromBody] CreateContainerRequest request, CancellationToken cancellationToken)
    {
        var created = await containerService.CreateAsync(request.ToCreateModel(), cancellationToken).ConfigureAwait(false);
        var dto = created.ToApiDto();
        await LogManagerActionAsync($"Created container {dto.Name}.", cancellationToken).ConfigureAwait(false);
        await realtimeNotifier
            .NotifyManagerDataChangedAsync(ManagerEditResourceTypes.Container, dto.Id.ToString(), "manager-container-created", dto.Id)
            .ConfigureAwait(false);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateContainerRequest request, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(ManagerEditLockTargets.Container(id), "This container") is { } conflict)
        {
            return conflict;
        }

        var existing = await containerService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Container with id {id} was not found.");
        }

        await containerService.UpdateAsync(request.ToUpdateModel(id), cancellationToken).ConfigureAwait(false);
        await LogManagerActionAsync($"Updated container {request.Name}.", cancellationToken).ConfigureAwait(false);
        await realtimeNotifier
            .NotifyManagerDataChangedAsync(ManagerEditResourceTypes.Container, id.ToString(), "manager-container-updated", id)
            .ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(ManagerEditLockTargets.Container(id), "This container") is { } conflict)
        {
            return conflict;
        }

        var existing = await containerService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Container with id {id} was not found.");
        }

        var result = await containerService.TryDeleteAsync(id, cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return BadRequest(ApiProblemDetailsFactory.CreateValidationProblem(
                HttpContext,
                result.Errors,
                result.Message));
        }

        await LogManagerActionAsync($"Deleted container {existing.Name}.", cancellationToken).ConfigureAwait(false);
        await realtimeNotifier
            .NotifyManagerDataChangedAsync(ManagerEditResourceTypes.Container, id.ToString(), "manager-container-deleted", id)
            .ConfigureAwait(false);
        return NoContent();
    }

    private Task LogManagerActionAsync(string action, CancellationToken cancellationToken)
        => workflowLogService.LogAsync(User, action, cancellationToken);

    private async Task<IReadOnlyDictionary<int, DateTimeOffset>> GetLastUpdatesAsync(
        IEnumerable<int> scheduleIds,
        CancellationToken cancellationToken)
    {
        if (scheduleLastUpdateService is null)
        {
            return new Dictionary<int, DateTimeOffset>();
        }

        return await scheduleLastUpdateService
            .GetLastUpdatesAsync(scheduleIds, cancellationToken)
            .ConfigureAwait(false);
    }

    private static string DescribeSchedule(ScheduleModel schedule)
    {
        var period = new DateTime(schedule.Year, schedule.Month, 1)
            .ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
        var shop = string.IsNullOrWhiteSpace(schedule.Shop?.Name) ? "the assigned shop" : $"shop \"{schedule.Shop.Name}\"";
        var container = string.IsNullOrWhiteSpace(schedule.Container?.Name) ? "the container" : $"container \"{schedule.Container.Name}\"";
        return $"schedule \"{schedule.Name}\" for {period} at {shop} in {container}";
    }

    private static string DescribeScheduleOrFallback(ScheduleModel? schedule)
        => schedule is null ? "the schedule" : DescribeSchedule(schedule);

    private static string DescribeShift(ScheduleSlotModel slot, ScheduleModel? schedule)
    {
        var date = schedule is not null &&
                   schedule.Month is >= 1 and <= 12 &&
                   slot.DayOfMonth >= 1 &&
                   slot.DayOfMonth <= DateTime.DaysInMonth(schedule.Year, schedule.Month)
            ? new DateTime(schedule.Year, schedule.Month, slot.DayOfMonth)
                .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            : $"day {slot.DayOfMonth}";
        var employee = slot.Employee is null
            ? "unassigned"
            : $"assigned to {GetEmployeeDisplayName(slot.Employee)}";
        return $"shift on {date}, {slot.FromTime}-{slot.ToTime}, {employee}";
    }

    private static string DescribeEmployee(ScheduleEmployeeModel assignment)
    {
        var name = assignment.Employee is null
            ? "the employee"
            : GetEmployeeDisplayName(assignment.Employee);
        var minimumHours = assignment.MinHoursMonth.HasValue
            ? $" with a {assignment.MinHoursMonth.Value}-hour monthly minimum"
            : string.Empty;
        return $"employee {name}{minimumHours}";
    }

    private static string GetEmployeeDisplayName(EmployeeModel employee)
    {
        var fullName = $"{employee.FirstName} {employee.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? "the employee" : $"\"{fullName}\"";
    }

    private async Task NotifyGraphChangedAsync(int containerId, int graphId, string reason)
    {
        await realtimeNotifier
            .NotifyManagerDataChangedAsync(ManagerEditResourceTypes.Schedule, $"{containerId}:{graphId}", reason, containerId, graphId)
            .ConfigureAwait(false);
        await realtimeNotifier.NotifyScheduleChangedAsync(containerId, graphId, reason).ConfigureAwait(false);
        await realtimeNotifier.NotifyShiftSwapsChangedAsync(containerId, graphId, graphId, reason).ConfigureAwait(false);
    }

    private ActionResult? CreateEditLockConflictResult(ManagerEditLockTarget target, string resourceLabel)
        => ManagerEditLockHttp.CreateConflictResult(this, editLockService, target, resourceLabel);

    private ProblemDetails CreateNotFoundProblem(string detail)
        => new()
        {
            Type = "not_found",
            Title = "Not Found",
            Status = StatusCodes.Status404NotFound,
            Detail = detail,
            Instance = HttpContext.Request.Path
        };
}
